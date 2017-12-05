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
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Core;

namespace ControllerRuntime.Logging
{
    public class WorkflowLogger : ILogEventSink

    {

        private readonly IFormatProvider _formatProvider = null;
        private ControllerLogger _controllerLogger;

        public WorkflowLogger(
            string connectionString,
            IFormatProvider formatProvider = null)
        {

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("connectionString");
            }

            _formatProvider = formatProvider;
            //if (String.IsNullOrEmpty(connectionString))
            //    connectionString = ConfigurationManager.AppSettings["Controller"];

            _controllerLogger = new ControllerLogger(connectionString);
        }

        public void Emit(LogEvent logEvent)
        {
            string message;

            int err = 0;
            if (logEvent.Exception != null)
            {
                err = logEvent.Exception.HResult;
                message = string.Format("{0} -- EXCEPTION: {1}", logEvent.RenderMessage(_formatProvider), logEvent.Exception.Message);
            }
            else
            {
                err = GetIntValue(logEvent, "ErrorCode", 0);
                message = logEvent.RenderMessage(_formatProvider);
            }


            int wfId = GetIntValue(logEvent, "WorkflowId", 0);
            int stepId = GetIntValue(logEvent, "StepId", 0);
            int runId = GetIntValue(logEvent, "RunId", 0);
            _controllerLogger.LogToController(message, err,wfId,stepId,runId);

        }

        private static int GetIntValue(LogEvent logEvent,string property, int defaultValue = 0)
        {
            LogEventPropertyValue prop;
            int value = defaultValue;
            if (logEvent.Properties.TryGetValue(property, out prop))
            {
                if (!Int32.TryParse(prop.ToString(), out value))
                {
                    value = defaultValue;
                }
            }
            return value;
        }

    }

}
