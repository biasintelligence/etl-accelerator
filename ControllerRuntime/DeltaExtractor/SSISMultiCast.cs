using System;
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
    public class SSISMultiCast : SSISModule,ISSISModule
    {
        public SSISMultiCast(MainPipe pipe, ILogger logger,Application app)
            : base(pipe, "Multicast", logger, app)
        {

        }
        public override IDTSComponentMetaData100 Initialize()
        {
            // Create multicast component
            IDTSComponentMetaData100 comp = base.Initialize();
            Reinitialize();
            return comp;

        }
    }
}
