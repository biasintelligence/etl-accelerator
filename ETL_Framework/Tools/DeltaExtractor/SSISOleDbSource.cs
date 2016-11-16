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
    public class SSISOleDbSource : SSISModule
    {
        public SSISOleDbSource(OleDbSource dbsrc, MainPipe pipe,ConnectionManager cm)
            : base(pipe, "OLE DB Source")
        {
            // create the oledb source
            //set connection properies
            cm.Name = "Oledb Source Connection Manager";
            cm.ConnectionString = dbsrc.ConnectionString;
            cm.Description = dbsrc.Description;

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            //default - execute from variable
            //dcomp.SetComponentProperty("AccessMode", 3);
            // Set oledb source custom properties
            //foreach (KeyValuePair<string, object> prop in parameters.DataSource.DBSource.CustomProperties.CustomPropertyCollection.InnerArrayList)
            foreach (KeyValuePair<string, object> prop in dbsrc.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                //if (prop.Key != "SqlCommand")
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            //default - execute from variable
            //if ((int)comp.CustomPropertyCollection["AccessMode"].Value == 3)
            //{
            //    dcomp.SetComponentProperty("SqlCommandVariable", "srcSelect");
            //}

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
