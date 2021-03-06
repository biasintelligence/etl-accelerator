﻿/******************************************************************
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

using Serilog;
using ControllerRuntime;

namespace DefaultActivities
{
    /// <summary>
    /// returns true if file exists
    /// </summary>
    class CheckFileActivity : IWorkflowActivity
    {
        private const string FILE_PATH = "CheckFile";


        private WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        private ILogger _logger;
        private List<string> _required_attributes = new List<string>() { FILE_PATH };


        public IEnumerable<string> RequiredAttributes
        {
            get { return _required_attributes; }
        }

        public void Configure(WorkflowActivityArgs args)
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

            _logger.Information("File: {File}", _attributes[FILE_PATH]);

        }

        public WfResult Run(CancellationToken token)
        {
            WfResult result = WfResult.Unknown;
            //_logger.Write(String.Format("SqlServer: {0} query: {1}", _attributes[CONNECTION_STRING], _attributes[QUERY_STRING]));

            bool exist = System.IO.File.Exists(_attributes[FILE_PATH]);
            result = (exist) ? WfResult.Succeeded : WfResult.Waiting;
            return result;
        }
    }
}
