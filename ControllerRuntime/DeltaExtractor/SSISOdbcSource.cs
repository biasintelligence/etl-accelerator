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
    public class SSISOdbcSource : SSISModule
    {
        public SSISOdbcSource(OdbcSource dbsrc, MainPipe pipe, ConnectionManager cm, IWorkflowLogger logger)
            : base(pipe, "ODBC Source", logger)
        {
            // create the odbc source
            //set connection properies
            cm.Name = "ODBC Source Connection Manager";
            cm.ConnectionString = dbsrc.ConnectionString;
            cm.Description = dbsrc.Description;
            //do not require Qualifier
            //cm.Qualifier = dbsrc.DBConnection.Qualifier;

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            //set Component Custom Properties
            foreach (KeyValuePair<string, object> prop in dbsrc.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

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
