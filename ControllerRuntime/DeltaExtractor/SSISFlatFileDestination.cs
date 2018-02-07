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
    public class SSISFlatFileDestination : SSISModule, ISSISModule
    {
        private FlatFileDestination _dst;
        private ConnectionManager _cm;

        public SSISFlatFileDestination(FlatFileDestination dst, MainPipe pipe, ConnectionManager cm, ILogger logger,Application app)
            : base(pipe, "Flat File Destination", logger,app)
        {
            _dst = dst;
            _cm = cm;

        }
        public override IDTSComponentMetaData100 Initialize()
        {
            //create flat file destination component           
            IDTSComponentMetaData100 comp = base.Initialize();

            _cm.Name = $"FlatFile Destination Connection Manager {comp.ID}";

            //Create a new FlatFileDestination component

           CManagedComponentWrapper dcomp = comp.Instantiate();
            foreach (KeyValuePair<string, object> prop in _dst.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            /*Specify the connection manager for Src.The Connections class is a collection of the connection managers that have been added to that package and are available for use at run time*/
            if (comp.RuntimeConnectionCollection.Count > 0)
            {
                comp.RuntimeConnectionCollection[0].ConnectionManagerID = _cm.ID;
                comp.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(_cm);
            }

            this.Reinitialize(dcomp);
            return comp;

        }

    }
}
