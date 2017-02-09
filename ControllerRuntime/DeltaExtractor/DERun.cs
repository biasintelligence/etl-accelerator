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

using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    public class DERun : IWorkflowRunner
    {
        private IWorkflowLogger _logger;
        private WorkflowActivityParameters _args;

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

        private const string CONNECTION_STRING = "ConnectionString";
        private const string BATCH_ID = "@BatchId";
        private const string STEP_ID = "@StepId";
        private const string RUN_ID = "@RunId";
        private const string XML = "XML";

        public WfResult Start(WorkflowActivityParameters args, IWorkflowLogger logger)
        {
            WfResult result = WfResult.Succeeded;
            _logger = logger;
            _args = args;

            Parameters parameters = new Parameters();

            try
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                string inputXml;
                if (_args.KeyList.AllKeys.Contains(XML))
                    inputXml = _args.Get(XML);
                else
                    inputXml = DeParameters();

                if (!inputXml.StartsWith("<?xml version="))
                {
                    byte[] base64ByteArr = Convert.FromBase64String(XML);
                    inputXml = System.Text.UnicodeEncoding.Unicode.GetString(base64ByteArr);
                }

                parameters = Parameters.DeSerializefromXml(inputXml);

                logger.Write(String.Format(CultureInfo.InvariantCulture, "Running DE v.{0} ({1})bit", v.ToString(), 8 * IntPtr.Size));
                logger.Write("Executing as: " + WindowsIdentity.GetCurrent().Name.ToString());

                logger.Write("DE XML: " + inputXml);

                DEController controller = new DEController();
                controller.Execute(parameters, logger);

            }
            catch (Exception ex)
            {
                logger.WriteError(ex.Message, ex.HResult);
                result = WfResult.Failed;
            }

            return result;
        }

        private string DeParameters()
        {

            StringBuilder sb = new StringBuilder(XML_HEADER);
            string cmd_text = String.Format(DE_PARAMETER_QUERY,
            _args.Get(BATCH_ID),
            _args.Get(STEP_ID),
            _args.Get(RUN_ID),
            ((_logger.Mode) ? 1 : 0));

            using (SqlConnection cn = new SqlConnection(_args.Get(CONNECTION_STRING)))
            using (SqlCommand cmd = new SqlCommand(cmd_text, cn))
            {
                try
                {
                    cn.Open();
                    cmd.CommandTimeout = 30;
                    sb.Append(cmd.ExecuteScalar().ToString());
                    //sb.Replace("\"","\\\"");
                    //sb.Insert(0, '\"').Append('\"');

                    _logger.WriteDebug(String.Format("CommandText : {0}", cmd.CommandText));
                    _logger.WriteDebug(String.Format("sb : {0}", sb.ToString()));

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
