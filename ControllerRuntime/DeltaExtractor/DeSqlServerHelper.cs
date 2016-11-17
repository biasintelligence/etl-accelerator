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
                DbConnectionStringBuilder dbsb = new DbConnectionStringBuilder();
                dbsb.ConnectionString = value;
                object srv = null;
                object db = null;

                if (dbsb.TryGetValue("Dsn", out srv))
                {
                    //User ODBC

                    RegistryKey dsn = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("ODBC").OpenSubKey("ODBC.INI").OpenSubKey(srv.ToString());
                    if (dsn == null)
                    {
                        dsn = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("ODBC").OpenSubKey("ODBC.INI").OpenSubKey(srv.ToString());
                    }
                    if (dsn == null)
                    {
                        throw new InvalidArgumentException(String.Format("Dsn={0} is not found", srv.ToString()));
                    }

                    try
                    {
                        srv = dsn.GetValue("Server");
                        db = dsn.GetValue("Database");
                        if (srv != null) this.server = srv.ToString();
                        if (db != null) this.database = db.ToString();
                    }
                    catch
                    {
                        throw new InvalidArgumentException(String.Format("Dsn={0} is not configured properly: Server and Database properties are required for SqlServer connection", srv.ToString()));
                    }
                }
                else
                {

                    if (dbsb.TryGetValue("Data Source", out srv))
                    {
                        this.server = srv.ToString();
                    }
                    else if (dbsb.TryGetValue("server", out srv))
                    {
                        this.server = srv.ToString();
                    }

                    if (dbsb.TryGetValue("Initial Catalog", out db))
                    {
                        this.database = db.ToString();
                    }
                    else if (dbsb.TryGetValue("database", out db))
                    {
                        this.database = db.ToString();
                    }
                }
                //this.connectionstring = dbsb.ConnectionString;
                this.connectionstring = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Server={0};Database={1};Trusted_Connection=yes", this.server, this.database);
            }

            private get
            {
                return this.connectionstring;
            }
        }


        public string CanonicTableName
        {
            get
            {
                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", (this.tbl_database == UNKNOWN) ? this.database : this.tbl_database, TableName);
            }
        }

        public string FullName
        {
            get
            {
                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}", this.server, CanonicTableName);
            }
        }


        public bool IsValid { get; set; }
        public bool IsView { get; set; }
        public bool Test(IWorkflowLogger logger)
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

        private bool test(IWorkflowLogger logger)
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
                    //cn.ConnectionString = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Server={0};Database={1};Trusted_Connection=yes", tempTableName[tempTableName.Length - 4], tempTableName[tempTableName.Length - 3]);
                    //overide cn database if Staging and Destination are different
                    if (this.tbl_database == UNKNOWN)
                    {
                        cn.ConnectionString = ConnectionString;
                    }
                    else
                    {
                        cn.ConnectionString = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Server={0};Database={1};Trusted_Connection=yes", this.server, this.tbl_database);
                    }
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(String.Format(System.Globalization.CultureInfo.InvariantCulture, "select OBJECTPROPERTY(object_id('{0}'),'IsTable')", this.tablename), cn))
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
                logger.WriteError(
                    String.Format(System.Globalization.CultureInfo.InvariantCulture, "Test Destination {0} failed with messsage: {1}"
                    , FullName, e.Message), e.HResult);
                this.IsValid = false;
                return false;
            }
        }

        public bool TruncateDestinationTable(IWorkflowLogger logger)
        {
            //if (!this.IsValid) { return false; }
            if (this.StagingBlock.Staging) { return true; }
            try
            {
                string query = "";
                if (this.IsView)
                {
                    logger.WriteDebug("Deleting view data " + FullName);
                    query = "delete from " + this.CanonicTableName;
                }
                else
                {
                    logger.WriteDebug("Truncating table data " + FullName);
                    query = "truncate table " + this.CanonicTableName;
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
                logger.WriteError("Failed to truncate table " + this.FullName + ": " + e.Message, e.HResult);
                throw (new CouldNotCreateStagingTableException("Failed to truncate table " + this.CanonicTableName, e));
            }
        }

        public bool CreateStagingTable(bool createflag, IWorkflowLogger logger)
        {
            if (!this.StagingBlock.Staging) { return true; }
            try
            {
                if (string.IsNullOrEmpty(this.StagingBlock.StagingTableName))
                {
                    string[] tempTableName = ParseTableName(TableName);
                    this.StagingBlock.StagingTableName = tempTableName[tempTableName.Length - 2] + ".staging_" + tempTableName[tempTableName.Length - 1];
                }

                logger.WriteDebug("Creating a Staging table " + this.StagingBlock.StagingTableName + " for the destination " + FullName);

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
                            logger.WriteDebug("Staging table prepare has been executed with options (" + this.StagingBlock.UserOptions + "): " + this.StagingBlock.StagingTableName);
                        }
                    }
                }
                else
                {
                    logger.WriteDebug("Staging table prepare has been skipped: " + this.StagingBlock.StagingTableName);
                }

                return true;
            }
            catch (Exception e)
            {
                this.StagingBlock.Staging = false;
                logger.WriteError("An error occurred while trying to create a staging table for " + this.CanonicTableName + ": " + e.Message, e.HResult);
                throw (new CouldNotCreateStagingTableException("An error occurred while trying to create a staging table for " + this.CanonicTableName, e));
            }
        }

        public bool UploadStagingTable(int RunId, IWorkflowLogger logger)
        {
            if (!this.StagingBlock.Staging) { return true; }
            logger.WriteDebug("Uploading staging table " + this.StagingBlock.StagingTableName + " to destination " + this.CanonicTableName);

            //if nothing has been staged, then the table can't be upserted.
            if (string.IsNullOrEmpty(this.StagingBlock.StagingTableName))
            {
                logger.Write("Staging table has not been created.  There must be a staging table in order to perform an upsert.");
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
                        logger.WriteDebug("Staging table upload has been executed with options (" + this.StagingBlock.UserOptions + "): " + this.TableName);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                logger.WriteError("An error occurred while performing the Upsert for " + this.CanonicTableName + ": " + e.Message, e.HResult);
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

            strTableName = strTableName.Replace("[", "");
            strTableName = strTableName.Replace("]", "");

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