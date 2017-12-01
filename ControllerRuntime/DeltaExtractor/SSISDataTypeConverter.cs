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
    public class SSISDataConverter : SSISModule
    {
        //exColId-vInputColId
        private Dictionary<int, int> m_converted = new Dictionary<int, int>();

        public SSISDataConverter(MainPipe pipe, IDTSComponentMetaData100 src, int outputID, IDTSExternalMetadataColumnCollection100 exColumns, ILogger logger)
            : base(pipe, "Data Conversion", outputID, logger)
        {
            //create datatype converter component

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            IDTSInput100 input = comp.InputCollection[0];
            input.ExternalMetadataColumnCollection.IsUsed = false;
            comp.InputCollection[0].HasSideEffects = false;

            this.Reinitialize(dcomp);
            this.ConnectComponents(src, outputID);

            this.PropagateInputColumns(exColumns);
        }

        public Dictionary<int, int> ConvertedColumns
        {
            get { return m_converted; }
        }

        private void PropagateInputColumns(IDTSExternalMetadataColumnCollection100 exColumns)
        {

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            IDTSInput100 input = comp.InputCollection[0];
            IDTSVirtualInput100 vInput = input.GetVirtualInput();
            IDTSVirtualInputColumnCollection100 vColumns = vInput.VirtualInputColumnCollection;

            IDTSOutput100 output = comp.OutputCollection[0];
            output.TruncationRowDisposition = DTSRowDisposition.RD_NotUsed;
            output.ErrorRowDisposition = DTSRowDisposition.RD_NotUsed;

            //create input columns for destination external
            foreach (IDTSExternalMetadataColumn100 exColumn in exColumns)
            {
                int vColumnID = FindVirtualInputColumnId(vColumns, exColumn.Name);
                if (vColumnID != 0)
                {
                    //do type conversion
                    IDTSVirtualInputColumn100 vColumn = vInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(vColumnID);
                    if (exColumn.DataType != vColumn.DataType
                        || vColumn.Length != exColumn.Length
                        || exColumn.Precision != vColumn.Precision
                        || exColumn.Scale != vColumn.Scale)
                    {

                        dcomp.SetUsageType(input.ID, vInput, vColumnID, DTSUsageType.UT_READONLY);

                        IDTSOutputColumn100 oColumn = dcomp.InsertOutputColumnAt(output.ID, 0, exColumn.Name, String.Empty);
                        //IDTSOutputColumn100 oColumn = output.OutputColumnCollection.New();
                        //oColumn.Name = exColumn.Name;
                        oColumn.SetDataTypeProperties(exColumn.DataType, exColumn.Length, exColumn.Precision, exColumn.Scale, exColumn.CodePage);
                        oColumn.ExternalMetadataColumnID = 0;
                        oColumn.MappedColumnID = 0;
                        oColumn.ErrorRowDisposition = DTSRowDisposition.RD_FailComponent;
                        oColumn.TruncationRowDisposition = DTSRowDisposition.RD_FailComponent;
                        //IDTSCustomProperty100 property = oColumn.CustomPropertyCollection.New();
                        //property.Name = "SourceInputColumnLineageID";
                        //property.Value = vColumnID;
                        //property = oColumn.CustomPropertyCollection.New();
                        //property.Name = "FastParse";
                        //property.Value = false;

                        dcomp.SetOutputColumnProperty(
                            output.ID,
                            oColumn.ID,
                            "SourceInputColumnLineageID",
                            vColumnID);

                        dcomp.SetOutputColumnProperty(
                            output.ID,
                            oColumn.ID,
                            "FastParse",
                            false);


                        //set of derived columns
                        m_converted.Add(exColumn.ID, oColumn.LineageID);
                    }
                    //else
                    //{
                    //    m_converted.Add(exColumn.ID, vColumnID);
                    //}
                }
                else
                {
                    //the column wasn't found if the Id is 0, so we'll print out a message and skip this row.
                    //PrintOutput.PrintToOutput("Converter: Could not map external column " + exColumn.Key + ". Skipping column.");
                }
            }
        }
    }
}
