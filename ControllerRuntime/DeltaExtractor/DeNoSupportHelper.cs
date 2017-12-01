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

using Serilog;
using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{

    public interface IDeStagingSupport
    {
        bool Test( ILogger logger);
        bool IsValid { get; set; }
        bool IsView { get; set; }
        bool TruncateDestinationTable(ILogger logger);
        bool CreateStagingTable(bool createflag, ILogger logger);
        bool UploadStagingTable(int RunId, ILogger logger);
    }


    public class DeNoSupportHelper : IDeStagingSupport
    {


        public bool IsValid { get; set; }
        public bool IsView { get; set; }
        public bool Test(ILogger logger)
        {
            logger.Debug("Not implemented");
            this.IsValid = true;
            return this.IsValid;
        }


        public bool TruncateDestinationTable(ILogger logger)
        {
            logger.Debug("Not implemented");
            return true;
        }

        public bool CreateStagingTable(bool createflag, ILogger logger)
        {
            logger.Debug("Not implemented");
            return true;
        }

        public bool UploadStagingTable(int RunId, ILogger logger)
        {
            logger.Debug("Not implemented");
            return true;
        }

    }
}