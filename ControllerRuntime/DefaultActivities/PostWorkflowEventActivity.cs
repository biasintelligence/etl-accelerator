﻿/******************************************************************
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

using ControllerRuntime;

namespace DefaultActivities
{
    /// <summary>
    /// Post Event to the Workflow Event Server
    /// </summary>
    public class PostWorkflowEventActivity : IWorkflowActivity
    {
        protected const string CONNECTION_STRING = "ConnectionString";
        protected const string EVENT_TYPE = "EventType";
        protected const string POST_DATE = "EventPostDate";
        protected const string EVENT_ARGS = "EventArgs";
        protected const string TIMEOUT = "Timeout";


        protected Dictionary<string, string> _attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        protected IWorkflowLogger _logger;
        protected List<string> _required_attributes = new List<string>() { EVENT_TYPE, CONNECTION_STRING, POST_DATE, EVENT_ARGS, TIMEOUT };


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

            _logger.WriteDebug(String.Format("EventServer: {0}", _attributes[CONNECTION_STRING]));
            _logger.WriteDebug(String.Format("EventType: {0}", _attributes[EVENT_TYPE]));

        }

        public virtual WfResult Run(CancellationToken token)
        {
            using (SqlConnection cn = new SqlConnection(_attributes[CONNECTION_STRING]))
            {
                try
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.prc_EventPost", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EventType", _attributes[EVENT_TYPE]);
                        cmd.Parameters.AddWithValue("@EventPosted", SqlDateTime.Parse(_attributes[POST_DATE]));
                        cmd.Parameters.Add("@EventArgs", SqlDbType.Xml, _attributes[EVENT_ARGS].Length);
                        if (!String.IsNullOrEmpty(_attributes[EVENT_ARGS]))
                            cmd.Parameters["@EventArgs"].Value = _attributes[EVENT_ARGS];
                        cmd.Parameters.AddWithValue("@Options", "");
                        cmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);

                        using (token.Register(cmd.Cancel))
                        {
                            cmd.ExecuteNonQuery();
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
            return WfResult.Succeeded;
        }

    }
}
