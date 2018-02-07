using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Data;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

using mwrt = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.InteropServices;

using Serilog;
using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    public class SSISExcelDestination : SSISModule,ISSISModule
    {

        private ExcelDestination _dst;
        private ConnectionManager _cm;

        public SSISExcelDestination(ExcelDestination dst, MainPipe pipe, ConnectionManager cm, ILogger logger,Application app)
            : base(pipe, "Excel Destination", logger, app)
        {
            _dst = dst;
            _cm = cm;

        }
        public override IDTSComponentMetaData100 Initialize()
        {
            //create excel destination component           
            IDTSComponentMetaData100 comp = base.Initialize();

             //set connection properties
            _cm.Name = $"Excel Destination Connection Manager {comp.ID}";
            _cm.ConnectionString = _dst.ConnectionString;
            _cm.Description = _dst.Description;


            //mwrt.IDTSConnectionManagerExcel100 ecm = cm.InnerObject as mwrt.IDTSConnectionManagerExcel100;
            //ecm.ExcelFilePath = dbdst.FilePath;
            //ecm.FirstRowHasColumnName = dbdst.Header;
            //ecm.ExcelVersionNumber = mwrt.DTSExcelVersion.DTSExcelVer_2007;


            CManagedComponentWrapper dcomp = comp.Instantiate();

            // Set oledb destination custom properties
            //default to openrowset
            dcomp.SetComponentProperty("AccessMode", 0);
            //foreach (KeyValuePair<string, object> prop in dbdst.CustomProperties.CustomPropertyCollection.InnerArrayList)
            foreach (KeyValuePair<string, object> prop in _dst.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            if (comp.RuntimeConnectionCollection.Count > 0)
            {
                comp.RuntimeConnectionCollection[0].ConnectionManagerID = _cm.ID;
                comp.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(_cm);
            }

            this.Reinitialize(dcomp);
            return comp;

        }

    }
}

