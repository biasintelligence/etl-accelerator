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

namespace BIAS.Framework.DeltaExtractor
{
    public class SSISMultiCast : SSISModule
    {
        public SSISMultiCast(MainPipe pipe, IDTSComponentMetaData100 src)
            : base(pipe, "Multicast")
        {
            // Create multicast component
            this.Reinitialize();
            this.ConnectComponents(src);

        }
    }
}
