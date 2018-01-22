using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DefaultActivities;
using AwsActivities;
using AzureActivities;
using ControllerRuntime;

using Serilog;

namespace ControllerRuntimeTest
{
    [TestClass]
    public class ActivityTest
    {

        private static TestContext _context;
        const string connectionString = @"Server=.;Database=etl_controller;Trusted_Connection=True;Connection Timeout=120;";

        private static ILogger _logger;


        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return _context;
            }
            set
            {
                _context = value;
            }
        }

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            _context = context;
            _logger = new LoggerConfiguration()
                  .MinimumLevel.Debug()
                  .WriteTo.Console()
                  .CreateLogger();
        }



        [TestMethod]
        public void TGZCompress_Ok()
        {
            TGZCompressActivity activity = new TGZCompressActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("InputFile", "c:\\Builds\\FlatFiles\\*.txt");
            list.Add("OutputFolder", "c:\\Builds\\ZipFiles");
            list.Add("ArchiveName", "test");
            list.Add("Timeout", "30");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void TGZDecompress_Ok()
        {
            TGZDecompressActivity activity = new TGZDecompressActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("InputFile", "c:\\Builds\\ZipFiles\\test.tar.gz");
            list.Add("OutputFolder", "c:\\Builds\\UnzipFiles");
            list.Add("Mode", "tgz");
            list.Add("Timeout", "30");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void BsonConvert_Ok()
        {
            BsonConverterActivity activity = new BsonConverterActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("InputFile", "C:\\Builds\\UnzipFiles\\mongobackup_10-19-2016-230145\\edxapp\\*.bson");
            list.Add("OutputFolder", "c:\\Builds\\JsonFiles\\10-19-2016\\edxapp");
            list.Add("Timeout", "30");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void BsonSqlLoader_Ok()
        {
            BsonSqlLoaderActivity activity = new BsonSqlLoaderActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("ConnectionString", @"Server=.\sql14; Database=etl_staging; Trusted_Connection=True; Connection Timeout=120; ");
            list.Add("InputFile", "C:\\Builds\\UnzipFiles\\mongobackup_10-19-2016-230145\\edxapp\\fs.files.bson");
            list.Add("TableName", "dbo.staging_bson_test");
            list.Add("Timeout", "600");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }


        [TestMethod]
        public void FileRegister_Ok()
        {
            FileRegisterActivity activity = new FileRegisterActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("RegisterConnectionString", @"Server=.\sql14; Database=etl_staging; Trusted_Connection=True; Connection Timeout=120; ");
            list.Add("RegisterPath", "c:\\Builds\\FlatFiles\\*.txt");
            list.Add("SourceName", "testFiles");
            list.Add("ProcessPriority", "1");
            list.Add("Timeout", "30");
            list.Add("@runId", "1");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void FileDequeue_Ok()
        {
            FileGetProcessListActivity activity = new FileGetProcessListActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("ConnectionString", @"Server=.\sql14; Database=etl_controller; Trusted_Connection = True; Connection Timeout = 120; ");
            list.Add("RegisterConnectionString", @"Server=.\sql14; Database=etl_staging; Trusted_Connection = True; Connection Timeout = 120; ");
            list.Add("SourceName", "testFiles");
            list.Add("Timeout", "30");
            list.Add("etl:RunId", "1");
            list.Add("etl:BatchId", "101");
            list.Add("etl:StepId", "1");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void FileSetStatus_Ok()
        {
            FileSetProgressStatusActivity activity = new FileSetProgressStatusActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("RegisterConnectionString", @"Server=.\sql14; Database=etl_staging; Trusted_Connection = True; Connection Timeout = 120; ");
            list.Add("FileId", "5");
            list.Add("FileStatus", "Completed");
            list.Add("Timeout", "30");
            list.Add("etl:RunId", "1");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void DERun_Ok()
        {
            DeltaExtractorActivity activity = new DeltaExtractorActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("ConnectionString", @"Server=.; Database=etl_controller; Trusted_Connection = True; Connection Timeout = 120; ");
            list.Add("@BatchId", "1006");
            list.Add("@StepId", "11");
            list.Add("@RunId", "0");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void EventPost_Ok()
        {
            PostWorkflowEventActivity activity = new PostWorkflowEventActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("ConnectionString", @"Server=.; Database=etl_event; Trusted_Connection = True; Connection Timeout = 120; ");
            list.Add("EventType", @"Process2_FINISHED");
            list.Add("EventPostDate", @"2016-12-16 18:04");
            //list.Add("EventArgs", "<dwc:EventArgs xmlns:dwc=\"EventArgs.XSD\" Source=\"Process2 Finished\" PeriodGrain=\"Week\" Period=\"201652\" />");
            list.Add("EventArgs", "");
            list.Add("Timeout", "0");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void EventCheck_Ok()
        {
            CheckWorkflowEventActivity activity = new CheckWorkflowEventActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("ConnectionString", @"Server=.; Database=etl_event; Trusted_Connection = True; Connection Timeout = 120; ");
            list.Add("EventType", @"Process2_FINISHED");
            list.Add("WatermarkEventType", @"Process1_FINISHED");
            list.Add("Timeout", "0");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void ExecuteSqlQuery_Ok()
        {
            SqlServerActivity activity = new SqlServerActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            //list.Add("ConnectionString", @"Server=.; Database=etl_staging; Trusted_Connection = True; Connection Timeout = 120; ");
            list.Add("ConnectionString", @"Server=.; Database=etl_staging; Trusted_Connection = False;User Id=test;Password=test; Connection Timeout = 120; ");
            list.Add("Query", @"
print 'this is print test';
raiserror ('this is err test',11,11);
");
            list.Add("Timeout", "0");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void AzureFileDownload_Ok()
        {
            AzureFileDownloadActivity activity = new AzureFileDownloadActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("ConnectionString", @"Server=.; Database=etl_Controller; Trusted_Connection = True; Connection Timeout = 120; ");
            list.Add("Prefix", @"");
            list.Add("Modified", @"");
            list.Add("OutputFolder", "C:\\Builds\\ZipFiles");
            list.Add("FileShare", "xxx");
            list.Add("AccountKey", "key");
            list.Add("AccountName", "name");
            list.Add("isSasToken", "true");
            list.Add("Timeout", "0");
            list.Add("SortOrder", "Desc");
            list.Add("Count", "");
            list.Add("CounterName", "");
            list.Add("etl:BatchId", "1006");
            list.Add("etl:StepId", "11");
            list.Add("@RunId", "0");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }

        [TestMethod]
        public void AwsS3Download_Ok()
        {
            AwsS3DownloadActivity activity = new AwsS3DownloadActivity();
            WorkflowActivityArgs wfa = new WorkflowActivityArgs();
            WorkflowAttributeCollection list = new WorkflowAttributeCollection();
            list.Add("ConnectionString", @"Server=.; Database=etl_Controller; Trusted_Connection = True; Connection Timeout = 120; ");
            list.Add("Prefix", @"");
            list.Add("OutputFolder", "C:\\Builds\\ZipFiles\\AWS");
            list.Add("BucketName", "thl-bias-messages-staging");
            //list.Add("ProfileName", "testRunner");
            list.Add("AccountName", "");
            list.Add("AccountKey", "");
            list.Add("RegionName", "us-west-2");
            list.Add("Timeout", "0");
            list.Add("SortOrder", "Desc");
            list.Add("Count", "");
            list.Add("CounterName", "");
            list.Add("etl:BatchId", "1006");
            list.Add("etl:StepId", "11");
            list.Add("@RunId", "0");
            wfa.RequiredAttributes = list;
            wfa.Logger = _logger;

            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }


    }


}
