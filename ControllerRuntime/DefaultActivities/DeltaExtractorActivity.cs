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
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using ControllerRuntime;
using BIAS.Framework.DeltaExtractor;
using Serilog;


namespace DefaultActivities
{
    /// <summary>
    /// DeltaExtractor activity
    /// current version uses console process to execute deltaextractor.exe
    /// deltaextractor.dll can be used instead as in-process invocation
    /// </summary>
    public class DeltaExtractorActivity : IWorkflowActivity
    {
        //External Attributes
        private const string CONNECTION_STRING = WorkflowConstants.ATTRIBUTE_CONTROLLER_CONNECTIONSTRING;

        //For DE Parameter call
        private const string BATCH_ID = WorkflowConstants.ATTRIBUTE_BATCH_ID;
        private const string STEP_ID = WorkflowConstants.ATTRIBUTE_STEP_ID;
        private const string RUN_ID = WorkflowConstants.ATTRIBUTE_RUN_ID;


        //Require validation

        private ILogger _logger;
        private WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        private List<string> _required_attributes = new List<string>() { CONNECTION_STRING, BATCH_ID, STEP_ID, RUN_ID };

        #region IWorkflowActivity
        public IEnumerable<string> RequiredAttributes
        {
            get { return _required_attributes; }
        }

        public void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            _logger.Debug("In Delta Extractor Configure method...");

            //Default Validations
            if (_required_attributes.Count != args.RequiredAttributes.Count)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }

            foreach (var attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Key, StringComparer.InvariantCultureIgnoreCase))
                {
                    _attributes.Add(attribute.Key, attribute.Value);
                }
            }

            //obfuscate the password
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING]);
            _logger.Debug("Controller: {Server}.{Database}", builder.DataSource, builder.InitialCatalog);
        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            int processId = Process.GetCurrentProcess().Id;
            _logger.Debug("Host process Id: {ProcessId}", processId);
            _logger.Debug("Running DeltaExtractor...");

            DERun runner = new DERun(_logger);
            return runner.Start(_attributes,token);
        }
        #endregion

    }
}
