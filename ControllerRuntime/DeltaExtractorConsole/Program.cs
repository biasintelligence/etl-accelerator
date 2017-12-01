/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     02/08/2016
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
using ControllerRuntime.Logging;
using Serilog;
using Serilog.Events;

namespace BIAS.Framework.DeltaExtractor.exe
{
    class Program
    {

        /// <summary>
        /// Entry point to Delta Extractor. Takes parameters defined by an XML string based
        /// on the parameters.XSD schema.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static void Main(string[] args)
        {

            if (args.Length == 0
                || args.Contains(@"/help", StringComparer.InvariantCultureIgnoreCase))
            {
                help();
                Environment.Exit(0);
            }

            bool debug = args.Contains(@"/D", StringComparer.InvariantCultureIgnoreCase);
            bool verbose = args.Contains(@"/V", StringComparer.InvariantCultureIgnoreCase);


            try
            {
                var minLogLevel = (debug) ? LogEventLevel.Debug : LogEventLevel.Information;
                ILogger logger = new LoggerConfiguration()
                    .MinimumLevel.Is(minLogLevel)
                    .WriteTo.Console()
                    .CreateLogger();


                ;
                WorkflowActivityParameters param = WorkflowActivityParameters.Create();
                param.Add("XML", args[0]);

                DERun runner = new DERun();
                WfResult result =  runner.Start(param, logger);
                if (result.StatusCode != WfStatus.Succeeded)
                {
                    throw new Exception(String.Format("DE returned Status: {0}", result.StatusCode.ToString()));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
            Environment.Exit(0);
        }
        public static void help()
        {
            Console.WriteLine(@"Usage: de <xml> /D /V");
            Console.WriteLine(@"Options:");
            Console.WriteLine(@"   <xml> - see paremeters.xsd for details");
            Console.WriteLine(@"   /D - debug mode");
            Console.WriteLine(@"   /V - verbose output");
        }

    }
}
