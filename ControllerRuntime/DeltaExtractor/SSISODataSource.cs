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
//using Microsoft.SqlServer.IntegrationServices.OData;

using mwrt = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.InteropServices;

using Serilog;
using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    public class SSISODataSource : SSISModule,ISSISModule
    {
        private ODataSource _src;
        private ConnectionManager _cm;

        public SSISODataSource(ODataSource src, MainPipe pipe, ConnectionManager cm, ILogger logger,Application app)
            : base(pipe, "OData Source", logger, app)
        {
            _src = src;
            _cm = cm;

        }
        public override IDTSComponentMetaData100 Initialize()
        {
            // create the odata source
            IDTSComponentMetaData100 comp = base.Initialize();
            //set connection properies
            _cm.Name = "OData Source Connection Manager";
            _cm.ConnectionString = _src.ConnectionString;
            _cm.Description = _src.Description;

            //ODataConnectionManager ocm = (ODataConnectionManager)cm.InnerObject;

            //mwrt.IDTSConnectionManager100 dcm = DtsConvert.GetExtendedInterface(cm);
            //dcm.AcquireConnection(null);

            //set connection properties
            //mwrt.IDTSConnectionManagerHttp100 hcm = cm.InnerObject as mwrt.IDTSConnectionManagerHttp100;

            //cm.Properties["UserName"].SetValue(cm, "");
            //string val = cm.Properties["Url"].GetValue(cm).ToString();



            //foreach( var prop in cm.Properties)
            //{
            //    string val = prop.GetValue(cm).ToString();
            //}

            CManagedComponentWrapper dcomp = comp.Instantiate();
            foreach (KeyValuePair<string, object> prop in _src.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

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
