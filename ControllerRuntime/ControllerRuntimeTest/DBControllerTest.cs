﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using ControllerRuntime;
using WorkflowConsoleRunner;
using DefaultActivities;

namespace ControllerRuntimeTest
{
    [TestClass]
    public class DBControllerTest
    {

        const string connectionString = @"Server=localhost;Database=etl_controller;Trusted_Connection=True;Connection Timeout=120;";

        [TestMethod]        
        public void Test_Graph_Run_Ok()
        {
                       
            DBController db = DBController.Create(connectionString);
            Workflow wf = db.WorkflowMetadataGet("Test100");
            WorkflowGraph wfg = WorkflowGraph.Create(wf,db);
            wfg.Start();
            
            BlockingCollection<string> step_set = new BlockingCollection<string>();

            Task t1 = Task.Factory.StartNew(() =>
            {
                WorkflowStep step = null;
                while (wfg.TryTake(out step,TimeSpan.FromMinutes(5)))
                {
                    wfg.SetNodeExecutionResult(step.Key, WfResult.Started);
                    Thread.Sleep(1000);
                    step_set.Add(step.Key);
                }

                step_set.CompleteAdding();
                Console.WriteLine(String.Format("Finishing Step Submitting thread"));

            });

            Task t2 = Task.Factory.StartNew(() =>
            {
                string Key = String.Empty;
                while (step_set.TryTake(out Key,-1))
                {

                    Console.WriteLine(String.Format("Processing step {0}", Key));
                    wfg.SetNodeExecutionResult(Key, WfResult.Succeeded);
                    //wfg.SetNodeExecutionResult(Key, WfResult.Failed);
                }

                Console.WriteLine(String.Format("Finishing Step Processing thread"));

            });

            Task.WaitAll(t1,t2);

            WfResult wr = wfg.WorkflowRunStatus;
            WfResult wc = wfg.WorkflowCompleteStatus;
            Console.WriteLine(String.Format("Run status {0}", wr.StatusCode.ToString()));
            Console.WriteLine(String.Format("Complete status {0}", wc.StatusCode.ToString()));
            Assert.IsTrue(wr.StatusCode == WfStatus.Succeeded);
        }

        //[TestMethod]
        public void Test_Create_Graph_Ok()
        {

            DBController db = DBController.Create(connectionString);
            Workflow wf = db.WorkflowMetadataGet("Test100");
            WorkflowGraph wfg = WorkflowGraph.Create(wf,db);

            Assert.IsTrue(wfg.WorkflowRunStatus.StatusCode == WfStatus.Unknown);
        }

        //[TestMethod]
        public void Test_Workflow_Steps_Run_Ok()
        {

            DBController db = DBController.Create(connectionString);
            Workflow wf = db.WorkflowMetadataGet("Test100");
            WorkflowGraph wfg = WorkflowGraph.Create(wf,db);
            Task[] tasks = new Task[wfg.Count];

            WfResult wf_status = wfg.Start();

            WorkflowStep step = null;
            int i = 0;
            while (wfg.TryTake(out step, TimeSpan.FromMinutes(5)))
            {
                wfg.SetNodeExecutionResult(step.Key, WfResult.Started);
                tasks[i++] =
                    Task.Factory.StartNew( (object obj) =>
                        {
                            WorkflowStep s = obj as WorkflowStep;
                            Console.WriteLine(String.Format("Processing step {0}", s.Key));
                            Thread.Sleep(1000);
                            wfg.SetNodeExecutionResult(s.Key, WfResult.Succeeded);
                            //wfg.SetNodeExecutionResult(Key, WfResult.Failed);
                        }, step);
            }

            Task.WaitAll(tasks);

            WfResult wr = wfg.WorkflowRunStatus;
            WfResult wc = wfg.WorkflowCompleteStatus;
            Console.WriteLine(String.Format("Run status {0}", wr.StatusCode.ToString()));
            Console.WriteLine(String.Format("Complete status {0}", wc.StatusCode.ToString()));
            Assert.IsTrue(wr.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void Test_Workflow_Processor_Run_Ok()
        {

            WorkflowProcessor wfp = new WorkflowProcessor("TestRunner");
            wfp.ConnectionString = connectionString;
            wfp.WorkflowName = "Test100";
            string[] options = new string[]
            {
                "debug",
                "verbose",
                "forcestart"
            };
            WfResult wr = wfp.Run(options);
            Assert.IsTrue(wr.StatusCode == WfStatus.Succeeded);
        }


        [TestMethod]
        public void Test_Log_Ok()
        {
            WorkflowControllerLogger logger = new WorkflowControllerLogger(-10, 1, 0, 1, connectionString,true,true);
            logger.Write("Test Info Message");
            logger.WriteDebug("Test Debug Message");
            logger.WriteError("Test Error Message", -1);

        }

        [TestMethod]
        public void Test_Workflow_Attributs_Get_Ok()
        {
            DBController db = DBController.Create(connectionString);
            WorkflowAttribute[] attributes = db.WorkflowAttributeCollectionGet(100,1,0,0);
            Assert.IsTrue(attributes.Length > 0);
        }

    }
}

