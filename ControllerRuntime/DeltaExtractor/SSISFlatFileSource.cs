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
    public class SSISFlatFileSource : SSISModule,ISSISModule
    {
        private FlatFileSource _src;
        private ConnectionManager _cm;

        public SSISFlatFileSource(FlatFileSource src, MainPipe pipe,ConnectionManager cm, ILogger logger,Application app)
            : base(pipe, "Flat File Source", logger,app)
        {
            _src = src;
            _cm = cm;
        }

        public override IDTSComponentMetaData100 Initialize()
        {
            IDTSComponentMetaData100 comp = base.Initialize();
            CManagedComponentWrapper dcomp = comp.Instantiate();

            // Set flatfile custom properties
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

            Reinitialize(dcomp);
            return comp;

        }
    }
}
