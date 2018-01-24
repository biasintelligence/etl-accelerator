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
using System.IO;
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
    /// Register all files by Path pattern for processing
    /// </summary>
    public class FileSetProgressStatusActivity : IWorkflowActivity
    {
        protected const string CONNECTION_STRING = "RegisterConnectionString";
        protected const string FILE_ID = "FileId";
        protected const string FILE_STATUS = "FileStatus";
        //protected const string FILE_NAME = "FileName";
        //protected const string FILE_SOURCE = "SourceName";
        protected const string TIMEOUT = "Timeout";
        protected const string ETL_RUNID = "etl:RunId";

        protected const string REGISTRATION_QUERY = "dbo.prc_FileProcessProgressUpdate";


        protected WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        protected ILogger _logger;
        protected List<string> _required_attributes = new List<string>()
        { CONNECTION_STRING, FILE_ID,TIMEOUT,ETL_RUNID,FILE_STATUS };


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
                if (_required_attributes.Contains(attribute.Key, StringComparer.InvariantCultureIgnoreCase))
                    _attributes.Add(attribute.Key, attribute.Value);
            }

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_attributes[CONNECTION_STRING]);
            _logger.Debug("Controller: {Server}.{Database}", builder.DataSource, builder.InitialCatalog);
            _logger.Debug("FileId: {Id}, Status: {Status}", _attributes[FILE_ID], _attributes[FILE_STATUS]);
        }

        public virtual WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));


            SqlConnection cn = new SqlConnection(_attributes[CONNECTION_STRING]);
            try
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(REGISTRATION_QUERY, cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    int fileId = 0;
                    Int32.TryParse(_attributes[FILE_ID], out fileId);
                    int runId = 0;
                    Int32.TryParse(_attributes[ETL_RUNID], out runId);


                    cmd.CommandTimeout = Int32.Parse(_attributes[TIMEOUT]);
                    cmd.Parameters.AddWithValue("@processId", runId);
                    cmd.Parameters.AddWithValue("@fileId", fileId);
                    cmd.Parameters.AddWithValue("@ProgressStatusName", _attributes[FILE_STATUS]);

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
            return result;
        }

    }
}
