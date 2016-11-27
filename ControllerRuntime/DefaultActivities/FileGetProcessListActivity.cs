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
        protected const string SET_COUNTER_QUERY = "dbo.prc_ETLCounterSet";


        protected Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        protected IWorkflowLogger _logger;
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

            _logger.WriteDebug(String.Format("ConnectionString: {0}", _attributes[CONNECTION_STRING]));
            _logger.WriteDebug(String.Format("RegisterConnectionString: {0}", _attributes[CONNECTION_STRING_REGISTER]));
            _logger.Write(String.Format("Processing from File Source: {0}", _attributes[FILE_SOURCE]));
        }

        public virtual WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));


            SqlConnection scn = new SqlConnection(_attributes[CONNECTION_STRING_REGISTER]);
            SqlConnection dcn = new SqlConnection(_attributes[CONNECTION_STRING]);
            try
            {
                scn.Open();
                using (SqlCommand scmd = new SqlCommand(GET_LIST_QUERY, scn))
                {
                    scmd.CommandType = CommandType.StoredProcedure;

                    int runId = 0;
                    Int32.TryParse(_attributes[ETL_RUNID], out runId);

                    int batchId = 0;
                    Int32.TryParse(_attributes[ETL_BATCHID], out batchId);

                    int stepId = 0;
                    Int32.TryParse(_attributes[ETL_STEPID], out stepId);

                    scmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);
                    scmd.Parameters.AddWithValue("@processId", runId);
                    scmd.Parameters.AddWithValue("@sourceName", _attributes[FILE_SOURCE]);
                    scmd.Parameters.AddWithValue("@count", 1);

                    using (token.Register(scmd.Cancel))
                    {
                        var reader = scmd.ExecuteReader();

                        dcn.Open();
                        using (SqlCommand dcmd = new SqlCommand(SET_COUNTER_QUERY, dcn))
                        {
                            dcmd.CommandType = CommandType.StoredProcedure;

                            dcmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);
                            dcmd.Parameters.AddWithValue("@pBatchId", batchId);
                            dcmd.Parameters.AddWithValue("@pStepId", stepId);
                            dcmd.Parameters.AddWithValue("@pRunId", runId);
                            dcmd.Parameters.Add("@pName", SqlDbType.NVarChar, 100);
                            dcmd.Parameters.Add("@pValue", SqlDbType.NVarChar, -1);

                            using (token.Register(dcmd.Cancel))
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        _logger.WriteDebug(String.Format("File to process id: {0}, name: {1}", reader["fileId"], reader["fullName"]));
                                        dcmd.Parameters["@pName"].SqlValue = "fileId";
                                        dcmd.Parameters["@pValue"].SqlValue = reader["fileId"];
                                        dcmd.ExecuteNonQuery();
                                        dcmd.Parameters["@pName"].SqlValue = String.Format("fileName_{0}", reader["fileId"]);
                                        dcmd.Parameters["@pValue"].SqlValue = reader["fullName"];
                                        dcmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    _logger.WriteDebug("No files to process");
                                    dcmd.Parameters["@pName"].SqlValue = "fileId";
                                    dcmd.Parameters["@pValue"].SqlValue = "0";
                                    dcmd.ExecuteNonQuery();

                                }
                            }

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
                if (scn.State != ConnectionState.Closed)
                    scn.Close();

                if (dcn.State != ConnectionState.Closed)
                    dcn.Close();

            }
            return result;
        }

    }
}
