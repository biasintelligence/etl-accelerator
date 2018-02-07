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
    public class SSISOleDbSource : SSISModule,ISSISModule
    {
        private OleDbSource _src;
        private ConnectionManager _cm;

        public SSISOleDbSource(OleDbSource src, MainPipe pipe,ConnectionManager cm, ILogger logger,Application app)
            : base(pipe, "OLE DB Source", logger,app)
        {
            _src = src;
            _cm = cm;

        }
        public override IDTSComponentMetaData100 Initialize()
        {
            // create the oledb source
            IDTSComponentMetaData100 comp = base.Initialize();
            //set connection properies
            _cm.Name = "Oledb Source Connection Manager";
            _cm.ConnectionString = _src.ConnectionString;
            _cm.Description = _src.Description;

            CManagedComponentWrapper dcomp = comp.Instantiate();

            //default - execute from variable
            //dcomp.SetComponentProperty("AccessMode", 3);
            // Set oledb source custom properties
            //foreach (KeyValuePair<string, object> prop in parameters.DataSource.DBSource.CustomProperties.CustomPropertyCollection.InnerArrayList)
            foreach (KeyValuePair<string, object> prop in _src.CustomProperties.CustomPropertyCollection.InnerArrayList)
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
                comp.RuntimeConnectionCollection[0].ConnectionManagerID = _cm.ID;
                comp.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(_cm);
            }

            // Finalize
            this.Reinitialize(dcomp);
            return comp;
            
        }
    }
}
