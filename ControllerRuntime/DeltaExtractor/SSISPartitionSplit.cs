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
    public class SSISPartitionSplit : SSISModule
    {
        public SSISPartitionSplit(MainPipe pipe, IDTSComponentMetaData100 src, MoveData parameters, ILogger logger)
            : base(pipe, "Conditional Split", logger)
        {
            // Create split component
            this.Reinitialize();
            this.ConnectComponents(src);

            //add partition column to input
            SetPartitionFunctionInput(parameters.Partition.Output, logger);
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
                logger.Information("DE Could not find partition function input column in the source.", ColumnName);
            }
            else
            {
                dcomp.SetUsageType(input.ID, vInput, inputColId, DTSUsageType.UT_READONLY);
            }
        }
    }
}
