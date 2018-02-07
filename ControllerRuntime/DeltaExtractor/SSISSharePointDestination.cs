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
    public class SSISSharePointDestination : SSISModule, ISSISModule
    {
        private SharePointDestination _dst;
        private ConnectionManager _cm;

        public SSISSharePointDestination(SharePointDestination dst, MainPipe pipe, ILogger logger,Application app)
        //: base(pipe, "SharePoint List Destination", outputID, "Microsoft.Samples.SqlServer.SSIS.SharePointListAdapters.SharePointListDestination, SharePointListAdapters, Version=1.2012.0.0, Culture=neutral, PublicKeyToken=f4b3011e1ece9d47", logger)
        : base(pipe, "SharePoint List Destination", "Microsoft.Samples.SqlServer.SSIS.SharePointListAdapters.SharePointListDestination, SharePointListAdapters, Version=1.2016.0.0, Culture=neutral, PublicKeyToken=f4b3011e1ece9d47", logger, app)
        {
            _dst = dst;
        }
        public override IDTSComponentMetaData100 Initialize()
        {
            //create SharePoint destination component           
            IDTSComponentMetaData100 comp = base.Initialize();
 
            CManagedComponentWrapper dcomp = comp.Instantiate();

            foreach (KeyValuePair<string, object> prop in _dst.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            this.Reinitialize(dcomp);
            return comp;

        }
    }
}
