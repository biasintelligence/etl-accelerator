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

namespace BIAS.Framework.DeltaExtractor
{
    public class SSISExcelDestination : SSISModule
    {

        public SSISExcelDestination(ExcelDestination dbdst, MainPipe pipe, IDTSComponentMetaData100 src, int outputID, ConnectionManager cm)
            : base(pipe, "Excel Destination", outputID)
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
            Dictionary<string, int> converted = new Dictionary<string, int>();
            IDTSVirtualInput100 vInput = src.InputCollection[0].GetVirtualInput();
            if (this.needDataTypeChange(vInput, comp.InputCollection[0]))
            {
                //create the destination column collection
                Dictionary<string, MyColumn> exColumns = new Dictionary<string, MyColumn>();
                foreach (IDTSExternalMetadataColumn100 exColumn in comp.InputCollection[0].ExternalMetadataColumnCollection)
                {
                    MyColumn col = new MyColumn();
                    col.Name = exColumn.Name;
                    col.DataType = exColumn.DataType;
                    col.Length = exColumn.Length;
                    col.Precision = exColumn.Precision;
                    col.Scale = exColumn.Scale;
                    col.CodePage = exColumn.CodePage;
                    exColumns.Add(exColumn.Name, col);
                }
                SSISDataConverter ssisdc = new SSISDataConverter(pipe, src, outputID, exColumns);
                src = ssisdc.MetadataCollection;
                converted = ssisdc.ConvertedColumns;
                outputID = 0;
            }

            this.ConnectComponents(src, outputID);
            this.MatchInputColumns(converted, true);
        }

    }
}

