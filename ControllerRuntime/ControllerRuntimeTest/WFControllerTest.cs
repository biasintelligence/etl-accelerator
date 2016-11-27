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
            string WFName = "Mongo1001";
            WorkflowProcessor wfp = new WorkflowProcessor(runnerName);
            wfp.ConnectionString = connectionString;
            wfp.WorkflowName = WFName;
            WfResult wr = wfp.Run(new string[] { "debug","forcestart" });
            Assert.IsTrue(wr.StatusCode == WfStatus.Succeeded);

        }

    }
}

