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

namespace DefaultActivities
{
    /// <summary>
    /// Thread.Sleep logic
    /// </summary>
    class WaitActivity : IWorkflowActivity
    {
        private const string TIMEOUT = "Timeout";

        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private IWorkflowLogger _logger;
        private List<string> _required_attributes = new List<string>() { TIMEOUT };

        private TimeSpan _timeout;

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

            _timeout = TimeSpan.FromSeconds(Int32.Parse(_attributes[TIMEOUT]));
            _logger.Write(String.Format("Wait Timeout: {0}", _attributes[TIMEOUT]));

        }

        public WfResult Run(CancellationToken token)
        {

            Task.Delay(_timeout, token).Wait();
            return WfResult.Succeeded;
        }
    }
}
