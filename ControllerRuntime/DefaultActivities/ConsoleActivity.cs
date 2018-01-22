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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using ControllerRuntime;
using WorkflowConsoleRunner;


namespace DefaultActivities
{
    /// <summary>
    /// Executes console applications
    /// Example: Console = powershell, Args = remove-item file
    /// </summary>
    class ConsoleActivity : IWorkflowActivity
    {
        private const string APP_NAME = "Console";
        private const string APP_ARGS = "Arg";
        private const string TIMEOUT = "Timeout";


        private WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        private ILogger _logger;
        private List<string> _required_attributes = new List<string>() { APP_NAME, APP_ARGS, TIMEOUT };

        #region IWorkflowActivity
        public IEnumerable<string> RequiredAttributes
        {
            get { return _required_attributes; }
        }

        public void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            if (_required_attributes.Count != args.RequiredAttributes.Count)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }


            foreach (var attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Key))
                    _attributes.Add(attribute.Key, attribute.Value);
            }



            _logger.Information("console: {Exe} {Args}", _attributes[APP_NAME], _attributes[APP_ARGS]);

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            using (ConsoleRunner p = new ConsoleRunner())
            {
                p.Timeout = Int32.Parse(_attributes[TIMEOUT]);
                p.haveOutput += delegate (object sender, HaveOutputEventArgs e)
                {
                    _logger.Debug(e.Output);
                };

                using (token.Register(p.Dispose))
                {
                    int ret = p.Execute(_attributes[APP_NAME], _attributes[APP_ARGS]);
                    result = (ret == 0) ? WfResult.Succeeded : WfResult.Failed;
                }
            }

            return result;
        }
        #endregion
    }

}
