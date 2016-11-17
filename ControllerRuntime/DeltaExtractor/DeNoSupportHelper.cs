using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Data;
using System.Runtime.Serialization;
using System.Globalization;
using System.Linq;
using System.Data.Common;
//using Excel = Microsoft.Office.Interop.Excel;

using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{

    public interface IDeStagingSupport
    {
        bool Test( IWorkflowLogger logger);
        bool IsValid { get; set; }
        bool IsView { get; set; }
        bool TruncateDestinationTable(IWorkflowLogger logger);
        bool CreateStagingTable(bool createflag, IWorkflowLogger logger);
        bool UploadStagingTable(int RunId, IWorkflowLogger logger);
    }


    public class DeNoSupportHelper : IDeStagingSupport
    {


        public bool IsValid { get; set; }
        public bool IsView { get; set; }
        public bool Test(IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            this.IsValid = true;
            return this.IsValid;
        }


        public bool TruncateDestinationTable(IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            return true;
        }

        public bool CreateStagingTable(bool createflag, IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            return true;
        }

        public bool UploadStagingTable(int RunId, IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            return true;
        }

    }
}