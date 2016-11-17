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
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using ControllerRuntime;
using WorkflowLibraryRunner;
using BIAS.Framework.DeltaExtractor;

namespace DefaultActivities
{
    /// <summary>
    /// DeltaExtractor activity
    /// current version uses console process to execute deltaextractor.exe
    /// deltaextractor.dll can be used instead as in-process invocation
    /// </summary>
    public class DeltaExtractorActivity : IWorkflowActivity
    {
        //External Attributes
        private const string CONNECTION_STRING = "ConnectionString";
        private const string XMLParameter = "XML";

        //For DE Parameter call
        private const string BATCH_ID = "@BatchID";
        private const string STEP_ID = "@StepID";
        private const string RUN_ID = "@RunID";


        private string _connection_string;


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

        //Require validation
        private WorkflowActivityParameters _parameters = WorkflowActivityParameters.Create();

        private IWorkflowLogger _logger;
        private Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        //private List<string> _required_attributes = new List<string>() { CONNECTION_STRING, BATCH_ID, STEP_ID, RUN_ID };
        private List<string> _required_attributes = new List<string>() { BATCH_ID, STEP_ID, RUN_ID };

        #region IWorkflowActivity
        public string[] RequiredAttributes
        {
            get { return _required_attributes.ToArray(); }
        }

        public void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            _logger.WriteDebug("In Delta Extractor Configure method...");

            //Default Validations
            if (_required_attributes.Count != args.RequiredAttributes.Length)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new Exception("Not all required attributes are provided");
            }

            foreach (WorkflowAttribute attribute in args.RequiredAttributes)
            {
                _attributes.Add(attribute.Name, attribute.Value);
                if (_required_attributes.Contains(attribute.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    switch (attribute.Name)
                    {
                        case CONNECTION_STRING:
                            _connection_string = attribute.Value;

                            break;
                        default:
                            break;
                    }
                }
            }

            DeParameters();

            //_logger.Write(String.Format("Conn: {1}", _attributes[CONNECTION_STRING]));
            _logger.WriteDebug(String.Format("DE Command: {0}", _parameters.Get(XMLParameter)));

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            _logger.WriteDebug("Running DeltaExtractor...");

            using (LibraryRunner libraryRunner = new LibraryRunner())
            {
                libraryRunner.haveOutput += delegate (object sender, HaveOutputEventArgs e)
                {
                    _logger.WriteDebug(e.Output);
                };

                result = libraryRunner.Execute(new DERun(), _parameters, _logger);
            }

            return result;
        }
        #endregion

        private void DeParameters()
        {
            var settings = ConfigurationManager.AppSettings;
            _connection_string = settings["Controller"];

            StringBuilder sb = new StringBuilder(XML_HEADER);
            SqlConnection cn = new SqlConnection(_connection_string);
            string cmd_text = String.Format(DE_PARAMETER_QUERY,
                _attributes[BATCH_ID],
                _attributes[STEP_ID],
                _attributes[RUN_ID],
                ((_logger.Mode) ? 1 : 0));
            try
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(cmd_text, cn))
                {
                    cmd.CommandTimeout = 30;
                    sb.Append(cmd.ExecuteScalar().ToString());
                    //sb.Replace("\"","\\\"");
                    //sb.Insert(0, '\"').Append('\"');

                    _logger.WriteDebug(String.Format("CommandText : {0}", cmd.CommandText));
                    _logger.WriteDebug(String.Format("sb : {0}", sb.ToString()));

                    if (_parameters == null)
                    {
                        _logger.WriteDebug("_parameters is null!");
                    }

                    string s = sb.ToString();

                    _parameters.Add(XMLParameter, sb.ToString());
                }
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
