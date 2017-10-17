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
using System.Data;
using System.Data.SqlClient;
using ControllerRuntime;
using BIAS.Framework.DeltaExtractor;

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
        private const string CONNECTION_STRING = "ConnectionString";

        //For DE Parameter call
        private const string BATCH_ID = "@BatchID";
        private const string STEP_ID = "@StepID";
        private const string RUN_ID = "@RunID";


        //Require validation
        private WorkflowActivityParameters _parameters = WorkflowActivityParameters.Create();

        private IWorkflowLogger _logger;
        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private List<string> _required_attributes = new List<string>() { CONNECTION_STRING, BATCH_ID, STEP_ID, RUN_ID };

        #region IWorkflowActivity
        public string[] RequiredAttributes
        {
            get { return _required_attributes.ToArray(); }
        }

        public void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            _logger.WriteDebug("In Delta Extractor Configure method...");

            //Default Validations
            if (_required_attributes.Count != args.RequiredAttributes.Length)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }

            foreach (WorkflowAttribute attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    _attributes.Add(attribute.Name, attribute.Value);
                    _parameters.Add(attribute.Name, attribute.Value);
                }
            }

            //obfuscate the password
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING]);
            if (!String.IsNullOrEmpty(builder.Password))
                builder.Password = "********";

            _logger.WriteDebug(String.Format("Conn: {0}", builder.ConnectionString));

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            _logger.WriteDebug("Running DeltaExtractor...");

            DERun runner = new DERun();
            return runner.Start(_parameters,_logger);
        }
        #endregion

    }
}
