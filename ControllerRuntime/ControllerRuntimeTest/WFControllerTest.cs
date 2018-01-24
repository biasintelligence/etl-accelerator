﻿using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//using DefaultActivities;
using ControllerRuntime;
using ControllerRuntime.Logging;
using Serilog;

namespace ControllerRuntimeTest
{
    [TestClass]
    public class WFControllerTest
    {
        const string connectionString = @"Server=devomasq91;Database=etl_controller;Trusted_Connection=True;Connection Timeout=120;";

        [TestMethod]
        public void WFRun_Ok()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.WorkflowLogger(connectionString: connectionString)
                .CreateLogger();

            string runnerName = "UTTest";
            string WFName = "test8200";

            WorkflowAttributeCollection attributes = new WorkflowAttributeCollection();
            attributes.Add(WorkflowConstants.ATTRIBUTE_PROCESSOR_NAME, runnerName);
            attributes.Add(WorkflowConstants.ATTRIBUTE_DEBUG, "true");
            attributes.Add(WorkflowConstants.ATTRIBUTE_VERBOSE, "true");
            attributes.Add(WorkflowConstants.ATTRIBUTE_FORCESTART, "true");
            attributes.Add(WorkflowConstants.ATTRIBUTE_CONTROLLER_CONNECTIONSTRING, connectionString);
            attributes.Add(WorkflowConstants.ATTRIBUTE_WORKFLOW_NAME, WFName);


            WfResult wr = WfResult.Unknown;
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                WorkflowProcessor wfp = new WorkflowProcessor(attributes);
                wr = wfp.Run(cts.Token);
            }

            Assert.IsTrue(wr.StatusCode == WfStatus.Succeeded);

        }

        [TestMethod]
        public void DeserializeParameter_Ok()
        {
            string json = @"
[{'Name':'Attr1','Override':['Over1','Over2','Attr1'],'Default':'Na trasse vse spokoyno'}
,{'Name':'Attr2','Override':['Over1','Attr2'],'Default':'no'}
,{'Name':'Attr2','Override':['Attr2']}]
";
            WorkflowProcess p = new WorkflowProcess();
            p.Param = json;

            Assert.IsTrue(p.Parameters != null && p.Parameters.Count > 0);

        }


    }
}

