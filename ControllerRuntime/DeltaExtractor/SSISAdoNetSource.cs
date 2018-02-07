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
    public class SSISAdoNetSource : SSISModule,ISSISModule
    {
        private AdoNetSource _src;
        private ConnectionManager _cm;

        public SSISAdoNetSource(AdoNetSource src, MainPipe pipe, ConnectionManager cm, ILogger logger,Application app)
            : base(pipe, "ADO NET Source", logger,app)
        {
            _src = src;
            _cm = cm;
        }

        public override IDTSComponentMetaData100 Initialize()
        {
            IDTSComponentMetaData100 comp = base.Initialize();
            // create the adonet source
            //set connection properies
            _cm.Name = "AdoNet Source Connection Manager";
            _cm.ConnectionString = _src.ConnectionString;
            _cm.Description = _src.Description;
            //cm.Qualifier = "System.Data.SqlClient.SqlConnection, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            if (!String.IsNullOrEmpty(_src.DBConnection.Qualifier))
                _cm.Qualifier = _src.DBConnection.Qualifier;

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
