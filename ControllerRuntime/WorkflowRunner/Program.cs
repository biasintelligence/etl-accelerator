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
using System.Threading.Tasks;
using System.Configuration;

using Serilog;
using Serilog.Events;
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

            List<string> options = new List<string>();

            var minLogLevel = LogEventLevel.Information;
            //bool debug = false;
            if (args.Contains(@"/D", StringComparer.InvariantCultureIgnoreCase))
            {
                options.Add("debug");
                //debug = true;
                minLogLevel = LogEventLevel.Debug;
            }

            //bool forcestart = false;
            if (args.Contains(@"/R", StringComparer.InvariantCultureIgnoreCase))
            {
                options.Add("forcestart");
                //forcestart = true;
            }

            bool verbose = false;
            if (args.Contains(@"/V", StringComparer.InvariantCultureIgnoreCase))
            {
                options.Add("verbose");
                verbose = true;
            }


            var settings = ConfigurationManager.AppSettings;
            string connectionString = settings["Controller"];

            string runnerName = settings["Runner"];
            if (String.IsNullOrEmpty(runnerName))
                runnerName = "Default";



            if (verbose)
                Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Is(minLogLevel)
                        .WriteTo.Console()
                        .WriteTo.WorkflowLogger(connectionString: connectionString)
                        .CreateLogger();
            else
                Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Is(minLogLevel)
                        .WriteTo.WorkflowLogger(connectionString: connectionString)
                        .CreateLogger();

            try
            {
                WorkflowProcessor wfp = new WorkflowProcessor(runnerName);
                wfp.WorkflowName = args[0].Replace("\"", "");
                wfp.ConnectionString = connectionString;
                WfResult wr = wfp.Run(options.ToArray());
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
            Console.WriteLine (@"Usage: runner <Name> /D /R /V");
            Console.WriteLine (@"Options:");
            Console.WriteLine (@"   /D - debug mode");
            Console.WriteLine (@"   /R - force restart");
            Console.WriteLine (@"   /V - console output");
        }

    }
}
