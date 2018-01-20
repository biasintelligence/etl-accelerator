/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     11/18/2016
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
*******************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System.Data;
using System.Data.SqlClient;

using Serilog;
using ControllerRuntime;


namespace AwsActivities
{
    /// <summary>
    /// Gzip compression
    /// Example: InputFile = c:\test.txt.gz, OutputFolder = c:\output Command = C(compress)/D(decompress)
    /// </summary>
    public class BsonSqlLoaderActivity : IWorkflowActivity
    {
        private const string CONNECTION_STRING = "ConnectionString";
        private const string TABLE_NAME = "TableName";
        private const string INPUT_FILE = "InputFile";
        private const string TIMEOUT = "Timeout";
        private const string RUNID = "@RunId";


        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private ILogger _logger;
        private List<string> _required_attributes = new List<string>() { CONNECTION_STRING, TABLE_NAME, INPUT_FILE, TIMEOUT, RUNID };

        #region IWorkflowActivity
        public string[] RequiredAttributes
        {
            get { return _required_attributes.ToArray(); }
        }

        public void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            if (_required_attributes.Count != args.RequiredAttributes.Length)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }


            foreach (WorkflowAttribute attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Name, StringComparer.InvariantCultureIgnoreCase))
                    _attributes.Add(attribute.Name, attribute.Value);
            }

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING]);
            _logger.Debug("SqlServer: {Server}.{Database}", builder.DataSource, builder.InitialCatalog);
            _logger.Information("Bson : {From} => {To}", _attributes[INPUT_FILE], _attributes[TABLE_NAME]);

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Int32.Parse(_attributes[TIMEOUT]))))
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token))
                {

                    BsonToSql( _attributes[TABLE_NAME], _attributes[INPUT_FILE], linkedCts.Token);
                    result = WfResult.Succeeded;
                }
            }

            return result;
        }
        #endregion

        private void BsonToSql(string tableName,string input, CancellationToken token)
        {
            const string PARAM_FILENAME = "@fileName";
            const string PARAM_DOCUMENT = "@document";
            const string PARAM_PROCESSID = "@processId";

            const string QUERY = @"insert {0} (fileName,document,processId) values (@fileName,@document,@processId);";


            using (var cn = new SqlConnection(_attributes[CONNECTION_STRING]))
            {
                cn.Open();
                try
                {
                    using (SqlCommand cmd = new SqlCommand(String.Format(QUERY, tableName), cn))
                    {

                        cmd.Parameters.Add(PARAM_FILENAME, SqlDbType.NVarChar, 500);
                        cmd.Parameters.Add(PARAM_DOCUMENT, SqlDbType.NVarChar, -1);
                        var param = cmd.Parameters.Add(PARAM_PROCESSID, SqlDbType.Int);
                        param.Value = Int32.Parse(_attributes[RUNID]);
                        cmd.Prepare();
                        using (token.Register(cmd.Cancel))
                        {

                            string[] files = Directory.GetFiles(Path.GetDirectoryName(input), Path.GetFileName(input), SearchOption.TopDirectoryOnly);
                            foreach (string file in files)
                            {
                                token.ThrowIfCancellationRequested();
                                CheckTable(tableName, token);
                                cmd.Parameters[PARAM_FILENAME].Value = file;

                                FileInfo fileToLoad = new FileInfo(file);
                                using (FileStream originalFileStream = fileToLoad.OpenRead())
                                {
                                    if ((File.GetAttributes(fileToLoad.FullName) &
                                       FileAttributes.Hidden) != FileAttributes.Hidden & fileToLoad.Extension != ".json")
                                    {

                                        using (var reader = new BsonBinaryReader(originalFileStream))
                                        {

                                            while (!reader.IsAtEndOfFile())
                                            {
                                                token.ThrowIfCancellationRequested();

                                                var bson = BsonSerializer.Deserialize<BsonDocument>(reader);
                                                string json = bson.ToJson(new JsonWriterSettings() { OutputMode = JsonOutputMode.Strict });
                                                cmd.Parameters[PARAM_DOCUMENT].Value = json;
                                                cmd.ExecuteNonQuery();

                                            }
                                        }


                                    }

                                    _logger.Information("Loaded {From} => {To}", fileToLoad.Name, tableName);
                                }
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw ex;
                    //_logger.Write(String.Format("SqlServer exception: {0}", ex.Message));
                    //result = WfResult.Create(WfStatus.Failed, ex.Message, ex.ErrorCode);
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();

                }

            }
        }

        private void CheckTable(string tableName, CancellationToken token)
        {
            const string QUERY = @"
declare @tableName sysname = '{0}';
declare @objectId int = object_id(@tableName,'U');
if (@objectId is null)
begin
	exec ('create table ' +  @tableName
	+ ' (recId int identity(1,1) primary key'
	+ ' ,fileName nvarchar(500) not null'
	+ ' ,document nvarchar(max) null'
	+ ' ,processId int null default 0'
	+ ' ,createDt datetime not null default getdate()'
	+ ' ,changeDt datetime null default getdate()'
	+ ' ,changeBy nvarchar(30) not null default suser_sname()'
	+ ');');

end
else
begin

	if (4 <> (select count(*) as cnt from sys.columns
				where [object_Id] = @objectId
				  and name in ('recId','fileName','document','processId')))
	begin
	declare @msg nvarchar(1000) = 'Table is not in correct format: ' + @tableName;
		throw 50001, @msg, 1;
	end

end
";

            using (SqlConnection cn = new SqlConnection(_attributes[CONNECTION_STRING]))
            {
                try
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(String.Format(QUERY, tableName), cn))
                    {
                        cmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);
                        using (token.Register(cmd.Cancel))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw ex;
                    //_logger.Write(String.Format("SqlServer exception: {0}", ex.Message));
                    //result = WfResult.Create(WfStatus.Failed, ex.Message, ex.ErrorCode);
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();

                }
            }
        }

    }

}
