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


        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>() { APP_NAME, APP_ARGS, TIMEOUT };

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
                throw new Exception("Not all required attributes are provided");
            }


            foreach (WorkflowAttribute attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Name, StringComparer.InvariantCultureIgnoreCase))
                    _attributes.Add(attribute.Name, attribute.Value);
            }



            _logger.Write(String.Format("console: {0} {1}", _attributes[APP_NAME], _attributes[APP_ARGS]));

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
                    _logger.WriteDebug(e.Output);
                };

                int ret = p.Execute(_attributes[APP_NAME], _attributes[APP_ARGS]);
                result = (ret == 0) ? WfResult.Succeeded : WfResult.Failed;
            }

            return result;
        }
        #endregion
    }

}
