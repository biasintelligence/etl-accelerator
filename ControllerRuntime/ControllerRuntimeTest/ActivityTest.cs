using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DefaultActivities;
using ControllerRuntime;

namespace ControllerRuntimeTest
{
    [TestClass]
    public class ActivityTest
    {

        const string connectionString = @"Server=.\sql14;Database=etl_controller;Trusted_Connection=True;Connection Timeout=120;";

        [TestMethod]
        public void TGZCompress_Ok()
        {
            TGZCompressActivity activity = new TGZCompressActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("InputFile", "c:\\Builds\\FlatFiles\\*.txt"));
            list.Add(new WorkflowAttribute("OutputFolder", "c:\\Builds\\ZipFiles"));
            list.Add(new WorkflowAttribute("ArchiveName", "test"));
            list.Add(new WorkflowAttribute("Timeout", "30"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true,true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void TGZDecompress_Ok()
        {
            TGZDecompressActivity activity = new TGZDecompressActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("InputFile", "c:\\Builds\\ZipFiles\\*.tar.gz"));
            list.Add(new WorkflowAttribute("OutputFolder", "c:\\Builds\\UnzipFiles"));
            list.Add(new WorkflowAttribute("Timeout", "30"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void BsonConvert_Ok()
        {
            BsonConverterActivity activity = new BsonConverterActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("InputFile", "C:\\Builds\\UnzipFiles\\mongobackup_10-19-2016-230145\\edxapp\\*.bson"));
            list.Add(new WorkflowAttribute("OutputFolder", "c:\\Builds\\JsonFiles\\10-19-2016\\edxapp"));
            list.Add(new WorkflowAttribute("Timeout", "30"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void BsonSqlLoader_Ok()
        {
            BsonSqlLoaderActivity activity = new BsonSqlLoaderActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=.\sql14; Database=etl_staging; Trusted_Connection=True; Connection Timeout=120; "));
            list.Add(new WorkflowAttribute("InputFile", "C:\\Builds\\UnzipFiles\\mongobackup_10-19-2016-230145\\edxapp\\fs.files.bson"));
            list.Add(new WorkflowAttribute("TableName", "dbo.staging_bson_test"));
            list.Add(new WorkflowAttribute("Timeout", "600"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }


        [TestMethod]
        public void FileRegister_Ok()
        {
            FileRegisterActivity activity = new FileRegisterActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("RegisterConnectionString", @"Server=.\sql14; Database=etl_staging; Trusted_Connection=True; Connection Timeout=120; "));
            list.Add(new WorkflowAttribute("RegisterPath", "c:\\Builds\\FlatFiles\\*.txt"));
            list.Add(new WorkflowAttribute("SourceName", "testFiles"));
            list.Add(new WorkflowAttribute("ProcessPriority", "1"));
            list.Add(new WorkflowAttribute("Timeout", "30"));
            list.Add(new WorkflowAttribute("@runId", "1"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void FileDequeue_Ok()
        {
            FileGetProcessListActivity activity = new FileGetProcessListActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=.\sql14; Database=etl_controller; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("RegisterConnectionString", @"Server=.\sql14; Database=etl_staging; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("SourceName", "testFiles"));
            list.Add(new WorkflowAttribute("Timeout", "30"));
            list.Add(new WorkflowAttribute("etl:RunId", "1"));
            list.Add(new WorkflowAttribute("etl:BatchId", "101"));
            list.Add(new WorkflowAttribute("etl:StepId", "1"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void FileSetStatus_Ok()
        {
            FileSetProgressStatusActivity activity = new FileSetProgressStatusActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("RegisterConnectionString", @"Server=.\sql14; Database=etl_staging; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("FileId", "5"));
            list.Add(new WorkflowAttribute("FileStatus", "Completed"));
            list.Add(new WorkflowAttribute("Timeout", "30"));
            list.Add(new WorkflowAttribute("etl:RunId", "1"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void DERun_Ok()
        {
            DeltaExtractorActivity activity = new DeltaExtractorActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=localhost; Database=etl_controller; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("@BatchId", "1002"));
            list.Add(new WorkflowAttribute("@StepId", "1"));
            list.Add(new WorkflowAttribute("@RunId", "0"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }


    }


}
