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
    public class SSISOdbcDestination : SSISModule,ISSISModule
    {
        private OdbcDestination _dst;
        private ConnectionManager _cm;

        public SSISOdbcDestination(OdbcDestination dst, MainPipe pipe, ConnectionManager cm, ILogger logger,Application app)
            : base(pipe, "ODBC Destination", logger,app)
        {
            _dst = dst;
            _cm = cm;

        }
        public override IDTSComponentMetaData100 Initialize()
        {
            //create odbc destination component           
            IDTSComponentMetaData100 comp = base.Initialize();

            //set connection properties
            _cm.Name = $"Odbc Destination Connection Manager {comp.ID}";
            _cm.ConnectionString = _dst.ConnectionString;
            _cm.Description = _dst.Description;

            CManagedComponentWrapper dcomp = comp.Instantiate();

            // Set odbc destination custom properties
            foreach (KeyValuePair<string, object> prop in _dst.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            //default - OpenRowset; ovveride OpenRowset with stagingtablename if staging is used
            if (!(_dst.StagingBlock == null) && _dst.StagingBlock.Staging)
            {
                dcomp.SetComponentProperty("TableName", _dst.StagingBlock.StagingTableName.RemoveQuotes());
            }
            else
            {
                dcomp.SetComponentProperty("TableName", _dst.CustomProperties.TableName.RemoveQuotes());
            }

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

