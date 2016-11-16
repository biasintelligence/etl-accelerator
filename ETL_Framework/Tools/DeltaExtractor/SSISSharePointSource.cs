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
    public class SSISSharePointSource : SSISModule
    {
        public SSISSharePointSource(SharePointSource spsrc, MainPipe pipe)
            : base(pipe, "SharePoint List Source", 0, "Microsoft.Samples.SqlServer.SSIS.SharePointListAdapters.SharePointListSource, SharePointListAdapters, Version=1.2012.0.0, Culture=neutral, PublicKeyToken=f4b3011e1ece9d47")
        {
            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            // Set SP custom properties
            foreach (KeyValuePair<string, object> prop in spsrc.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            this.Reinitialize(dcomp);

        }
    }
}
