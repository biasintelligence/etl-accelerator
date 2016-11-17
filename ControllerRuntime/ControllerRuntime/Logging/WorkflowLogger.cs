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

namespace ControllerRuntime
{
    public abstract class WorkflowLogger : IWorkflowLogger
    {
        protected bool _debug_mode_enabled = false;
        protected bool _verbose_mode_enabled = false;
        public bool Mode { get { return _debug_mode_enabled; } }
        protected WorkflowLogger(bool DebugEnabled, bool VerboseEnabled)
        {
            _debug_mode_enabled = DebugEnabled;
            _verbose_mode_enabled = VerboseEnabled;
        }

        public virtual void Write(string Message)
        {
            if (_verbose_mode_enabled)
                Console.WriteLine(Message);
        }

        public virtual void WriteDebug(string Message)
        {
            if (_debug_mode_enabled && _verbose_mode_enabled)
                Console.WriteLine(Message);
        }


        public virtual void WriteError(string Message, int ErrorCode)
        {
            Console.WriteLine(String.Format("Error: {0} {1}", ErrorCode, Message));
        }

    }

    /// <summary>
    /// Direct all process output to the Console
    /// </summary>
    public class WorkflowConsoleLogger : WorkflowLogger
    {
        public WorkflowConsoleLogger(bool Debug, bool Verbose)
            : base(Debug, Verbose)
        {
        }
    }

    /// <summary>
    /// Logs all process output to the ETL Controller
    /// </summary>
    public class WorkflowControllerLogger : WorkflowLogger, IDisposable
    {

        private bool disposed = false;
        private SqlConnection conn = new SqlConnection();
        private SqlCommand cmd = new SqlCommand();
        private int wf_id = 0;
        private int step_id = 0;
        private int const_id = 0;
        private int run_id = 0;

        public WorkflowControllerLogger(int WorkflowId, int StepId, int ConstId, int RunId, string ConnectionString, bool Debug, bool Verbose)
            : base(Debug, Verbose)
        {
            conn.ConnectionString = ConnectionString;
            wf_id = WorkflowId;
            step_id = StepId;
            const_id = ConstId;
            run_id = RunId;

            InitializeCommand();

        }

        public override void WriteError(string Message, int ErrorCode)
        {
            LogToController(Message, ErrorCode);
            base.WriteError(Message, ErrorCode);
        }


        public override void Write(string Message)
        {
            LogToController(Message, 0);
            base.Write(Message);
        }

        public override void WriteDebug(string Message)
        {
            if (_debug_mode_enabled)
                LogToController(Message, 0);

            base.WriteDebug(Message);
        }


        private void LogToController(string Message, int ErrorCode)
        {

            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }

                using (SqlCommand cmd_run = cmd.Clone())
                {
                    cmd_run.Parameters["@pMessage"].Value = Message;
                    cmd_run.Parameters["@pErr"].Value = ErrorCode;
                    cmd_run.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(String.Format("Logger Error: {0} {1}", ex.ErrorCode, ex.Message));
                //throw ex;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }

        }

        private void InitializeCommand()
        {
            cmd.Connection = conn;
            cmd.CommandTimeout = 120;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[prc_ApplicationLog]";

            cmd.Parameters.Add("@pMessage", SqlDbType.NVarChar, -1);
            cmd.Parameters.Add("@pErr", SqlDbType.Int);
            cmd.Parameters.AddWithValue("@pBatchId", wf_id);
            cmd.Parameters.AddWithValue("@pStepId", step_id);
            cmd.Parameters.AddWithValue("@pRunId", run_id);

        }

        void IDisposable.Dispose()
        {
            if (!disposed)
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();

                cmd.Dispose();
                conn.Dispose();
                disposed = true;
            }
        }
    }

}
