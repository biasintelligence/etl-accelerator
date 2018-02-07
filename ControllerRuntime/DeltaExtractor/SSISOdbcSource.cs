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
    public class SSISOdbcSource : SSISModule,ISSISModule
    {
        private OdbcSource _src;
        private ConnectionManager _cm;

        public SSISOdbcSource(OdbcSource src, MainPipe pipe, ConnectionManager cm, ILogger logger,Application app)
            : base(pipe, "ODBC Source", logger,app)
        {
            _src = src;
            _cm = cm;

        }
        public override IDTSComponentMetaData100 Initialize()
        {
            // create the odbc source
            IDTSComponentMetaData100 comp = base.Initialize();
            //set connection properies
            _cm.Name = "ODBC Source Connection Manager";
            _cm.ConnectionString = _src.ConnectionString;
            _cm.Description = _src.Description;
            //do not require Qualifier
            //cm.Qualifier = dbsrc.DBConnection.Qualifier;

            CManagedComponentWrapper dcomp = comp.Instantiate();

            //set Component Custom Properties
            foreach (KeyValuePair<string, object> prop in _src.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            if (comp.RuntimeConnectionCollection.Count > 0)
            {
                comp.RuntimeConnectionCollection[0].ConnectionManagerID = _cm.ID;
                comp.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(_cm);
            }

            // Finalize
            Reinitialize(dcomp);
            return comp;

        }
    }
}
