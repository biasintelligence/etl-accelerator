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

using Serilog;
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


        protected WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        protected ILogger _logger;
        protected List<string> _required_attributes = new List<string>() { QUERY_STRING, CONNECTION_STRING, TIMEOUT };


        public IEnumerable<string> RequiredAttributes
        {
            get { return _required_attributes; }
        }

        public virtual void Configure(WorkflowActivityArgs args)
        {
            _logger = args.Logger;

            if (_required_attributes.Count != args.RequiredAttributes.Count)
            {
                //_logger.WriteError(String.Format("Not all required attributes are provided"), -11);
                throw new ArgumentException("Not all required attributes are provided");
            }


            foreach (var attribute in args.RequiredAttributes)
            {
                if (_required_attributes.Contains(attribute.Key))
                    _attributes.Add(attribute.Key, attribute.Value);
            }

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING]);
            _logger.Debug("SqlServer: {Server}.{Database}", builder.DataSource,builder.InitialCatalog);
            _logger.Debug("Query: {Query}", _attributes[QUERY_STRING]);

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
                    _logger.Error(ex,"SqlServer exception {ErrorCode}: {Message}", ex.Number,ex.Message);
                    result = WfResult.Failed;
                    //throw ex;
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
            _logger.Information(args.Message);
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
