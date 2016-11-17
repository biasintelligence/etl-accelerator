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

using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    public class SSISAdoNetDestination : SSISModule
    {

        public SSISAdoNetDestination(AdoNetDestination dbdst, MainPipe pipe, IDTSComponentMetaData100 src, int outputID, ConnectionManager cm, IWorkflowLogger logger)
            : base(pipe, "ADO NET Destination", outputID, logger)
        {

            //create Ado Net destination component           
            //set connection properties
            cm.Name = String.Format(CultureInfo.InvariantCulture, "AdoNet Destination Connection Manager {0}", outputID);
            cm.ConnectionString = dbdst.ConnectionString;
            cm.Description = dbdst.Description;
            //cm.Qualifier = dbdst.DBConnection.Qualifier;

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            // Set AdoNet destination custom properties
            foreach (KeyValuePair<string, object> prop in dbdst.CustomProperties.CustomPropertyCollection.InnerArrayList)
            {
                dcomp.SetComponentProperty(prop.Key, prop.Value);
            }

            //default - OpenRowset; ovveride OpenRowset with stagingtablename if staging is used
            if (!(dbdst.StagingBlock == null) && dbdst.StagingBlock.Staging)
            {
                dcomp.SetComponentProperty("TableOrViewName", dbdst.StagingBlock.StagingTableName);
            }
            else
            {
                dcomp.SetComponentProperty("TableOrViewName", dbdst.CustomProperties.TableOrViewName);
            }

            if (comp.RuntimeConnectionCollection.Count > 0)
            {
                comp.RuntimeConnectionCollection[0].ConnectionManagerID = cm.ID;
                comp.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(cm);
            }

            this.Reinitialize(dcomp);

            //Create datatype converter if needed
            Dictionary<string, int> converted = new Dictionary<string, int>();
            IDTSVirtualInput100 vInput = src.InputCollection[0].GetVirtualInput();
            if (this.needDataTypeChange(vInput, comp.InputCollection[0]))
            {
                //create the destination column collection
                Dictionary<string, MyColumn> exColumns = new Dictionary<string, MyColumn>();
                foreach (IDTSExternalMetadataColumn100 exColumn in comp.InputCollection[0].ExternalMetadataColumnCollection)
                {
                    MyColumn col = new MyColumn();
                    col.Name = exColumn.Name;
                    col.DataType = exColumn.DataType;
                    col.Length = exColumn.Length;
                    col.Precision = exColumn.Precision;
                    col.Scale = exColumn.Scale;
                    col.CodePage = exColumn.CodePage;
                    exColumns.Add(exColumn.Name, col);
                }
                SSISDataConverter ssisdc = new SSISDataConverter(pipe, src, outputID, exColumns,logger);
                src = ssisdc.MetadataCollection;
                converted = ssisdc.ConvertedColumns;
                outputID = 0;
            }

            this.ConnectComponents(src, outputID);
            this.MatchInputColumns(converted, true,logger);
        }

    }
}

