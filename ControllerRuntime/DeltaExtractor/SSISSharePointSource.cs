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
    public class SSISSharePointSource : SSISModule,ISSISModule
    {
        private SharePointSource _src;

        public SSISSharePointSource(SharePointSource src, MainPipe pipe, ILogger logger,Application app)
            //: base(pipe, "SharePoint List Source", 0, "Microsoft.Samples.SqlServer.SSIS.SharePointListAdapters.SharePointListSource, SharePointListAdapters, Version=1.2012.0.0, Culture=neutral, PublicKeyToken=f4b3011e1ece9d47", logger)
            : base(pipe, "SharePoint List Source", "Microsoft.Samples.SqlServer.SSIS.SharePointListAdapters.SharePointListSource, SharePointListAdapters, Version=1.2016.0.0, Culture=neutral, PublicKeyToken=f4b3011e1ece9d47", logger, app)
        {
            _src = src;
        }
        public override IDTSComponentMetaData100 Initialize()
        {
            // create the odbc source
            IDTSComponentMetaData100 comp = base.Initialize();
            CManagedComponentWrapper dcomp = comp.Instantiate();

            // Set SP custom properties
            foreach (KeyValuePair<string, object> prop in _src.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            Reinitialize(dcomp);
            return comp;

        }
    }
}
