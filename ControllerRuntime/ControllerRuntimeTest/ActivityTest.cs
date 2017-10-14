using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DefaultActivities;
using ControllerRuntime;
using THLActivities;

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
            list.Add(new WorkflowAttribute("InputFile", "c:\\Builds\\ZipFiles\\test.tar.gz"));
            list.Add(new WorkflowAttribute("OutputFolder", "c:\\Builds\\UnzipFiles"));
            list.Add(new WorkflowAttribute("Mode", "tgz"));
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
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=.; Database=etl_controller; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("@BatchId", "1006"));
            list.Add(new WorkflowAttribute("@StepId", "11"));
            list.Add(new WorkflowAttribute("@RunId", "0"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void EventPost_Ok()
        {
            PostWorkflowEventActivity activity = new PostWorkflowEventActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=.; Database=etl_event; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("EventType", @"Process2_FINISHED"));
            list.Add(new WorkflowAttribute("EventPostDate", @"2016-12-16 18:04"));
            //list.Add(new WorkflowAttribute("EventArgs", "<dwc:EventArgs xmlns:dwc=\"EventArgs.XSD\" Source=\"Process2 Finished\" PeriodGrain=\"Week\" Period=\"201652\" />"));
            list.Add(new WorkflowAttribute("EventArgs", ""));
            list.Add(new WorkflowAttribute("Timeout", "0"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void EventCheck_Ok()
        {
            CheckWorkflowEventActivity activity = new CheckWorkflowEventActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=.; Database=etl_event; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("EventType", @"Process2_FINISHED"));
            list.Add(new WorkflowAttribute("WatermarkEventType", @"Process1_FINISHED"));
            list.Add(new WorkflowAttribute("Timeout", "0"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void ExecuteSqlQuery_Ok()
        {
            SqlServerActivity activity = new SqlServerActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=.; Database=etl_staging; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("Query", @"
print 'this is print test';
raiserror ('this is err test',11,11);
"));
            list.Add(new WorkflowAttribute("Timeout", "0"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void AzureFileDownload_Ok()
        {
            AzureFileDownloadActivity activity = new AzureFileDownloadActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=.; Database=etl_Controller; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("Prefix", @""));
            list.Add(new WorkflowAttribute("Modified", @""));
            list.Add(new WorkflowAttribute("OutputFolder", "C:\\Builds\\ZipFiles"));
            list.Add(new WorkflowAttribute("FileShare", "xxx"));
            list.Add(new WorkflowAttribute("AccountKey", "key"));
            list.Add(new WorkflowAttribute("AccountName", "name"));
            list.Add(new WorkflowAttribute("isSasToken", "true"));
            list.Add(new WorkflowAttribute("Timeout", "0"));
            list.Add(new WorkflowAttribute("SortOrder", "Desc"));
            list.Add(new WorkflowAttribute("Count", ""));
            list.Add(new WorkflowAttribute("CounterName", ""));
            list.Add(new WorkflowAttribute("etl:BatchId", "1006"));
            list.Add(new WorkflowAttribute("etl:StepId", "11"));
            list.Add(new WorkflowAttribute("@RunId", "0"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void AwsS3Download_Ok()
        {
            AwsS3DownloadActivity activity = new AwsS3DownloadActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=.; Database=etl_Controller; Trusted_Connection = True; Connection Timeout = 120; "));
            list.Add(new WorkflowAttribute("Prefix", @""));
            list.Add(new WorkflowAttribute("OutputFolder", "C:\\Builds\\ZipFiles\\AWS"));
            list.Add(new WorkflowAttribute("BucketName", "thl-bias-messages-staging"));
            //list.Add(new WorkflowAttribute("ProfileName", "testRunner"));
            list.Add(new WorkflowAttribute("AccountName", ""));
            list.Add(new WorkflowAttribute("AccountKey", ""));
            list.Add(new WorkflowAttribute("RegionName", "us-west-2"));
            list.Add(new WorkflowAttribute("Timeout", "0"));
            list.Add(new WorkflowAttribute("SortOrder", "Desc"));
            list.Add(new WorkflowAttribute("Count", ""));
            list.Add(new WorkflowAttribute("CounterName", ""));
            list.Add(new WorkflowAttribute("etl:BatchId", "1006"));
            list.Add(new WorkflowAttribute("etl:StepId", "11"));
            list.Add(new WorkflowAttribute("@RunId", "0"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void ThlDownload_Ok()
        {
            ThlDownloadActivity activity = new ThlDownloadActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            List<WorkflowAttribute> list = new List<WorkflowAttribute>();
            list.Add(new WorkflowAttribute("ConnectionString", @"Server=.; Database=etl_staging; Trusted_Connection = True; Connection Timeout = 120; "));
            //list.Add(new WorkflowAttribute("ProfileName", "testRunner"));
            list.Add(new WorkflowAttribute("AccountName", ""));
            list.Add(new WorkflowAttribute("AccountKey", ""));
            list.Add(new WorkflowAttribute("RegionName", "us-west-2"));
            list.Add(new WorkflowAttribute("Timeout", "0"));
            list.Add(new WorkflowAttribute("SqsUrl", "https://sqs.us-west-2.amazonaws.com/845009241909/bias-external-staging-pointUpdated"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }



    }


}
