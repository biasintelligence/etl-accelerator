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

namespace BIAS.Framework.DeltaExtractor
{

    public interface IDeStagingSupport
    {
        bool Test();
        bool IsValid { get; set; }
        bool IsView { get; set; }
        bool TruncateDestinationTable();
        bool CreateStagingTable(bool createflag);
        bool UploadStagingTable(int RunId);
    }


    public class DeNoSupportHelper : IDeStagingSupport
    {


        public bool IsValid { get; set; }
        public bool IsView { get; set; }
        public bool Test()
        {
            PrintOutput.PrintToOutput("Not implemented", DERun.Debug);
            this.IsValid = true;
            return this.IsValid;
        }


        public bool TruncateDestinationTable()
        {
            PrintOutput.PrintToOutput("Not implemented", DERun.Debug);
            return true;
        }

        public bool CreateStagingTable(bool createflag)
        {
            PrintOutput.PrintToOutput("Not implemented", DERun.Debug);
            return true;
        }

        public bool UploadStagingTable(int RunId)
        {
            PrintOutput.PrintToOutput("Not implemented", DERun.Debug);
            return true;
        }

    }
}