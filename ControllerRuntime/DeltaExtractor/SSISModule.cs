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
    public abstract class SSISModule
    {
        protected MainPipe m_pipe;
        private int m_ID;


        protected SSISModule(MainPipe pipe, string module_name, int module_id,string module_clsid, IWorkflowLogger logger)
        {
            //create SSIS component

            m_pipe = pipe;

            IDTSComponentMetaData100 comp = pipe.ComponentMetaDataCollection.New();
            m_ID = comp.ID;
            Application app = new Application();
            comp.ComponentClassID = (String.IsNullOrEmpty(module_clsid)) ? app.PipelineComponentInfos[module_name].CreationName : module_clsid;
            CManagedComponentWrapper dcomp = comp.Instantiate();
            dcomp.ProvideComponentProperties();

            //set common SSIS module properties
            if (module_id == 0)
            {
                comp.Name = String.Format(CultureInfo.InvariantCulture, "{0}", module_name);
            }
            else
            {
                comp.Name = String.Format(CultureInfo.InvariantCulture, "{0} - {1}", module_name, module_id);
            }

            logger.WriteDebug(String.Format(CultureInfo.InvariantCulture, "DE added {0}", comp.Name));

        }

        protected SSISModule(MainPipe pipe, string module_name, int module_id, IWorkflowLogger logger)
            : this(pipe, module_name, module_id, String.Empty, logger)
        {
        }

        protected SSISModule(MainPipe pipe, string module_name, IWorkflowLogger logger)
            : this(pipe, module_name, 0, logger)
        {
        }


        public IDTSComponentMetaData100 MetadataCollection
        {
            get { return m_pipe.ComponentMetaDataCollection.GetObjectByID(m_ID); }
        }

        protected void Reinitialize(CManagedComponentWrapper dcomp)
        {
            // Finalize
            if (dcomp == null)
            {
                dcomp = MetadataCollection.Instantiate();
            }
            dcomp.AcquireConnections(null);
            dcomp.ReinitializeMetaData();
            dcomp.ReleaseConnections();

        }

        protected void Reinitialize()
        {
            Reinitialize(null);
        }


        protected void ConnectComponents(IDTSComponentMetaData100 src, int outputID)
        {
            if (src != null)
            {
                IDTSOutput100 output = (outputID == 0) ? src.OutputCollection[0] : src.OutputCollection.GetObjectByID(outputID);
                IDTSInput100 input = MetadataCollection.InputCollection[0];
                IDTSPath100 path = m_pipe.PathCollection.New();

                path.AttachPathAndPropagateNotifications(output, input);
            }
        }

        protected void ConnectComponents(IDTSComponentMetaData100 src)
        {
            ConnectComponents(src, 0);
        }


        //Loop through the Virtual Input column Collection, and see if one matches the name
        protected int FindVirtualInputColumnId(IDTSVirtualInputColumnCollection100 in_ColumnCollection, string in_columnName)
        {
            
            in_columnName = in_columnName.ToLower();
            foreach (IDTSVirtualInputColumn100 inputColumn in in_ColumnCollection)
            {
                if (inputColumn.Name.ToLower() == in_columnName)
                    return inputColumn.LineageID;
            }
            return 0;
        }

        protected virtual bool needDataTypeChange(IDTSVirtualInput100 vinput, IDTSInput100 input)
        {
            IDTSExternalMetadataColumnCollection100 exColumns = input.ExternalMetadataColumnCollection;
            foreach (IDTSExternalMetadataColumn100 exColumn in exColumns)
            {
                int vColumnID = FindVirtualInputColumnId(vinput.VirtualInputColumnCollection, exColumn.Name);
                if (vColumnID != 0)
                {
                    IDTSVirtualInputColumn100 vColumn = vinput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(vColumnID);
                    //allow converter only for simple within datatype change
                    if ( (exColumn.DataType == vColumn.DataType)
                            &&((exColumn.Length > 0 && exColumn.Length < vColumn.Length)
                            || exColumn.Precision != vColumn.Precision
                            || exColumn.Scale != vColumn.Scale)
                        )
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        protected virtual void MatchInputColumns(Dictionary<string, int> converted, bool needschema, IWorkflowLogger logger)
        {

            IDTSComponentMetaData100 comp = this.MetadataCollection;
            CManagedComponentWrapper dcomp = comp.Instantiate();

            IDTSInput100 input = comp.InputCollection[0];
            IDTSVirtualInput100 vInput = input.GetVirtualInput();
            IDTSVirtualInputColumnCollection100 vColumns = vInput.VirtualInputColumnCollection;
            IDTSExternalMetadataColumnCollection100 exColumns = input.ExternalMetadataColumnCollection;

            if (exColumns != null && exColumns.Count > 0)
            {
                bool hasMatch = false;
                foreach (IDTSExternalMetadataColumn100 exColumn in exColumns)
                {
                    int inputColId;
                    if (converted.ContainsKey(exColumn.Name.ToLower()))
                    {
                        inputColId = (int)converted[exColumn.Name.ToLower()];
                    }
                    else
                    {
                        inputColId = FindVirtualInputColumnId(vColumns, exColumn.Name);
                    }

                    if (inputColId == 0)
                    {
                        //the column wasn't found if the Id is 0, so we'll print out a message and skip this row.
                        logger.WriteDebug("DE could not map external column " + exColumn.Name + ". Skipping column.");
                    }
                    else
                    {
                        // create input column
                        IDTSInputColumn100 vCol = dcomp.SetUsageType(input.ID, vInput, inputColId, DTSUsageType.UT_READONLY);
                        // and then we'll map it to the input row.                            
                        dcomp.MapInputColumn(input.ID, vCol.ID, exColumn.ID);
                        hasMatch = true;
                    }
                }
                if (!hasMatch)
                {
                    throw new InvalidArgumentException("Unable to map input to destination");
                }
            }
            //if output schema is required and not provided
            else if (needschema)
            {
                //PrintOutput.PrintToError("No destination columns available");
                throw new InvalidArgumentException("No destination columns available");
            }
            //otherwise use virtual inputs
            else
            {
                foreach (IDTSVirtualInputColumn100 vColumn in vColumns)
                {
                    // create input column for all virtual input columns
                    dcomp.SetUsageType(input.ID, vInput, vColumn.LineageID, DTSUsageType.UT_READONLY);
                }
            }
        }
    }
}