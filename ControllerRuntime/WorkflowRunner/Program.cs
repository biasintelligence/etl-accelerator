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

using ControllerRuntime;

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
            if (args.Contains(@"/D", StringComparer.InvariantCultureIgnoreCase))
                options.Add("debug");

            if (args.Contains(@"/R", StringComparer.InvariantCultureIgnoreCase))
                options.Add("forcestart");

            if (args.Contains(@"/V", StringComparer.InvariantCultureIgnoreCase))
                options.Add("verbose");


            var settings = ConfigurationManager.AppSettings;
            string connectionString = settings["Controller"];
            if (String.IsNullOrEmpty(connectionString))
                connectionString = @"Server=localhost;Database=etl_controller;Trusted_Connection=True;Connection Timeout=120;"; ;

            string runnerName = settings["Runner"];
            if (String.IsNullOrEmpty(runnerName))
                runnerName = "Default";

            try
            {
                WorkflowProcessor wfp = new WorkflowProcessor(runnerName);
                wfp.ConnectionString = connectionString;
                wfp.WorkflowName = args[0].Replace("\"", "");
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
