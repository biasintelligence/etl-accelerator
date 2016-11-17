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
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

using ControllerRuntime;

namespace DefaultActivities
{
    /// <summary>
    /// Execute SqlClient ExecuteScalar function
    /// </summary>
    class SqlServerExecuteScalarActivity : SqlServerActivity
    {

        public override WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            SqlConnection cn = new SqlConnection(base._attributes[CONNECTION_STRING]);
            try
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(_attributes[QUERY_STRING], cn))
                {
                    using (token.Register(cmd.Cancel))
                    {
                        cmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);
                        var value = cmd.ExecuteScalar();

                        int int_value;
                        if (Int32.TryParse(value.ToString(), out int_value))
                        {
                            result = (int_value > 0) ? WfResult.Succeeded : WfResult.Failed;
                        }
                        else
                            result = WfResult.Failed;
                    }
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
            return result;
        }

    }
}
