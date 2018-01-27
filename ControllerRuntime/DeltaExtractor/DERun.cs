using System;
using System.Globalization;
using System.Security.Principal;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Serialization;
using System.IO;
using System.Threading;

using Serilog;
using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    public class DERun : IWorkflowRunner
    {
        private ILogger _logger;
        private WorkflowAttributeCollection _args;

        private const string DE_PARAMETER_QUERY = @"
            declare @pHeader xml;
            declare @pContext xml;
            declare @pProcessRequest xml;
            declare @pParameters xml;
            exec dbo.prc_CreateHeader @pHeader out,{0},{1},null,{2},{3},15;
            exec dbo.prc_CreateContext @pContext out,@pHeader;
            exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext;
            exec dbo.prc_de_CreateParameters @pParameters out, @pProcessRequest;
            select cast(@pParameters as nvarchar(max));
            ";

        private const string XML_HEADER = "<?xml version=\"1.0\"?>";
        private const string XML = "XML";

        public WfResult Start(WorkflowAttributeCollection args, ILogger logger, CancellationToken token)
        {
            WfResult result = WfResult.Succeeded;
            _logger = logger;
            _args = args;

            Parameters parameters = new Parameters();

            try
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                string inputXml;
                if (_args.ContainsKey(XML))
                    inputXml = _args[XML];
                else
                    inputXml = DeParameters();

                if (!inputXml.StartsWith("<?xml version="))
                {
                    byte[] base64ByteArr = Convert.FromBase64String(XML);
                    inputXml = System.Text.UnicodeEncoding.Unicode.GetString(base64ByteArr);
                }

                parameters = Parameters.DeSerializefromXml(inputXml);

                logger.Information("Running DE v.{Version} ({Bit} bit)", v.ToString(), 8 * IntPtr.Size);
                logger.Information("Executing as: {User}", WindowsIdentity.GetCurrent().Name.ToString());
                logger.Debug("DE input parameter: {DeXml}", inputXml);

                //logger.WriteDebug("DE XML: " + inputXml);

                DEController controller = new DEController();
                controller.Execute(parameters, logger, token);

            }
            catch (Exception ex)
            {
                logger.Error(ex,"Exception: {Message}",ex.Message);
                result = WfResult.Failed;
            }

            return result;
        }

        private string DeParameters()
        {

            StringBuilder sb = new StringBuilder(XML_HEADER);
            string cmd_text = String.Format(DE_PARAMETER_QUERY,
            _args[WorkflowConstants.ATTRIBUTE_BATCH_ID],
            _args[WorkflowConstants.ATTRIBUTE_STEP_ID],
            _args[WorkflowConstants.ATTRIBUTE_RUN_ID],
            (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug)) ? 1 : 0);

            using (SqlConnection cn = new SqlConnection(_args[WorkflowConstants.ATTRIBUTE_CONTROLLER_CONNECTIONSTRING]))
            using (SqlCommand cmd = new SqlCommand(cmd_text, cn))
            {
                try
                {
                    cn.Open();
                    cmd.CommandTimeout = 30;
                    sb.Append(cmd.ExecuteScalar().ToString());
                    //sb.Replace("\"","\\\"");
                    //sb.Insert(0, '\"').Append('\"');

                    //_logger.Debug("DE Parameter : {Xml}", sb.ToString());
                    //_logger.WriteDebug(String.Format("sb : {0}", sb.ToString()));

                    return sb.ToString();
                }
                catch (SqlException ex)
                {
                    throw ex;
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();
                }
            }
        }

    }
}
