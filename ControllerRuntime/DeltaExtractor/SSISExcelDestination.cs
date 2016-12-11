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

using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    public class SSISExcelDestination : SSISModule
    {

        public SSISExcelDestination(ExcelDestination dbdst, MainPipe pipe, IDTSComponentMetaData100 src, int outputID, ConnectionManager cm, IWorkflowLogger logger)
            : base(pipe, "Excel Destination", outputID, logger)
        {

            //create ole db destination component           
            //set connection properties
            cm.Name = String.Format(CultureInfo.InvariantCulture, "Excel Destination Connection Manager {0}", outputID);
            cm.ConnectionString = dbdst.ConnectionString;
            cm.Description = dbdst.Description;


            //mwrt.IDTSConnectionManagerExcel100 ecm = cm.InnerObject as mwrt.IDTSConnectionManagerExcel100;
            //ecm.ExcelFilePath = dbdst.FilePath;
            //ecm.FirstRowHasColumnName = dbdst.Header;
            //ecm.ExcelVersionNumber = mwrt.DTSExcelVersion.DTSExcelVer_2007;


            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            // Set oledb destination custom properties
            //default to openrowset
            dcomp.SetComponentProperty("AccessMode", 0);
            //foreach (KeyValuePair<string, object> prop in dbdst.CustomProperties.CustomPropertyCollection.InnerArrayList)
            foreach (KeyValuePair<string, object> prop in dbdst.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            if (comp.RuntimeConnectionCollection.Count > 0)
            {
                comp.RuntimeConnectionCollection[0].ConnectionManagerID = cm.ID;
                comp.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(cm);
            }

            this.Reinitialize(dcomp);

            //Create datatype converter if needed
            Dictionary<int, int> map = new Dictionary<int, int>();
            IDTSVirtualInput100 vInput = src.InputCollection[0].GetVirtualInput();
            IDTSExternalMetadataColumnCollection100 exColumns = comp.InputCollection[0].ExternalMetadataColumnCollection;
            if (this.needDataTypeChange(vInput, exColumns))
            {
                SSISDataConverter ssisdc = new SSISDataConverter(pipe, src, outputID, exColumns, logger);
                src = ssisdc.MetadataCollection;
                map = ssisdc.ConvertedColumns;
                outputID = 0;
            }

            this.ConnectComponents(src, outputID);
            this.MatchInputColumns(map, true, logger);
        }

    }
}

