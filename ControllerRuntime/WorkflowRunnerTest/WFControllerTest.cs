using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//using DefaultActivities;
using ControllerRuntime;
using ControllerRuntime.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ControllerRuntimeTest
{
    [TestClass]
    public class WFControllerTest
    {
        //const string connectionString = @"Server=devomasq91;Database=etl_controller;Trusted_Connection=True;Connection Timeout=120;";

        [TestMethod]
        public void WFRun_Ok()
        {

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string runnerName = builder.GetSection("Data:Runner").Value;
            string connectionString = builder.GetSection("Data:Controller").Value;
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder)
                .MinimumLevel.Debug()
                //.WriteTo.File(path: @"c:\logs\log.txt",
                //             rollOnFileSizeLimit: true,
                //             rollingInterval: RollingInterval.Hour,
                //             fileSizeLimitBytes: 1024000
                //            )
                .WriteTo.WorkflowLogger(connectionString: connectionString)
                .CreateLogger();

            string WFName = "test105";

            WorkflowAttributeCollection attributes = new WorkflowAttributeCollection();
            //attributes.Add(WorkflowConstants.ATTRIBUTE_PROCESSOR_NAME, runnerName);
            attributes.Add(WorkflowConstants.ATTRIBUTE_DEBUG, "false");
            attributes.Add(WorkflowConstants.ATTRIBUTE_VERBOSE, "true");
            attributes.Add(WorkflowConstants.ATTRIBUTE_FORCESTART, "true");
            attributes.Add(WorkflowConstants.ATTRIBUTE_CONTROLLER_CONNECTIONSTRING, connectionString);
            attributes.Add(WorkflowConstants.ATTRIBUTE_WORKFLOW_NAME, WFName);
            //attributes.Add(WorkflowConstants.ATTRIBUTE_REQUEST_ID, Guid.NewGuid().ToString());


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

