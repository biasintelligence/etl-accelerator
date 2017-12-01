/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     12/16/2016
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
*******************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;

using Serilog;
using ControllerRuntime;

namespace DefaultActivities
{
    /// <summary>
    /// Check the Workflow Event Server for an event
    /// </summary>
    public class CheckWorkflowEventActivity : IWorkflowActivity
    {
        protected const string CONNECTION_STRING = "ConnectionString";
        protected const string EVENT_TYPE = "EventType";
        protected const string WATERMARK_EVENT_TYPE = "WatermarkEventType";
        protected const string TIMEOUT = "Timeout";


        protected Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        protected ILogger _logger;
        protected List<string> _required_attributes = new List<string>() { EVENT_TYPE, CONNECTION_STRING, WATERMARK_EVENT_TYPE, TIMEOUT };


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
            _logger.Debug("EventServer: {Server}.{Database}", builder.DataSource, builder.InitialCatalog);
           _logger.Debug("EventType: {Type}", _attributes[EVENT_TYPE]);
            _logger.Debug("WatermarkEventType: {Type}", _attributes[WATERMARK_EVENT_TYPE]);

        }

        public virtual WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            using (SqlConnection cn = new SqlConnection(_attributes[CONNECTION_STRING]))
            {
                try
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.prc_CheckEventCondition", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EventType", _attributes[EVENT_TYPE]);
                        cmd.Parameters.AddWithValue("@WatermarkEventType", _attributes[WATERMARK_EVENT_TYPE]);
                        cmd.Parameters.Add("@Status", SqlDbType.Bit).Value = SqlBoolean.Null;
                        cmd.Parameters["@Status"].Direction = ParameterDirection.Output;
                        cmd.Parameters.AddWithValue("@Options", "");
                        cmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);

                        using (token.Register(cmd.Cancel))
                        {
                            cmd.ExecuteNonQuery();
                            SqlBoolean Status = (SqlBoolean)cmd.Parameters["@Status"].SqlValue;
                            if (Status.IsNull || Status.IsFalse)
                                result = WfResult.Failed;
                            else
                                result = WfResult.Succeeded;
                        }
                    }
                }
                catch (SqlException e)
                {
                    throw e;
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();
                }
            }
            return result;
        }



        public void GetEvent(out SqlGuid EventId, out SqlDateTime EventPosted, out SqlDateTime EventReceived, out SqlXml EventArgs, SqlString EventType, SqlString Options, CancellationToken token)
        {
            using (SqlConnection cn = new SqlConnection(_attributes[CONNECTION_STRING]))
            {
                try
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.prc_EventGet", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@EventID", SqlDbType.UniqueIdentifier).Value = SqlGuid.Null;
                        cmd.Parameters["@EventID"].Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@EventPosted", SqlDbType.DateTime).Value = SqlDateTime.Null;
                        cmd.Parameters["@EventPosted"].Direction = ParameterDirection.Output;
                        cmd.Parameters.Add("@EventReceived", SqlDbType.DateTime).Value = SqlDateTime.Null;
                        cmd.Parameters["@EventReceived"].Direction = ParameterDirection.Output;
                        cmd.Parameters.AddWithValue("@EventArgs", SqlDbType.Xml).Value = SqlXml.Null;
                        cmd.Parameters["@EventArgs"].Direction = ParameterDirection.Output;
                        cmd.Parameters.AddWithValue("@EventType", EventType);
                        cmd.Parameters.AddWithValue("@Options", Options);
                        cmd.CommandTimeout = 0;

                        using (token.Register(cmd.Cancel))
                        {
                            cmd.ExecuteNonQuery();
                            EventId = (SqlGuid)cmd.Parameters["@EventID"].SqlValue;
                            EventPosted = (SqlDateTime)cmd.Parameters["@EventPosted"].SqlValue;
                            EventReceived = (SqlDateTime)cmd.Parameters["@EventReceived"].SqlValue;
                            EventArgs = (SqlXml)cmd.Parameters["@EventArgs"].SqlValue;
                        }
                    }
                }
                catch (SqlException e)
                {
                    throw e;
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
