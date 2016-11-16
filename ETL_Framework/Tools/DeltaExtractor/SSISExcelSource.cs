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
    public class SSISExcelSource : SSISModule
    {
        public SSISExcelSource(ExcelSource dbsrc, MainPipe pipe, ConnectionManager cm)
            : base(pipe, "Excel Source")
        {
            // create the oledb source
            //set connection properies
            cm.Name = "Excel Source Connection Manager";
            cm.ConnectionString = dbsrc.ConnectionString;
            cm.Description = dbsrc.Description;

            //set connection properties
            //mwrt.IDTSConnectionManagerExcel100 ecm = cm.InnerObject as mwrt.IDTSConnectionManagerExcel100;
            //ecm.ExcelFilePath = dbsrc.FilePath;
            //ecm.FirstRowHasColumnName = dbsrc.Header;
            //ecm.ExcelVersionNumber = mwrt.DTSExcelVersion.DTSExcelVer_2007;

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            foreach (KeyValuePair<string, object> prop in dbsrc.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            /*Specify the connection manager for Src.The Connections class is a collection of the connection managers that have been added to that package and are available for use at run time*/
            if (comp.RuntimeConnectionCollection.Count > 0)
            {
                comp.RuntimeConnectionCollection[0].ConnectionManagerID = cm.ID;
                comp.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(cm);
            }

            // Finalize
            this.Reinitialize(dcomp);

        }
    }
}
