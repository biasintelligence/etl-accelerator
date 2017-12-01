/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     02/20/2015
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
*******************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

using Serilog;
using ControllerRuntime;

namespace DefaultActivities
{
    /// <summary>
    /// Push new file to process list to Controller Counter table
    /// </summary>
    public class FileGetProcessListActivity : IWorkflowActivity
    {
        protected const string CONNECTION_STRING = "ConnectionString";
        protected const string CONNECTION_STRING_REGISTER = "RegisterConnectionString";
        protected const string FILE_SOURCE = "SourceName";
        protected const string TIMEOUT = "Timeout";
        protected const string ETL_RUNID = "etl:RunId";
        protected const string ETL_BATCHID = "etl:BatchId";
        protected const string ETL_STEPID = "etl:StepId";

        protected const string GET_LIST_QUERY = "dbo.prc_FileProcessGetNext";

        protected Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        protected ILogger _logger;
        protected List<string> _required_attributes = new List<string>()
        { CONNECTION_STRING,
          CONNECTION_STRING_REGISTER,
          FILE_SOURCE,
          TIMEOUT,
          ETL_RUNID,
          ETL_BATCHID,
          ETL_STEPID
        };


        public string[] RequiredAttributes
        {
            get { return _required_attributes.ToArray(); }
        }

        public virtual void Configure(WorkflowActivityArgs args)
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

            SqlConnectionStringBuilder builder_controller = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING]);
            SqlConnectionStringBuilder builder_register = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING_REGISTER]);
            _logger.Debug("Controller: {Server}.{Database}", builder_controller.DataSource, builder_controller.InitialCatalog);
            _logger.Debug("Register: {Server}.{Database}", builder_register.DataSource, builder_register.InitialCatalog);
            _logger.Information("Processing from File Source: {File}", _attributes[FILE_SOURCE]);
        }

        public virtual WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            int runId = 0;
            Int32.TryParse(_attributes[ETL_RUNID], out runId);

            int batchId = 0;
            Int32.TryParse(_attributes[ETL_BATCHID], out batchId);

            int stepId = 0;
            Int32.TryParse(_attributes[ETL_STEPID], out stepId);

            Dictionary<string, string> files = new Dictionary<string, string>();
            ControllerCounter counter = new ControllerCounter(_attributes[CONNECTION_STRING], _logger)
            {
                BatchId = batchId,
                StepId = stepId,
                RunId = runId
            };

            SqlConnection cn = new SqlConnection(_attributes[CONNECTION_STRING_REGISTER]);
            try
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(GET_LIST_QUERY, cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;


                    cmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);
                    cmd.Parameters.AddWithValue("@processId", runId);
                    cmd.Parameters.AddWithValue("@sourceName", _attributes[FILE_SOURCE]);
                    cmd.Parameters.AddWithValue("@count", 1);

                    using (token.Register(cmd.Cancel))
                    {
                        var reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                //_logger.WriteDebug(String.Format("File to process id: {0}, name: {1}", reader["fileId"], reader["fullName"]));
                                files.Add("fileId", reader["fileId"].ToString());
                                files.Add(String.Format("fileName_{0}", reader["fileId"]), reader["fullName"].ToString());
                            }
                        }
                        else
                        {
                            _logger.Debug("No files to process");
                            files.Add("fileId", "0");
                        }

                        result = WfResult.Succeeded;
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

            counter.SetCounters(files);
            return result;
        }

    }
}
