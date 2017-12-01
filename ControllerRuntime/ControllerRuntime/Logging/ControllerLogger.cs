using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.Data;
using System.Data.SqlClient;
using Serilog;

namespace ControllerRuntime.Logging
{
    /// <summary>
    /// Logs all process output to the ETL Controller
    /// </summary>
    public class ControllerLogger
    {

        private string _connectionString;
        ILogger _logger;

        public ControllerLogger(string ConnectionString)
        {
            _connectionString = ConnectionString;
            _logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();

        }

        public async Task LogToControllerAsync(
            string Message,
            int ErrorCode,
            CancellationToken Token,
            int WorkflowId,
            int StepId,
            int RunId
            )
        {
            await Task.Factory.StartNew(() =>
            {
                LogToController(Message, ErrorCode, WorkflowId,StepId,RunId);
            },Token);
        }

        public void LogToController(
            string Message,
            int ErrorCode,
            int WorkflowId,
            int StepId,
            int RunId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[prc_ApplicationLog]", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 120;

                        cmd.Parameters.AddWithValue("@pBatchId", WorkflowId);
                        cmd.Parameters.AddWithValue("@pStepId", StepId);
                        cmd.Parameters.AddWithValue("@pRunId", RunId);

                        cmd.Parameters.AddWithValue("@pMessage", Message);
                        cmd.Parameters.AddWithValue("@pErr", ErrorCode);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    _logger.Error(ex, "Logger Error {ErrorCode}: {Message}", ex.ErrorCode, ex.Message);
                }
                finally
                {
                    if (connection.State != ConnectionState.Closed)
                        connection.Close();
                }
            }

        }
    }
}
