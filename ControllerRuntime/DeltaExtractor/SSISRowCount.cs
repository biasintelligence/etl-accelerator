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
    public class SSISRowCount : SSISModule,ISSISModule
    {

        // Create row count component
        public SSISRowCount(MainPipe pipe, ILogger logger,Application app)
            : base(pipe, "Row Count", logger, app)
        {

        }
        public override IDTSComponentMetaData100 Initialize()
        {
            // Create rowcount component
            IDTSComponentMetaData100 comp = base.Initialize();
            CManagedComponentWrapper dcomp = comp.Instantiate();

            dcomp.SetComponentProperty("VariableName", "RowCount");
            this.Reinitialize(dcomp);
            return comp;
        }
    }
}
