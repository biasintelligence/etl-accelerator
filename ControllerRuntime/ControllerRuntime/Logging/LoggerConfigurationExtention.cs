using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace ControllerRuntime.Logging
{
    public static class LoggerConfigurationExtention
    {
        public static LoggerConfiguration WorkflowLogger(
               this LoggerSinkConfiguration loggerConfiguration,
               IFormatProvider formatProvider = null,
               string connectionString = null)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException("loggerConfiguration");
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            return loggerConfiguration.Sink(new WorkflowLogger(connectionString, formatProvider));
        }
    }
}
