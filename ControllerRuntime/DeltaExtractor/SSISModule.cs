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
    public abstract class SSISModule
    {
        protected MainPipe _pipe;
        private string _moduleName = String.Empty;
        private string _moduleClsid = String.Empty;
        protected ILogger _logger;
        private IDTSComponentMetaData100 _metadata = null;
        protected Application _app;


        protected SSISModule(MainPipe pipe, string moduleName, string moduleClsid, ILogger logger,Application app)
        {
            _pipe = pipe;
            _moduleName = moduleName;
            _moduleClsid = moduleClsid;
            _logger = logger;
            _app = app;
        }

        protected SSISModule(MainPipe pipe, string moduleName, ILogger logger,Application app)
            : this(pipe, moduleName, String.Empty, logger,app)
        {
        }

        public IDTSComponentMetaData100 MetadataCollection {get => _metadata; }

        public virtual IDTSComponentMetaData100 Initialize()
        {
            //create SSIS component

            _metadata = _pipe.ComponentMetaDataCollection.New();
            _metadata.ComponentClassID = (String.IsNullOrEmpty(_moduleClsid)) ? _app.PipelineComponentInfos[_moduleName].CreationName : _moduleClsid;
            CManagedComponentWrapper dcomp = _metadata.Instantiate();
            dcomp.ProvideComponentProperties();

            //set common SSIS module properties
            _metadata.Name = $"{_moduleName}-{_metadata.ID}";

            _logger.Debug("DE added {CompName}", _metadata.Name);
            return _metadata;

        }

        public virtual IDTSComponentMetaData100 Connect(IDTSComponentMetaData100 src, int outputID = 0)
        {
            ConnectComponents(src, outputID);
            return _metadata;
        }

        public virtual IDTSComponentMetaData100 ConnectDestination(IDTSComponentMetaData100 src, int outputID = 0)
        {
            //Create datatype converter if needed
            IDTSComponentMetaData100 comp = MetadataCollection;
            IDictionary<int, int> map = new Dictionary<int, int>();
            IDTSVirtualInput100 vInput = src.InputCollection[0].GetVirtualInput();

            var exColumns = comp.InputCollection[0].ExternalMetadataColumnCollection;
            if (this.needDataTypeChange(vInput, exColumns))
            {
                SSISDataConverter converter = new SSISDataConverter(_pipe, _logger, _app);
                var convComp = converter.Initialize();
                converter.Connect(src, outputID);
                map = converter.PropagateInputColumns(exColumns);
                src = convComp;
                outputID = 0;
            }

            ConnectComponents(src, outputID);
            MatchInputColumns(map, true);
            return _metadata;
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


        protected void ConnectComponents(IDTSComponentMetaData100 src, int outputID = 0)
        {
            if (src != null)
            {
                IDTSOutput100 output = (outputID == 0) ? src.OutputCollection[0] : src.OutputCollection.GetObjectByID(outputID);
                IDTSInput100 input = MetadataCollection.InputCollection[0];
                IDTSPath100 path = _pipe.PathCollection.New();

                path.AttachPathAndPropagateNotifications(output, input);
            }
        }

        //Loop through the Virtual Input column Collection, and see if one matches the name
        protected int FindVirtualInputColumnId(IDTSVirtualInputColumnCollection100 in_ColumnCollection, string in_columnName)
        {

            foreach (IDTSVirtualInputColumn100 inputColumn in in_ColumnCollection)
            {
                //[] brakets are removed to support MDX column name convention
                //ssis doesnt like destination names with []
                string inputCol = inputColumn.Name.Replace("[", "").Replace("]", "");
                string outputCol = in_columnName.Replace("[", "").Replace("]", "");
                if (inputCol.Equals(outputCol, StringComparison.InvariantCultureIgnoreCase))
                    return inputColumn.LineageID;
            }
            return 0;
        }

        protected virtual bool needDataTypeChange(IDTSVirtualInput100 vinput, IDTSExternalMetadataColumnCollection100 exColumns)
        {
            foreach (IDTSExternalMetadataColumn100 exColumn in exColumns)
            {
                int vColumnID = FindVirtualInputColumnId(vinput.VirtualInputColumnCollection, exColumn.Name);
                if (vColumnID != 0)
                {

                    IDTSVirtualInputColumn100 vColumn = vinput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(vColumnID);

                    //int exC = exColumn.Length;
                    //int vC = vColumn.Length;
                    //mwrt.DataType exDt = exColumn.DataType;
                    //mwrt.DataType vDt = vColumn.DataType;

                    //converter doesnt work on blobs
                    if (exColumn.Length > 4000)
                        continue;

                    //convert within the same datatype only
                        if (exColumn.DataType != vColumn.DataType
                            || exColumn.Length < vColumn.Length
                            || exColumn.Precision != vColumn.Precision
                            || exColumn.Scale != vColumn.Scale
                            )
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        protected virtual void MatchInputColumns(IDictionary<int, int> map, bool needschema)
        {

            IDTSComponentMetaData100 comp = MetadataCollection;
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
                    int inputColId = 0;
                    if (map.ContainsKey(exColumn.ID))
                    {
                        inputColId = map[exColumn.ID];
                    }
                    else
                    {
                        inputColId = FindVirtualInputColumnId(vColumns, exColumn.Name);
                    }

                    if (inputColId == 0)
                    {
                        //the column wasn't found if the Id is 0, so we'll print out a message and skip this row.
                        _logger.Debug("DE could not map external column {ColName}. Skipping column.", exColumn.Name);
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