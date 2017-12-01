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
    public class SSISRowCount : SSISModule
    {

        // Create row count component
        public SSISRowCount(MainPipe pipe, IDTSComponentMetaData100 src, ILogger logger)
            : base(pipe, "Row Count", logger)
        {
            CManagedComponentWrapper dcomp = this.MetadataCollection.Instantiate();

            dcomp.SetComponentProperty("VariableName", "RowCount");
            this.Reinitialize(dcomp);
            this.ConnectComponents(src);
        }        
    }
}
