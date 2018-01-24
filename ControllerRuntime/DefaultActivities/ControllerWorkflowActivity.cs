/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     01/20/2018
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
*******************************************************************

 */

using System;
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
    /// Exceute SqlClient ExecuteNonQuery function 
    /// </summary>
    public class ControllerWorkflowActivity : IWorkflowActivity
    {
        protected const string CONNECTION_STRING = WorkflowConstants.ATTRIBUTE_CONTROLLER_CONNECTIONSTRING;
        protected const string PROCESSOR_NAME = WorkflowConstants.ATTRIBUTE_PROCESSOR_NAME;
        protected const string PROCESSOR_MODE_DEBUG = WorkflowConstants.ATTRIBUTE_DEBUG;
        protected const string PROCESSOR_MODE_VERBOSE = WorkflowConstants.ATTRIBUTE_VERBOSE;
        protected const string PROCESSOR_MODE_FORCESTART = WorkflowConstants.ATTRIBUTE_FORCESTART;
        protected const string WORKFLOW_NAME = "WorkflowName";
        protected const string TIMEOUT = "Timeout";


        protected WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        protected ILogger _logger;
        protected List<string> _required_attributes = new List<string>()
        { WORKFLOW_NAME, CONNECTION_STRING, TIMEOUT, PROCESSOR_NAME,PROCESSOR_MODE_DEBUG,PROCESSOR_MODE_VERBOSE,PROCESSOR_MODE_FORCESTART};


        public IEnumerable<string> RequiredAttributes
        {
            get { return _required_attributes; }
        }

        public virtual void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            if (_required_attributes.Count != args.RequiredAttributes.Count)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }


            foreach (var attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Key, StringComparer.InvariantCultureIgnoreCase))
                    _attributes.Add(attribute.Key, attribute.Value);
            }

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING]);
            _logger.Debug("Controller: {Server}.{Database}", builder.DataSource, builder.InitialCatalog);
            _logger.Debug("Workflow: {WF}", _attributes[WORKFLOW_NAME]);
            _logger.Debug("Mode: ProcessorName:{ProcessorName}, Debug:{Debug}, Verbose:{Verbose}, Forcestart:{Forstart}"
                , _attributes[PROCESSOR_NAME], _attributes[PROCESSOR_MODE_DEBUG], _attributes[PROCESSOR_MODE_VERBOSE], _attributes[PROCESSOR_MODE_FORCESTART]);

        }

        public virtual WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            try
            {
                WorkflowAttributeCollection attributes = new WorkflowAttributeCollection();
                attributes.Add(PROCESSOR_NAME, _attributes[PROCESSOR_NAME]);
                attributes.Add(PROCESSOR_MODE_DEBUG, _attributes[PROCESSOR_MODE_DEBUG]);
                attributes.Add(PROCESSOR_MODE_VERBOSE, _attributes[PROCESSOR_MODE_VERBOSE]);
                attributes.Add(PROCESSOR_MODE_FORCESTART, _attributes[PROCESSOR_MODE_FORCESTART]);
                attributes.Add(WorkflowConstants.ATTRIBUTE_WORKFLOW_NAME, _attributes[WORKFLOW_NAME]);
                attributes.Add(CONNECTION_STRING, _attributes[CONNECTION_STRING]);

                WorkflowProcessor wfp = new WorkflowProcessor(attributes);
                result = wfp.Run(token);
                _logger.Information("Activity finished with result {WfStatus}: {Message}", result.StatusCode, result.Message);

            }
            catch (SqlException ex)
            {
                throw ex;
                //_logger.Write(String.Format("SqlServer exception: {0}", ex.Message));
                //result = WfResult.Create(WfStatus.Failed, ex.Message, ex.ErrorCode);
            }
            return result;
        }

    }
}
