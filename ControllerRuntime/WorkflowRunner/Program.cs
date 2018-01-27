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
using System.Configuration;

using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Configuration;
using ControllerRuntime;
using ControllerRuntime.Logging;


namespace WorkflowRunner
{
    /// <summary>
    /// Workflow Console
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {

            if (args.Length == 0
                || args.Contains(@"/help", StringComparer.InvariantCultureIgnoreCase))
            {
                help();
                return 0;
            }

            WorkflowAttributeCollection attributes = new WorkflowAttributeCollection();
            attributes.Add(WorkflowConstants.ATTRIBUTE_WORKFLOW_NAME, args[0].Replace("\"", ""));
            attributes.Add(WorkflowConstants.ATTRIBUTE_DEBUG, "false");
            attributes.Add(WorkflowConstants.ATTRIBUTE_VERBOSE, "false");
            attributes.Add(WorkflowConstants.ATTRIBUTE_FORCESTART, "false");

            var minLogLevel = LogEventLevel.Information;
            //bool debug = false;
            if (args.Contains(@"/D", StringComparer.InvariantCultureIgnoreCase))
            {
                attributes[WorkflowConstants.ATTRIBUTE_DEBUG]= "true";
                //debug = true;
                minLogLevel = LogEventLevel.Debug;
            }

            //bool forcestart = false;
            if (args.Contains(@"/R", StringComparer.InvariantCultureIgnoreCase))
            {
                attributes[WorkflowConstants.ATTRIBUTE_FORCESTART] = "true";
                //forcestart = true;
            }

            bool verbose = false;
            if (args.Contains(@"/V", StringComparer.InvariantCultureIgnoreCase))
            {
                attributes[WorkflowConstants.ATTRIBUTE_VERBOSE] = "true";
                verbose = true;
            }


            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string runnerName = builder.GetSection("Data:Runner").Value;
            string connectionString = builder.GetSection("Data:Controller").Value;

            //var settings = ConfigurationManager.AppSettings;
            //string connectionString = settings["Controller"];

            attributes.Add(WorkflowConstants.ATTRIBUTE_CONTROLLER_CONNECTIONSTRING, connectionString);


            //string runnerName = settings["Runner"];
            if (String.IsNullOrEmpty(runnerName))
                runnerName = "Default";

            attributes.Add(WorkflowConstants.ATTRIBUTE_PROCESSOR_NAME, runnerName);


            if (verbose)
                Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(builder)
                        .MinimumLevel.Is(minLogLevel)
                        //.WriteTo.Console()
                        .WriteTo.WorkflowLogger(connectionString: connectionString)
                        .CreateLogger();
            else
                Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Is(minLogLevel)
                        .WriteTo.WorkflowLogger(connectionString: connectionString)
                        .CreateLogger();

            try
            {
                WfResult wr = WfResult.Unknown;
                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    WorkflowProcessor wfp = new WorkflowProcessor(attributes);
                    wr = wfp.Run(cts.Token);
                }

                if (wr.StatusCode != WfStatus.Succeeded)
                    return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine (String.Format("Error: {0}, {1}",ex.HResult,ex.Message));
                return 1;
            }
            return 0;           
        }

        private static void help()
        {
            Console.WriteLine (@"Usage: runner <Name> /D /R /V /F");
            Console.WriteLine (@"Options:");
            Console.WriteLine (@"   /D - debug mode");
            Console.WriteLine (@"   /R - force restart");
            Console.WriteLine(@"   /V - appsettings sinks output");
        }

    }
}
