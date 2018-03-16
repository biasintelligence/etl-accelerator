using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Data;
using System.Runtime.Serialization;
using System.Globalization;
using System.Linq;
using System.Data.Common;
using Microsoft.Win32;
//using Excel = Microsoft.Office.Interop.Excel;

using ControllerRuntime;
using Serilog;

namespace BIAS.Framework.DeltaExtractor
{

    //public interface IDeStagingSupport
    //{
    //    bool Test();
    //    bool IsValid { get; set; }
    //    bool IsView { get; set; }
    //    bool TruncateDestinationTable();
    //    bool CreateStagingTable(bool createflag);
    //    bool UploadStagingTable(int RunId);        
    //}


    public class DeSqlServerHelper : IDeStagingSupport
    {

        const string UNKNOWN = "Unknown";
        private string connectionstring;
        private string tablename = UNKNOWN;
        private string inputtablename = UNKNOWN;
        private string server = UNKNOWN;
        private string database = UNKNOWN;
        private string tbl_database = UNKNOWN;

        public PartitionRange PartitionRange { private get; set; }
        public StagingBlock StagingBlock { private get; set; }
        public DBConnection DBConnection { private get; set; }

        public string TableName
        {
            private get
            {
                return this.tablename;
            }
            set
            {
                inputtablename = value;
                string[] tempTableName = ParseTableName(value);
                this.tablename = tempTableName[tempTableName.Length - 2] + "." + tempTableName[tempTableName.Length - 1];

                if (tempTableName.Length > 2)
                {
                    this.tbl_database = tempTableName[tempTableName.Length - 3];
                }
            }
        }

        public string ConnectionString
        {
            set
            {             
                this.connectionstring = PrepareSqlConnectionString(value);
                //this.connectionstring = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Server={0};Database={1};Trusted_Connection=yes", this.server, this.database);
            }

            private get
            {
                return this.connectionstring;
            }
        }

        private string PrepareSqlConnectionString(string connectionString)
        {
            DbConnectionStringBuilder dbsb = new DbConnectionStringBuilder();
            SqlConnectionStringBuilder sqlsb = new SqlConnectionStringBuilder();
            dbsb.ConnectionString = connectionString;
            object prop = null;

            if (dbsb.TryGetValue("Dsn", out prop))
            {
                //User ODBC

                RegistryKey dsn = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("ODBC").OpenSubKey("ODBC.INI").OpenSubKey(prop.ToString());
                if (dsn == null)
                {
                    dsn = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("ODBC").OpenSubKey("ODBC.INI").OpenSubKey(prop.ToString());
                }
                if (dsn == null)
                {
                    throw new InvalidArgumentException(String.Format("Dsn={0} is not found", prop.ToString()));
                }

                try
                {
                    prop = dsn.GetValue("Server");
                    if (prop != null) this.server = prop.ToString();

                    prop = dsn.GetValue("Database");
                    if (prop != null) this.database = prop.ToString();

                    sqlsb.DataSource = this.server;
                    sqlsb.InitialCatalog = this.database;

                }
                catch
                {
                    throw new InvalidArgumentException(String.Format("Dsn={0} is not configured properly: Server and Database properties are required for SqlServer connection", prop.ToString()));
                }
            }
            else
            {


                if (dbsb.TryGetValue("Data Source", out prop))
                {
                    this.server = prop.ToString();
                }
                else if (dbsb.TryGetValue("server", out prop))
                {
                    this.server = prop.ToString();
                }
                sqlsb.DataSource = this.server;

                if (dbsb.TryGetValue("Initial Catalog", out prop))
                {
                    this.database = prop.ToString();
                }
                else if (dbsb.TryGetValue("database", out prop))
                {
                    this.database = prop.ToString();
                }
                sqlsb.InitialCatalog = this.database;

                if (dbsb.TryGetValue("Trusted_Connection", out prop))
                {
                    sqlsb.IntegratedSecurity = bool.Parse(prop.ToString());
                }
                else if (dbsb.TryGetValue("Integrated Security", out prop))
                {
                    sqlsb.IntegratedSecurity = (prop.ToString().Equals("SSPI")) ? true : false;
                }

                if (!sqlsb.IntegratedSecurity)
                {
                    if (dbsb.TryGetValue("User ID", out prop))
                    {
                        sqlsb.UserID = prop.ToString();
                    }
                    else if (dbsb.TryGetValue("Usr", out prop))
                    {
                        sqlsb.UserID = prop.ToString();
                    }

                    if (dbsb.TryGetValue("Password", out prop))
                    {
                        sqlsb.Password = prop.ToString();
                    }
                    else if (dbsb.TryGetValue("Pwd", out prop))
                    {
                        sqlsb.Password = prop.ToString();
                    }

                }

                if (dbsb.TryGetValue("Connection Timeout", out prop))
                {
                    sqlsb.ConnectTimeout = int.Parse(prop.ToString());
                }
            }
            return sqlsb.ConnectionString;
        }



        public string CanonicTableName
        {
            get
            {
                if (this.tbl_database == UNKNOWN)
                    return $"{this.database.AddQuotes()}.{TableName}";
                else
                    return $"{this.tbl_database}.{TableName}";

            }
        }


        public string FullName
        {
            get
            {
                return $"{this.server}.{CanonicTableName}";
            }
        }


        public bool IsValid { get; set; }
        public bool IsView { get; set; }
        public bool Test(ILogger logger)
        {
            this.IsValid = this.test(logger);
            if (this.IsValid)
            {
                if (!(StagingBlock == null))
                {
                    if (StagingBlock.Staging)
                    {
                        this.IsValid = CreateStagingTable(true, logger);
                        if (!this.IsValid)
                        {
                            throw new CouldNotCreateStagingTableException(StagingBlock.StagingTableName);
                        }
                    }
                    else if (StagingBlock.Reload)
                    {
                        this.IsValid = TruncateDestinationTable(logger);
                    }
                }
            }
            return this.IsValid;
        }

        private bool test(ILogger logger)
        {
            try
            {
                if (this.TableName == UNKNOWN)
                    throw new InvalidArgumentException("Destination TableName is empty");

                if (!(PartitionRange == null) && (PartitionRange.Min > PartitionRange.Max))
                    throw new InvalidArgumentException("Invalid Destination Partition Settings");

                //ParseTableName returns a string array in the shape of [server].[db].[schema].[table]
                //Use the table and schema to test for existance
                //string[] tempTableName = ParseTableName(TableName);
                using (SqlConnection cn = new SqlConnection())
                {
                    cn.ConnectionString = ConnectionString;
                    //if (this.tbl_database == UNKNOWN)
                    //{
                    //    cn.ConnectionString = ConnectionString;
                    //}
                    //else
                    //{
                    //    cn.ConnectionString = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Server={0};Database={1};Trusted_Connection=yes", this.server, this.tbl_database.RemoveQuotes());
                    //}
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(String.Format(System.Globalization.CultureInfo.InvariantCulture,
@"if(object_id('{0}','U') is not null)
    select 1
else if (object_id('{0}', 'V') is not null)
    select 0
else select cast(null as int)",
                        this.inputtablename), cn))
                    {
                        cmd.CommandTimeout = this.DBConnection.QueryTimeout;
                        cmd.CommandType = CommandType.Text;
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader.GetSqlInt32(0).IsNull)
                                {
                                    throw new InvalidTableNameException("The table/view is not found");
                                }
                                this.IsView = (reader.GetInt32(0) == 0);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                logger.Error(e,
                    "Test Destination {Name} failed with messsage: {Message}"
                    , FullName, e.Message);
                this.IsValid = false;
                return false;
            }
        }

        public bool TruncateDestinationTable(ILogger logger)
        {
            //if (!this.IsValid) { return false; }
            if (this.StagingBlock.Staging) { return true; }
            try
            {
                string query = "";
                if (this.IsView)
                {
                    logger.Debug("Deleting view data {Name}", FullName);
                    query = "delete from " + this.inputtablename;
                }
                else
                {
                    logger.Debug("Truncating table data {Name}", FullName);
                    query = "truncate table " + this.inputtablename;
                }

                using (SqlConnection cn = new SqlConnection())
                {
                    cn.ConnectionString = ConnectionString;
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, cn))
                    {
                        cmd.CommandTimeout = this.DBConnection.QueryTimeout;
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                this.StagingBlock.Staging = false;
                logger.Error(e,"Failed to truncate table {Name}: {Message}", this.FullName, e.Message);
                throw (new CouldNotCreateStagingTableException("Failed to truncate table " + this.CanonicTableName, e));
            }
        }

        public bool CreateStagingTable(bool createflag, ILogger logger)
        {
            if (!this.StagingBlock.Staging) { return true; }
            try
            {
                if (string.IsNullOrEmpty(this.StagingBlock.StagingTableName))
                {
                    string[] tempTableName = ParseTableName(TableName);
                    this.StagingBlock.StagingTableName = tempTableName[tempTableName.Length - 2] + ".staging_" + tempTableName[tempTableName.Length - 1];
                }

                logger.Debug("Creating a Staging table {Table} for the destination {Destination}", this.StagingBlock.StagingTableName, FullName);

                if (createflag)
                {
                    using (SqlConnection cn = new SqlConnection())
                    {
                        //cn.ConnectionString = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Server={0};Database={1};Trusted_Connection=yes", this.DBConnection.Server, this.DBConnection.Database);
                        cn.ConnectionString = ConnectionString;
                        cn.Open();
                        using (SqlCommand cmd = new SqlCommand(String.IsNullOrEmpty(this.StagingBlock.StagingTablePrepare) ? "dbo.prc_StagingTablePrepare" : this.StagingBlock.StagingTablePrepare, cn))
                        {
                            cmd.CommandTimeout = this.DBConnection.QueryTimeout;
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@src", this.CanonicTableName);
                            cmd.Parameters.Add(CreateParameter("@dst", this.StagingBlock.StagingTableName, System.Data.SqlDbType.VarChar, 255, System.Data.ParameterDirection.InputOutput));
                            cmd.Parameters.AddWithValue("@options", this.StagingBlock.UserOptions);
                            cmd.ExecuteNonQuery();

                            this.StagingBlock.StagingTableName = cmd.Parameters["@dst"].Value.ToString();
                            logger.Debug("Staging table prepare has been executed with options ({Options}): {TableName}", this.StagingBlock.UserOptions, this.StagingBlock.StagingTableName);
                        }
                    }
                }
                else
                {
                    logger.Debug("Staging table prepare has been skipped: {TableName}", this.StagingBlock.StagingTableName);
                }

                return true;
            }
            catch (Exception e)
            {
                this.StagingBlock.Staging = false;
                logger.Error(e,"An error occurred while trying to create a staging table for {TableName}: {Message}", this.CanonicTableName, e.Message);
                throw (new CouldNotCreateStagingTableException("An error occurred while trying to create a staging table for " + this.CanonicTableName, e));
            }
        }

        public bool UploadStagingTable(int RunId, ILogger logger)
        {
            if (!this.StagingBlock.Staging) { return true; }
            logger.Debug("Uploading staging table {TableName} to destination {Destination}", this.StagingBlock.StagingTableName, this.CanonicTableName);

            //if nothing has been staged, then the table can't be upserted.
            if (string.IsNullOrEmpty(this.StagingBlock.StagingTableName))
            {
                logger.Information("Staging table has not been created. There must be a staging table in order to perform an upsert.");
                return false;
            }
            try
            {

                using (SqlConnection cn = new SqlConnection())
                {
                    //cn.ConnectionString = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Server={0};Database={1};Trusted_Connection=yes", this.DBConnection.Server, this.DBConnection.Database);
                    cn.ConnectionString = ConnectionString;
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(String.IsNullOrEmpty(this.StagingBlock.StagingTableUpload) ? "dbo.prc_StagingTableUpload" : this.StagingBlock.StagingTableUpload, cn))
                    {
                        cmd.CommandTimeout = this.DBConnection.QueryTimeout;
                        cmd.CommandType = CommandType.StoredProcedure;
                        //cmd.Parameters.Add(CDRDBController.CreateParameter("@RTC", retval, System.Data.SqlDbType.Int, 4, System.Data.ParameterDirection.ReturnValue));
                        cmd.Parameters.AddWithValue("@src", this.StagingBlock.StagingTableName);
                        cmd.Parameters.AddWithValue("@dst", this.CanonicTableName);
                        cmd.Parameters.AddWithValue("@RunId", RunId);
                        cmd.Parameters.AddWithValue("@options", this.StagingBlock.UserOptions);
                        cmd.ExecuteNonQuery();

                        this.StagingBlock.StagingTableName = cmd.Parameters["@dst"].Value.ToString();
                        logger.Debug("Staging table upload has been executed with options ({Options}): {TableName}", this.StagingBlock.UserOptions, this.TableName);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                logger.Error(e,"An error occurred while performing the Upsert for {TableName}: {Message}", this.CanonicTableName, e.Message);
                throw (new CouldNotPerformUpsert("An error occurred while performing the Upsert for " + this.CanonicTableName + ": " + e.Message, e));
            }
        }


        private string[] ParseTableName(string strTableName)
        {
            if (!strTableName.Contains("."))
            {
                strTableName = "dbo." + strTableName;
            }
            else if (strTableName.Contains(".."))
            {
                strTableName = strTableName.Replace("..", ".dbo.");
            }

            //strTableName = strTableName.Replace("[", "");
            //strTableName = strTableName.Replace("]", "");

            string[] retarr = strTableName.Split('.');

            return retarr;
        }

        private SqlParameter CreateParameter(string parameterName, Object parameterValue, System.Data.SqlDbType dbType, int parameterSize, System.Data.ParameterDirection parameterDirection)
        {
            SqlParameter sParameter = new SqlParameter(parameterName, dbType, parameterSize);
            sParameter.Size = parameterSize;
            sParameter.Direction = parameterDirection;
            sParameter.Value = parameterValue;
            return sParameter;
        }
    }
}