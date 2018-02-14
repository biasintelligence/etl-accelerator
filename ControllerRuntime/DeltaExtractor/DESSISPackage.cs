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
using System.IO;
using System.Linq;

using mwrt = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Runtime.InteropServices;

using Serilog;
using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    public class DESSISPackage
    {
        private MoveData _movedata;
        private ILogger _logger;

        //allow only one SSIS package build at a time
        private static object _lockBuild = new object();

        public DESSISPackage(MoveData p,ILogger logger)
        {
            _movedata = p;
            _logger = logger;
        }

        public Package LoadPackage()
        {
            Package package;
            //Load Package from the file
            if (!(_movedata.SavePackage == null) && _movedata.SavePackage.Load)
            {
                if (String.IsNullOrEmpty(_movedata.SavePackage.File))
                {
                    _logger.Information("Warning: No location to load the package from was supplied. Package can not be loaded.");
                    package = BuildPackage();
                }
                else if (!File.Exists(_movedata.SavePackage.File))
                {
                    _logger.Debug("Warning: File not found {File}", _movedata.SavePackage.File);
                    package = BuildPackage();
                }
                else
                {
                    _logger.Debug("DE trying to load the Package {File}", _movedata.SavePackage.File);
                    try
                    {
                        Application app = new Application();
                        package = app.LoadPackage(_movedata.SavePackage.File, null);
                    }
                    catch (COMException cexp)
                    {
                        _logger.Error(cexp, "Exception occured {Target}: ", cexp.TargetSite);
                        throw;
                    }
                }
            }
            else
            {
                package = BuildPackage();
            }
            return package;

        }
        private Package BuildPackage()
        {

            Application app = new Application();
            _logger.Information($"component store path: {app.ComponentStorePath}");
            Package package = new Package();
            lock (_lockBuild)
            {
                package.Name = Guid.NewGuid().ToString();
                try
                {
                    _logger.Debug("DE building the Package {Name}...", package.Name);

                    //SSISEvents ev = new SSISEvents();
                    //_p.DesignEvents = ev;

                    //Executable ex = package.Executables.Add("SSIS.Pipeline");
                    Executable ex = package.Executables.Add("STOCK:PipelineTask");
                    TaskHost host = ex as TaskHost;
                    host.Name = $"DE Data Flow Task {package.ID}";
                    MainPipe pipe = host.InnerObject as MainPipe;
                    _logger.Debug($"package creation name: {package.CreationName}");
                    _logger.Debug($"host creation name: {host.CreationName}");


                    // Set the IDTSComponentEvent handler to capture the details from any 
                    // COMExceptions raised during package generation
                    SSISEventHandler events = new SSISEventHandler(_logger);
                    pipe.Events = DtsConvert.GetExtendedInterface(events as IDTSComponentEvents);

                    // Add variable to point to staging area root
                    dsv dsv = null;
                    if (!String.IsNullOrEmpty(_movedata.StagingAreaRoot))
                    {
                        package.Variables.Add("StagingAreaRoot", true, "", _movedata.StagingAreaRoot);
                        dsv = new dsv(_movedata.StagingAreaRoot);
                    }
                    // Add variable RowCount
                    package.Variables.Add("RowCount", false, "", 0);


                    IDTSComponentMetaData100 src = null;
                    IDTSComponentMetaData100 current = null;
                    ISSISModule module;

                    //create FlatFile source
                    if (_movedata.DataSource.Type == SourceType.FlatFile)
                    {
                        // use dsv as external metadata
                        bool bFound = false;
                        if (dsv != null)
                        {
                            bFound = dsv.FindTable(_movedata.DataSource.FlatFileSource.CustomProperties.StagingAreaTableName);
                            if (!bFound)
                            {
                                _logger.Debug("Warning: DsvSchemaTable is not found");
                                //throw new DsvTableNotFound(_movedata.StagingAreaRoot,_movedata.DataSource.FlatFileSource.CustomProperties.StagingAreaTableName);
                            }
                        }

                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("FLATFILE");

                        Dictionary<string, MyColumn> colCollection = (bFound) ? dsv.ColumnCollection : null;
                        SSISFlatFileConnection.ConfigureConnectionManager(cm, _movedata.DataSource.FlatFileSource.CustomProperties.FlatFileConnectionProperties, colCollection, _logger);
                        module = new SSISFlatFileSource(_movedata.DataSource.FlatFileSource, pipe, cm, _logger, app);
                        src = module.Initialize();
                    }
                    //create Excel source
                    else if (_movedata.DataSource.Type == SourceType.Excel)
                    {

                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("EXCEL");
                        module = new SSISExcelSource(_movedata.DataSource.ExcelSource, pipe, cm, _logger, app);
                        src = module.Initialize();
                    }
                    //create SharePoint source
                    else if (_movedata.DataSource.Type == SourceType.SPList)
                    {
                        module = new SSISSharePointSource(_movedata.DataSource.SharePointSource, pipe, _logger, app);
                        src = module.Initialize();
                    }
                    //create OleDb source
                    else if (_movedata.DataSource.Type == SourceType.OleDb)
                    {
                        //Add variable for SQL query if access mode is 3
                        package.Variables.Add("srcSelect", true, "", _movedata.DataSource.OleDbSource.CustomProperties.SqlCommand);

                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("OLEDB");
                        module = new SSISOleDbSource(_movedata.DataSource.OleDbSource, pipe, cm, _logger, app);
                        src = module.Initialize();
                    }
                    //create AdoNet source
                    else if (_movedata.DataSource.Type == SourceType.AdoNet)
                    {
                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("ADO.NET");
                        module = new SSISAdoNetSource(_movedata.DataSource.AdoNetSource, pipe, cm, _logger, app);
                        src = module.Initialize();
                    }
                    //create Odbc source
                    else if (_movedata.DataSource.Type == SourceType.Odbc)
                    {
                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("ODBC");
                        module = new SSISOdbcSource(_movedata.DataSource.OdbcSource, pipe, cm, _logger, app);
                        src = module.Initialize();
                    }
                    //create OData source
                    else if (_movedata.DataSource.Type == SourceType.OData)
                    {

                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("ODATA");
                        module = new SSISODataSource(_movedata.DataSource.ODataSource, pipe, cm, _logger, app);
                        src = module.Initialize();
                    }
                    else
                    {
                        throw new UnknownSourceType();
                    }


                    //create and connect rowcount to the source
                    module = new SSISRowCount(pipe, _logger, app);
                    current = module.Initialize();
                    src = module.Connect(src);

                    if (_movedata.Partition == null || String.IsNullOrEmpty(_movedata.Partition.Function) || _movedata.Partition.Function == "NONE")
                    {
                        //create and connect multicast to the rowcount
                        module = new SSISMultiCast(pipe, _logger, app);
                        current = module.Initialize();
                        src = module.Connect(src);
                    }
                    else
                    {
                        //create and connect partition data custom component
                        module = new SSISPartitionColumn(_movedata, pipe, _logger, app);
                        current = module.Initialize();
                        src = module.Connect(src);

                        //create  and connect a partition splitter
                        module = new SSISPartitionSplit(_movedata, pipe, _logger, app);
                        current = module.Initialize();
                        src = module.Connect(src);
                    }

                    //connect none partition destinations to multicast
                    //connect partition destinations to partition splitter
                    CManagedComponentWrapper dsrc = src.Instantiate();

                    foreach (object odst in _movedata.DataDestination.Destinations)
                    {

                        //FlatFile Destinations
                        if (((IDeDestination)odst).Type == DestinationType.FlatFile)
                        {
                            FlatFileDestination dst = (FlatFileDestination)odst;
                            // use dsv as external metadata
                            bool bFound = false;
                            if (dsv != null)
                            {
                                bFound = dsv.FindTable(dst.CustomProperties.StagingAreaTableName);
                                if (!bFound)
                                {
                                    _logger.Information("Warning: DsvSchemaTable is not found");
                                    //throw new DsvTableNotFound(_movedata.StagingAreaRoot, dst.CustomProperties.StagingAreaTableName);
                                }
                            }

                            IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);

                            //Connection manager
                            ConnectionManager cm = package.Connections.Add("FLATFILE");
                            //cm.Name = String.Format(CultureInfo.InvariantCulture, "FlatFile Destination Connection Manager {0}", output.ID);
                            Dictionary<string, MyColumn> colCollection = (bFound) ? dsv.ColumnCollection : getColumnCollectionFromPipe(src);
                            SSISFlatFileConnection.ConfigureConnectionManager(cm, dst.CustomProperties.FlatFileConnectionProperties, colCollection, _logger);
                            module = new SSISFlatFileDestination(dst, pipe, cm, _logger, app);
                            current = module.Initialize();
                            src = module.ConnectDestination(src, output.ID);
                        }


                        // OleDb Destinations
                        if (((IDeDestination)odst).Type == DestinationType.OleDb)
                        {

                            OleDbDestination dst = (OleDbDestination)odst;
                            IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                            //Connection manager
                            ConnectionManager cm = package.Connections.Add("OLEDB");
                            //cm.Name = String.Format(CultureInfo.InvariantCulture, "OLEDB Destination Connection Manager {0}", output.ID);
                            module = new SSISOleDbDestination(dst, pipe, cm, _logger, app);
                            current = module.Initialize();
                            src = module.ConnectDestination(src, output.ID);

                        }


                        //ExcelDestinations
                        if (((IDeDestination)odst).Type == DestinationType.Excel)
                        {

                            ExcelDestination dst = (ExcelDestination)odst;
                            IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                            //Connection manager
                            ConnectionManager cm = package.Connections.Add("EXCEL");
                            //cm.Name = String.Format(CultureInfo.InvariantCulture, "Excel Destination Connection Manager {0}", output.ID);
                            module = new SSISExcelDestination(dst, pipe, cm, _logger, app);
                            current = module.Initialize();
                            src = module.ConnectDestination(src, output.ID);

                        }


                        // Create SharePointDestinations
                        if (((IDeDestination)odst).Type == DestinationType.SPList)
                        {
                            SharePointDestination dst = (SharePointDestination)odst;
                            IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                            module = new SSISSharePointDestination(dst, pipe, _logger, app);
                            current = module.Initialize();
                            src = module.ConnectDestination(src, output.ID);
                        }

                        // Ado Net Destinations
                        if (((IDeDestination)odst).Type == DestinationType.AdoNet)
                        {

                            AdoNetDestination dst = (AdoNetDestination)odst;
                            IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                            //Connection manager
                            ConnectionManager cm = package.Connections.Add("ADO.NET");
                            //cm.Name = String.Format(CultureInfo.InvariantCulture, "ADONET Destination Connection Manager {0}", output.ID);
                            module = new SSISAdoNetDestination(dst, pipe, cm, _logger, app);
                            current = module.Initialize();
                            src = module.ConnectDestination(src, output.ID);
                        }

                        // Odbc Destinations
                        if (((IDeDestination)odst).Type == DestinationType.Odbc)
                        {

                            OdbcDestination dst = (OdbcDestination)odst;
                            IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                            //Connection manager
                            ConnectionManager cm = package.Connections.Add("ODBC");
                            //cm.Name = String.Format(CultureInfo.InvariantCulture, "ODBC Destination Connection Manager {0}", output.ID);
                            module = new SSISOdbcDestination(dst, pipe, cm, _logger, app);
                            current = module.Initialize();
                            src = module.ConnectDestination(src, output.ID);
                        }

                        // SqlBulk Destinations
                        if (((IDeDestination)odst).Type == DestinationType.SqlBulk)
                        {

                            SqlBulkDestination dst = (SqlBulkDestination)odst;
                            IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                            //Connection manager
                            ConnectionManager cm = package.Connections.Add("OLEDB");
                            //cm.Name = String.Format(CultureInfo.InvariantCulture, "OLEDB Destination Connection Manager {0}", output.ID);
                            module = new SSISSqlBulkDestination(dst, pipe, cm, _logger, app);
                            current = module.Initialize();
                            src = module.ConnectDestination(src, output.ID);
                        }

                    }

                    _logger.Debug("DE Package is ready");

                }
                //catch (COMException cexp)
                catch (Exception cexp)
                {
                    _logger.Error(cexp, "Exception occured: {Target}.", cexp.TargetSite);
                    //StringBuilder dtserrors = new StringBuilder();
                    foreach (DtsError error in package.Errors)
                    {
                        _logger.Error("Error: {Desc}, {ErrorCode}", error.Description, error.ErrorCode);
                        //dtserrors.AppendLine(error.Description);
                    }
                    if (package != null) package.Dispose();
                    throw new UnexpectedSsisException("Failed to build SSIS package.");
                }
                finally
                {
                    //Save the Package to XML
                    if (!(_movedata.SavePackage == null) && _movedata.SavePackage.Save)
                    {
                        if (String.IsNullOrEmpty(_movedata.SavePackage.File))
                        {
                            _logger.Information("No location to save the package was supplied. Package will not be saved to XML.");
                        }
                        else
                        {
                            app.SaveToXml(_movedata.SavePackage.File, package, null);
                        }
                    }
                }
            }
            return package;

        }

        private IDTSOutput100 ConfigureOutput(IDTSComponentMetaData100 src, IDeDestination dst)
        {
            return ConfigureOutput(src, dst, null);
        }

        private IDTSOutput100 ConfigureOutput(IDTSComponentMetaData100 src, IDeDestination dst, CManagedComponentWrapper dcomp)
        {
            if (dcomp == null)
            {
                dcomp = src.Instantiate();
            }

            IDTSOutput100 output = null;
            if (_movedata.Partition == null || String.IsNullOrEmpty(_movedata.Partition.Function) || _movedata.Partition.Function == "NONE")
            {
                output = src.OutputCollection.New();
                output.Name = String.Format(CultureInfo.InvariantCulture, "Full Output to {0}", output.ID);
                output.SynchronousInputID = src.InputCollection[0].ID;
            }
            else
            {
                //connect to splitter, make sure the default output is the last
                output = dcomp.InsertOutput(DTSInsertPlacement.IP_BEFORE, src.OutputCollection[0].ID);
                output.Name = String.Format(CultureInfo.InvariantCulture, "Partitioned Output to {0}", output.ID);
                output.Description = String.Format(CultureInfo.InvariantCulture, "Partitions {0}-{1} ", dst.PartitionRange.Min, dst.PartitionRange.Max);
                output.SynchronousInputID = src.InputCollection[0].ID;
                output.IsErrorOut = false;

                // Note: You will get an exception if you try to set these properties on the Default Output.
                dcomp.SetOutputProperty(output.ID, "EvaluationOrder", 0);
                if (dst.PartitionRange.Min == dst.PartitionRange.Max)
                    dcomp.SetOutputProperty(output.ID, "FriendlyExpression", String.Format(CultureInfo.InvariantCulture, "[{0}] == {1}", _movedata.Partition.Output, dst.PartitionRange.Min));
                else
                    dcomp.SetOutputProperty(output.ID, "FriendlyExpression", String.Format(CultureInfo.InvariantCulture, "[{0}] >= {1} && [{0}] <= {2}", _movedata.Partition.Output, dst.PartitionRange.Min, dst.PartitionRange.Max));
            }

            return output;

        }

        private Dictionary<string, MyColumn> getColumnCollectionFromPipe(IDTSComponentMetaData100 comp)
        {
            IDTSVirtualInput100 vinput = comp.InputCollection[0].GetVirtualInput();
            Dictionary<string, MyColumn> colCollection = null;

            if (vinput.VirtualInputColumnCollection.Count > 0)
            {
                //define input column
                colCollection = new Dictionary<string, MyColumn>();
                foreach (IDTSVirtualInputColumn100 vCol in vinput.VirtualInputColumnCollection)
                {
                    MyColumn col = new MyColumn();
                    col.Name = vCol.Name;
                    col.DataType = vCol.DataType;
                    col.Length = vCol.Length;
                    col.Precision = vCol.Precision;
                    col.Scale = vCol.Scale;
                    colCollection.Add(col.Name, col);
                }
            }
            return colCollection;
        }

    }
}
