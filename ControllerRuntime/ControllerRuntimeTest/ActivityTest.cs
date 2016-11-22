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
            list.Add(new WorkflowAttribute("InputFile", "c:\\Builds\\BsonFiles\\*.txt"));
            list.Add(new WorkflowAttribute("OutputFolder", "c:\\Builds\\JsonFiles"));
            list.Add(new WorkflowAttribute("Timeout", "30"));
            wfa.RequiredAttributes = list.ToArray();
            wfa.Logger = new WorkflowConsoleLogger(true, true);


            activity.Configure(wfa);
            WfResult result = activity.Run(CancellationToken.None);
            Assert.IsTrue(result.StatusCode == WfStatus.Succeeded);
        }



    }
}
