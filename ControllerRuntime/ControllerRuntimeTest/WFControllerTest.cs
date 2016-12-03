using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DefaultActivities;
using ControllerRuntime;

namespace ControllerRuntimeTest
{
    [TestClass]
    public class WFControllerTest
    {
        const string connectionString = @"Server=.\Sql14;Database=etl_controller;Trusted_Connection=True;Connection Timeout=120;";

        [TestMethod]
        public void WFRun_Ok()
        {
            string runnerName = "UTTest";
            string WFName = "Test100";
            WorkflowProcessor wfp = new WorkflowProcessor(runnerName);
            wfp.ConnectionString = connectionString;
            wfp.WorkflowName = WFName;
            WfResult wr = wfp.Run(new string[] { "debug" });
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

