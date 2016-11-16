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
using System.Data;
using System.Data.SqlClient;

using ControllerRuntime;
using WorkflowConsoleRunner;


namespace DefaultActivities
{
    /// <summary>
    /// DeltaExtractor activity
    /// current version uses console process to execute deltaextractor.exe
    /// deltaextractor.dll can be used instead as in-process invocation
    /// </summary>
    class DeltaExtractorActivity : IWorkflowActivity
    {
        //External Attributes
        private const string DE_PATH = "DEPath";
        private const string CONNECTION_STRING = "ConnectionString";
        private const string TIMEOUT = "Timeout";
        //For DE Parameter call
        private const string BATCH_ID = "@BatchID";
        private const string STEP_ID = "@StepID";
        private const string RUN_ID = "@RunID";


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
        private string _de_parameters;
        private string _de_path;
        private string _connection_string;
        private int _timeout;

        private IWorkflowLogger _logger;
        private Dictionary<string, string> _attributes = new Dictionary<string, string>();
        private List<string> _required_attributes = new List<string>() { DE_PATH, CONNECTION_STRING, TIMEOUT, BATCH_ID, STEP_ID, RUN_ID };

        #region IWorkflowActivity
        public string[] RequiredAttributes
        {
            get { return _required_attributes.ToArray(); }
        }

        public void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            //Default Validations
            if (_required_attributes.Count != args.RequiredAttributes.Length)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new Exception("Not all required attributes are provided");
            }


            foreach (WorkflowAttribute attribute in args.RequiredAttributes)
            {
                _attributes.Add(attribute.Name, attribute.Value);
                if (_required_attributes.Contains(attribute.Name))
                {
                    switch (attribute.Name)
                    {
                        case DE_PATH:
                            _de_path = attribute.Value;
                            break;
                        case CONNECTION_STRING:
                            _connection_string = attribute.Value;
                            break;
                        case TIMEOUT:
                            _timeout = Int32.Parse(attribute.Value);
                            break;
                        default:
                            break;
                    }
                }
            }

            DeParameters();

            _logger.Write(String.Format("DE: {0}, Conn: {1}", _attributes[DE_PATH], _attributes[CONNECTION_STRING]));
            _logger.WriteDebug(String.Format("DE Command: {0}", _de_parameters));

        }

        public WfResult Run()
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            using (ConsoleRunner p = new ConsoleRunner())
            {
                p.Timeout = Int32.Parse(_attributes[TIMEOUT]);
                p.haveOutput += delegate(object sender, HaveOutputEventArgs e)
                {
                    _logger.WriteDebug(e.Output);
                };

                int ret = p.Execute(_attributes[DE_PATH], _de_parameters);
                result = (ret == 0) ? WfResult.Succeeded : WfResult.Failed;
            }

            return result;
        }
        public void Cancel()
        {
            return;
        }
        #endregion

        private void DeParameters()
        {
            
            StringBuilder sb = new StringBuilder(XML_HEADER);
            SqlConnection cn = new SqlConnection(_connection_string);
            string cmd_text = String.Format(DE_PARAMETER_QUERY,
                _attributes[BATCH_ID],
                _attributes[STEP_ID],
                _attributes[RUN_ID],
                ((_logger.Mode)? 1 : 0));
            try
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(cmd_text, cn))
                {
                    cmd.CommandTimeout = 30;
                    sb.Append(cmd.ExecuteScalar().ToString());
                    sb.Replace("\"","\\\"");
                    sb.Insert(0, '\"').Append('\"');
                    _de_parameters = sb.ToString();
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
