using System;
using System.Globalization;
using System.Security.Principal;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Serialization;
using System.IO;

using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    public class DERun : IWorkflowRunner
    {

        //private static string m_XSDPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\Parameters.xsd";
        private bool debug = false;
        private bool verbose = false;
        private const string XMLParameter = "XML";

        public WfResult Start(WorkflowActivityParameters args, IWorkflowLogger logger)
        {
            WfResult result = WfResult.Succeeded;

            Parameters parameters = new Parameters();

            try
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string XML = args.Get(XMLParameter);
                if (!XML.StartsWith("<?xml version="))
                {
                    byte[] base64ByteArr = Convert.FromBase64String(args.Get(XMLParameter));
                    XML = System.Text.UnicodeEncoding.Unicode.GetString(base64ByteArr);
                }

                parameters = Parameters.DeSerializefromXml(XML);
                //Enable ETLController loging
                //EventLoggerDE de = EventLoggerDE.Create();
                debug = parameters.Debug;

                if (parameters.HeaderType == HeaderType.ETLHeader)
                {
                    //de.SetEventContext(new Dictionary<string, string>() 
                    //{
                    //    {"CS",String.Format(@"Data Source={0};Initial Catalog={1};Integrated Security=SSPI;",parameters.ETLHeader.Controller.Server,parameters.ETLHeader.Controller.Database)},
                    //    {"Timeout",parameters.ETLHeader.Controller.QueryTimeout.ToString()},
                    //    {"prcPrint", "dbo.prc_Print"}
                    //});

                    //ETLController.Connect(parameters.ETLHeader);
                }

                logger.Write(String.Format(CultureInfo.InvariantCulture, "Running DE v.{0} ({1})bit", v.ToString(), 8 * IntPtr.Size));
                logger.Write("Executing as: " + WindowsIdentity.GetCurrent().Name.ToString());

                logger.Write("DE XML: " + XML);

                DEController controller = new DEController();
                controller.Debug = Debug;
                controller.Execute(parameters, logger);

            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message, ex.HResult);
                result = WfResult.Failed;
            }

            return result;
        }
        public static void DisplayHelp(IWorkflowLogger logger)
        {
            //::TODO:: Add help output for new parameters

            logger.Write("DeltaExtractor - created by v-andrsh");
            logger.Write("Pulls data from database/file and pushes to multiple database/file destinations.");
            logger.Write("---------------------------------------------------------------");
            logger.Write("DeltaExtractor.exe <XML>");
            logger.Write("See Parameters.xsd for XML schema definition.");
        }

        public bool Debug
        { get { return debug; } }

        public bool Verbose
        { get { return verbose; } }
    }
}
