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

namespace DefaultActivities
{
    /// <summary>
    /// Thread.Sleep logic
    /// </summary>
    class WaitActivity : IWorkflowActivity
    {
        private const string TIMEOUT = "Timeout";

        private WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        private ILogger _logger;
        private List<string> _required_attributes = new List<string>() { TIMEOUT };

        private TimeSpan _timeout;

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
                if (_required_attributes.Contains(attribute.Key, StringComparer.InvariantCultureIgnoreCase))
                    _attributes.Add(attribute.Key, attribute.Value);
            }

            _timeout = TimeSpan.FromSeconds(Int32.Parse(_attributes[TIMEOUT]));
            _logger.Information("Wait Timeout: {Timeout}", _attributes[TIMEOUT]);

        }

        public WfResult Run(CancellationToken token)
        {

            Task.Delay(_timeout, token).Wait();
            return WfResult.Succeeded;
        }
    }
}
