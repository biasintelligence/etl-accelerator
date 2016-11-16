using System;
using System.Globalization;
using System.Security.Principal;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Serialization;
using System.IO;

namespace BIAS.Framework.DeltaExtractor
{
    class DERun
    {

        //private static string m_XSDPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\Parameters.xsd";
        private static bool debug = false;
        private static bool verbose = false;

        /// <summary>
        /// Entry point to CDR Delta Extractor. Takes parameters defined by an XML string based
        /// on the parameters.XSD schema.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static void Main(string[] args)
        {

            Parameters parameters = new Parameters();

            try
            {

                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string XML = args[0];
                if (!XML.StartsWith("<?xml version="))
                {
                    byte[] base64ByteArr = Convert.FromBase64String(args[0]);
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

                    ETLController.Connect(parameters.ETLHeader);
                }

                PrintOutput.PrintToOutput(String.Format(CultureInfo.InvariantCulture, "Running DE v.{0} ({1})bit", v.ToString(), 8 * IntPtr.Size));
                PrintOutput.PrintToOutput("Executing as: " + WindowsIdentity.GetCurrent().Name.ToString(), DERun.Debug);

                PrintOutput.PrintToOutput("DE XML: " + XML, DERun.Debug);


                DEController.Execute(parameters);

            }
            catch (Exception ex)
            {
                PrintOutput.PrintToError(ex.Message);
                Environment.Exit(1);
            }
            Environment.Exit(0);
        }
        public static void DisplayHelp()
        {
            //::TODO:: Add help output for new parameters

            PrintOutput.PrintToOutput("DeltaExtractor - created by andrey@biasintelligence.com");
            PrintOutput.PrintToOutput("Pulls data from database/file and pushes to multiple database/file destinations.");
            PrintOutput.PrintToOutput("---------------------------------------------------------------");
            PrintOutput.PrintToOutput("DeltaExtractor.exe <XML>");
            PrintOutput.PrintToOutput("See Parameters.xsd for XML schema definition.");
        }

        public static bool Debug
        { get { return debug; } }

        public static bool Verbose
        { get { return verbose; } }
    }
}
