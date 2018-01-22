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
    /// Load File List into Controller Counter table
    /// </summary>
    public class FileListToCounterActivity : IWorkflowActivity
    {
        protected const string CONNECTION_STRING = "ConnectionString";
        protected const string FILE_PATH = "FilePath";
        protected const string TIMEOUT = "Timeout";
        protected const string ETL_RUNID = "@RunId";
        protected const string ETL_BATCHID = "etl:BatchId";
        protected const string ETL_STEPID = "etl:StepId";

        protected WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        protected ILogger _logger;
        protected List<string> _required_attributes = new List<string>()
        { CONNECTION_STRING,
          FILE_PATH,
          TIMEOUT,
          ETL_RUNID,
          ETL_BATCHID,
          ETL_STEPID
        };


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
            _logger.Debug("Controller: {Server}.{Database}", builder.DataSource, builder.InitialCatalog);
            _logger.Information("File Path: {File}", _attributes[FILE_PATH]);
        }

        public virtual WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));


            int runId = 0;
            Int32.TryParse(_attributes[ETL_RUNID], out runId);

            int batchId = 0;
            Int32.TryParse(_attributes[ETL_BATCHID], out batchId);

            int stepId = 0;
            Int32.TryParse(_attributes[ETL_STEPID], out stepId);

            string input = _attributes[FILE_PATH];

            ControllerCounter counter = new ControllerCounter(_attributes[CONNECTION_STRING], _logger)
            {
                BatchId = batchId,
                StepId = stepId,
                RunId = runId
            };

            int id = 0;
            Dictionary<string,string> files = Directory.GetFiles(Path.GetDirectoryName(input), Path.GetFileName(input), SearchOption.TopDirectoryOnly)
                .ToDictionary(k => String.Format("file_{0}",id++), v => v);
             
            counter.SetCounters(files);
            return WfResult.Succeeded;
        }

    }
}
