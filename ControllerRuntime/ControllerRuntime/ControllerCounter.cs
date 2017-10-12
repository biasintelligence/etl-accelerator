using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using ControllerRuntime;

namespace ControllerRuntime
{
    public class ControllerCounter
    {
        protected const string SET_COUNTER_QUERY = "dbo.prc_ETLCounterSet";
        private string _connectionString;
        private IWorkflowLogger _logger;
        public ControllerCounter(string ConnectionString,IWorkflowLogger Logger)
        {
            _connectionString = ConnectionString;
            _logger = Logger;
        }

        public int BatchId { get; set; }
        public int StepId { get; set; }
        public int RunId { get; set; }

        public void SetCounters(IEnumerable<KeyValuePair<string,string>> counters)
        {
            SqlConnection cn = new SqlConnection(_connectionString);
            try
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(SET_COUNTER_QUERY, cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.CommandTimeout = 120;
                    cmd.Parameters.AddWithValue("@pBatchId", BatchId);
                    cmd.Parameters.AddWithValue("@pStepId", StepId);
                    cmd.Parameters.AddWithValue("@pRunId", RunId);
                    cmd.Parameters.Add("@pName", SqlDbType.NVarChar, 100);
                    cmd.Parameters.Add("@pValue", SqlDbType.NVarChar, -1);

                    foreach (var counter in counters)
                    {
                        _logger.WriteDebug(String.Format("Set Counter: {0}:{1}", counter.Key,counter.Value));
                        cmd.Parameters["@pName"].SqlValue = counter.Key;
                        cmd.Parameters["@pValue"].SqlValue = counter.Value;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw ex;
                //_logger.Write(String.Format("SqlServer exception: {0}", ex.Message));
            }
            finally
            {
                if (cn.State != ConnectionState.Closed)
                    cn.Close();
            }

        }

    }
}
