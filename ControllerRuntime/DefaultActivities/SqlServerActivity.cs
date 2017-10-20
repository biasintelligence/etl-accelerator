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
*******************************************************************
    6/7/2017        andrey              add with nowait to command InfoMessage

 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

using ControllerRuntime;

namespace DefaultActivities
{
    /// <summary>
    /// Exceute SqlClient ExecuteNonQuery function 
    /// </summary>
    public class SqlServerActivity : IWorkflowActivity
    {
        protected const string CONNECTION_STRING = "ConnectionString";
        protected const string QUERY_STRING = "Query";
        protected const string TIMEOUT = "Timeout";


        protected Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        protected IWorkflowLogger _logger;
        protected List<string> _required_attributes = new List<string>() { QUERY_STRING, CONNECTION_STRING, TIMEOUT };


        public string[] RequiredAttributes
        {
            get { return _required_attributes.ToArray(); }
        }

        public virtual void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            if (_required_attributes.Count != args.RequiredAttributes.Length)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }


            foreach (WorkflowAttribute attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Name, StringComparer.InvariantCultureIgnoreCase))
                    _attributes.Add(attribute.Name, attribute.Value);
            }

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING]);
            _logger.WriteDebug(String.Format("SqlServer: {0}.{1}", builder.DataSource,builder.InitialCatalog));
            _logger.WriteDebug(String.Format("Query: {0}", _attributes[QUERY_STRING]));

        }

        public virtual WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            using (SqlConnection cn = new SqlConnection(_attributes[CONNECTION_STRING]))
            {
                try
                {
                    cn.InfoMessage += new SqlInfoMessageEventHandler(OnInfoMessage);
                    cn.FireInfoMessageEventOnUserErrors = false;
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(_attributes[QUERY_STRING], cn))
                    {
                        cmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);
                        using (token.Register(cmd.Cancel))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        result = WfResult.Succeeded;
                    }
                }
                catch (SqlException ex)
                {
                    throw ex;
                    //_logger.Write(String.Format("SqlServer exception: {0}", ex.Message));
                    //result = WfResult.Create(WfStatus.Failed, ex.Message, ex.ErrorCode);
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();

                }
            }
            return result;
        }
        protected void OnInfoMessage(
          object sender, SqlInfoMessageEventArgs args)
        {
            _logger.Write(args.Message);
            //if ((args.Errors.Count) == 0)
            //    return;

            //foreach (SqlError err in args.Errors)
            //{
            //    _logger.Write(String.Format(
            //  "source: {0}, severity: {1}, state: {2} error: {3}\n" +
            //  "line: {4}, procedure: {5}, server: {6}:\n{7}",
            //   err.Source, err.Class, err.State, err.Number, err.LineNumber,
            //   err.Procedure, err.Server, err.Message));
            //}

        }

    }
}
