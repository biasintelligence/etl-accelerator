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
    /// Set Counter to Table ControlValue (incremental)
    /// </summary>
    public class TableControlToCounterActivity : IWorkflowActivity
    {
        protected const string CONTROLLER_CONNECTION_STRING = "ConnectionString";
        protected const string SOURCE_CONNECTION_STRING = "SourceConnectionString";
        protected const string TABLE_NAME = "SourceTableName";
        protected const string TABLE_CONTROL_COLUMN = "ControlColumn";
        protected const string COUNTER_NAME = "CounterName";
        protected const string TIMEOUT = "Timeout";
        protected const string ETL_RUNID = "@RunId";
        protected const string ETL_BATCHID = "etl:BatchId";
        protected const string ETL_STEPID = "etl:StepId";

        protected Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        protected IWorkflowLogger _logger;
        protected List<string> _required_attributes = new List<string>()
        { CONTROLLER_CONNECTION_STRING,
          SOURCE_CONNECTION_STRING,
          TABLE_NAME,
          TABLE_CONTROL_COLUMN,
          COUNTER_NAME,
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

            _logger.WriteDebug(String.Format("ControllerConnectionString: {0}", _attributes[CONTROLLER_CONNECTION_STRING]));
            _logger.WriteDebug(String.Format("SourceConnectionString: {0}", _attributes[SOURCE_CONNECTION_STRING]));
            _logger.Write(String.Format("Table: {0}, Coltrol: {1}", _attributes[TABLE_NAME], _attributes[TABLE_CONTROL_COLUMN]));
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

            ControllerCounter counter = new ControllerCounter(_attributes[CONTROLLER_CONNECTION_STRING], _logger)
            {
                BatchId = batchId,
                StepId = stepId,
                RunId = runId
            };


            Dictionary<string, string> controlset = new Dictionary<string, string>();
            controlset.Add(_attributes[COUNTER_NAME], GetControlValue(token));
            counter.SetCounters(controlset);
            return WfResult.Succeeded;
        }

        private string GetControlValue(CancellationToken token)
        {
            SqlConnection cn = new SqlConnection(_attributes[SOURCE_CONNECTION_STRING]);
            try
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(String.Format("select cast(max({0}) as nvarchar(100)) as ControlValue from {1};",
                    _attributes[TABLE_CONTROL_COLUMN],
                    _attributes[TABLE_NAME]), cn)
                )
                {
                    cmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);
                    using (token.Register(cmd.Cancel))
                    {
                        var value = cmd.ExecuteScalar();
                        return (value == null) ? String.Empty : value.ToString();
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
