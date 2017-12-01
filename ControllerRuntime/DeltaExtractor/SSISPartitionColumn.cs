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
    public class SSISPartitionColumn : SSISModule
    {

        public SSISPartitionColumn(MainPipe pipe, IDTSComponentMetaData100 src, MoveData parameters, ILogger logger)
            : base(pipe, "PartitionData",0,"Microsoft.AdCenter.Jazz.PartitionData, PartitionData, Version=1.0.0.1, Culture=neutral, PublicKeyToken=5783f99a1329d562", logger)
        {
            // Create custom PartitionFunction component

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            //set Partition Function
            dcomp.SetComponentProperty("PartitionFunction", PartitionFunction(parameters.Partition.Function));
            dcomp.SetComponentProperty("PartitionFunctionOutput", parameters.Partition.Output);


            // Finalize
            this.Reinitialize(dcomp);
            this.ConnectComponents(src);

            //add partitionfunction input
            SetPartitionFunctionInput(parameters.Partition.Input, logger);

        }

        private int PartitionFunction(string function)
        {

            switch (function)
            {
                case "UPS":
                    return 0;
                case "SRC":
                    return 1;
                default :
                    return 1;
            }
        }

        private void SetPartitionFunctionInput(string ColumnName, ILogger logger)
        {

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();
            IDTSInput100 input = comp.InputCollection[0];
            IDTSVirtualInput100 vInput = input.GetVirtualInput();
            IDTSVirtualInputColumnCollection100 vColumns = vInput.VirtualInputColumnCollection;
            int inputColId = FindVirtualInputColumnId(vColumns, ColumnName);

            if (inputColId == 0)
            {
                //the column wasn't found if the Id is 0, so we'll print out a message and skip this row.
                logger.Information("DE Could not find partition function input column {ColName} in the source.",ColumnName);
            }
            else
            {
                dcomp.SetUsageType(input.ID, vInput, inputColId, DTSUsageType.UT_READONLY);
            }
        }
    }
}
