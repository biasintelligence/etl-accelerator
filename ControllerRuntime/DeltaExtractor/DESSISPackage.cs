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

using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    public class DESSISPackage
    {
        private MoveData m_movedata;

        public DESSISPackage(MoveData p)
        {
            m_movedata = p;
        }

        public Package LoadPackage(IWorkflowLogger logger)
        {
            Application app = new Application();
            Package package;
            //Load Package from the file
            if (!(m_movedata.SavePackage == null) && m_movedata.SavePackage.Load)
            {
                if (String.IsNullOrEmpty(m_movedata.SavePackage.File))
                {
                    logger.Write("Warning: No location to load the package from was supplied. Package can not be loaded.");
                    package = this.BuildPackage(logger);
                }
                else if (!File.Exists(m_movedata.SavePackage.File))
                {
                    logger.WriteDebug(String.Format(CultureInfo.InvariantCulture, "Warning: File not found {0}", m_movedata.SavePackage.File));
                    package = this.BuildPackage(logger);
                }
                else
                {
                    logger.WriteDebug(String.Format(CultureInfo.InvariantCulture, "DE trying to load the Package {0}", m_movedata.SavePackage.File));
                    try
                    {
                        package = app.LoadPackage(m_movedata.SavePackage.File, null);
                    }
                    catch (COMException cexp)
                    {
                        logger.WriteError("Exception occured : " + cexp.TargetSite + cexp, cexp.HResult);
                        throw;
                    }
                }
            }
            else
            {
                package = this.BuildPackage(logger);
            }
            return package;

        }
        private Package BuildPackage(IWorkflowLogger logger)
        {
            Application app = new Application();
            Package package = new Package();
            try
            {
                logger.WriteDebug("DE building the Package...");

                //SSISEvents ev = new SSISEvents();
                //m_p.DesignEvents = ev;

                Executable ex = package.Executables.Add("STOCK:PipelineTask");
                TaskHost host = ex as TaskHost;
                host.Name = "DE Data Flow Task";
                MainPipe pipe = host.InnerObject as MainPipe;

                // Set the IDTSComponentEvent handler to capture the details from any 
                // COMExceptions raised during package generation
                SSISEventHandler events = new SSISEventHandler(logger);
                pipe.Events = DtsConvert.GetExtendedInterface(events as IDTSComponentEvents);

                // Add variable to point to staging area root
                dsv dsv = null;
                if (!String.IsNullOrEmpty(m_movedata.StagingAreaRoot))
                {
                    package.Variables.Add("StagingAreaRoot", true, "", m_movedata.StagingAreaRoot);
                    dsv = new dsv(m_movedata.StagingAreaRoot);
                }
                // Add variable RowCount
                package.Variables.Add("RowCount", false, "", 0);


                IDTSComponentMetaData100 src = null;

                //create FlatFile source
                if (m_movedata.DataSource.Type == SourceType.FlatFile)
                {
                    // use dsv as external metadata
                    bool bFound = false;
                    if (dsv != null)
                    {
                        bFound = dsv.FindTable(m_movedata.DataSource.FlatFileSource.CustomProperties.StagingAreaTableName);
                        if (!bFound)
                        {
                            logger.WriteDebug("Warning: DsvSchemaTable is not found");
                            //throw new DsvTableNotFound(m_movedata.StagingAreaRoot,m_movedata.DataSource.FlatFileSource.CustomProperties.StagingAreaTableName);
                        }
                    }

                    //Connection manager
                    ConnectionManager cm = package.Connections.Add("FLATFILE");

                    Dictionary<string, MyColumn> colCollection = (bFound) ? dsv.ColumnCollection : null;
                    SSISFlatFileConnection.ConfigureConnectionManager(cm, m_movedata.DataSource.FlatFileSource.CustomProperties.FlatFileConnectionProperties, colCollection, logger);
                    SSISFlatFileSource ssissource = new SSISFlatFileSource(m_movedata.DataSource.FlatFileSource, pipe, cm, logger);
                    src = ssissource.MetadataCollection;
                }
                //create Excel source
                else if (m_movedata.DataSource.Type == SourceType.Excel)
                {

                    //Connection manager
                    ConnectionManager cm = package.Connections.Add("EXCEL");
                    SSISExcelSource ssissource = new SSISExcelSource(m_movedata.DataSource.ExcelSource, pipe, cm, logger);
                    src = ssissource.MetadataCollection;
                }
                //create SharePoint source
                else if (m_movedata.DataSource.Type == SourceType.SPList)
                {
                    SSISSharePointSource ssissource = new SSISSharePointSource(m_movedata.DataSource.SharePointSource, pipe, logger);
                    src = ssissource.MetadataCollection;
                }
                //create OleDb source
                else if (m_movedata.DataSource.Type == SourceType.OleDb)
                {
                    //Add variable for SQL query if access mode is 3
                    package.Variables.Add("srcSelect", true, "", m_movedata.DataSource.OleDbSource.CustomProperties.SqlCommand);

                    //Connection manager
                    ConnectionManager cm = package.Connections.Add("OLEDB");
                    SSISOleDbSource ssissource = new SSISOleDbSource(m_movedata.DataSource.OleDbSource, pipe, cm, logger);
                    src = ssissource.MetadataCollection;
                }
                //create AdoNet source
                else if (m_movedata.DataSource.Type == SourceType.AdoNet)
                {
                    //Connection manager
                    ConnectionManager cm = package.Connections.Add("ADO.NET");
                    SSISAdoNetSource ssissource = new SSISAdoNetSource(m_movedata.DataSource.AdoNetSource, pipe, cm, logger);
                    src = ssissource.MetadataCollection;
                }
                //create Odbc source
                else if (m_movedata.DataSource.Type == SourceType.Odbc)
                {
                    //Connection manager
                    ConnectionManager cm = package.Connections.Add("ODBC");
                    SSISOdbcSource ssissource = new SSISOdbcSource(m_movedata.DataSource.OdbcSource, pipe, cm, logger);
                    src = ssissource.MetadataCollection;
                }
                //create OData source
                else if (m_movedata.DataSource.Type == SourceType.OData)
                {

                    //Connection manager
                    ConnectionManager cm = package.Connections.Add("ODATA");
                    SSISODataSource ssissource = new SSISODataSource(m_movedata.DataSource.ODataSource, pipe, cm, logger);
                    src = ssissource.MetadataCollection;
                }
                else
                {
                    throw new UnknownSourceType();
                }


                //create and connect rowcount to the source
                SSISRowCount ssiscount = new SSISRowCount(pipe, src, logger);
                src = ssiscount.MetadataCollection;


                if (m_movedata.Partition == null || String.IsNullOrEmpty(m_movedata.Partition.Function) || m_movedata.Partition.Function == "NONE")
                {
                    //create and connect multicast to the rowcount
                    SSISMultiCast ssissplit = new SSISMultiCast(pipe, src, logger);
                    src = ssissplit.MetadataCollection;
                }
                else
                {
                    //create and connect partition data custom component
                    SSISPartitionColumn ssispcol = new SSISPartitionColumn(pipe, src, m_movedata, logger);
                    src = ssispcol.MetadataCollection;

                    //create  and connect a partition splitter
                    SSISPartitionSplit ssissplit = new SSISPartitionSplit(pipe, src, m_movedata, logger);
                    src = ssissplit.MetadataCollection;
                }

                //connect none partition destinations to multicast
                //connect partition destinations to partition splitter
                CManagedComponentWrapper dsrc = src.Instantiate();



                foreach (object odst in m_movedata.DataDestination.Destinations)
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
                                logger.Write("Warning: DsvSchemaTable is not found");
                                //throw new DsvTableNotFound(m_movedata.StagingAreaRoot, dst.CustomProperties.StagingAreaTableName);
                            }
                        }

                        IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);

                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("FLATFILE");
                        //cm.Name = String.Format(CultureInfo.InvariantCulture, "FlatFile Destination Connection Manager {0}", output.ID);
                        Dictionary<string, MyColumn> colCollection = (bFound) ? dsv.ColumnCollection : getColumnCollectionFromPipe(src);
                        SSISFlatFileConnection.ConfigureConnectionManager(cm, dst.CustomProperties.FlatFileConnectionProperties, colCollection, logger);
                        SSISFlatFileDestination ssisdest = new SSISFlatFileDestination(dst, pipe, src, output.ID, cm, logger);
                    }


                    // OleDb Destinations
                    if (((IDeDestination)odst).Type == DestinationType.OleDb)
                    {

                        OleDbDestination dst = (OleDbDestination)odst;
                        IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("OLEDB");
                        //cm.Name = String.Format(CultureInfo.InvariantCulture, "OLEDB Destination Connection Manager {0}", output.ID);
                        SSISOleDbDestination ssisdest = new SSISOleDbDestination(dst, pipe, src, output.ID, cm, logger);
                    }


                    //ExcelDestinations
                    if (((IDeDestination)odst).Type == DestinationType.Excel)
                    {

                        ExcelDestination dst = (ExcelDestination)odst;
                        IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("EXCEL");
                        //cm.Name = String.Format(CultureInfo.InvariantCulture, "Excel Destination Connection Manager {0}", output.ID);
                        SSISExcelDestination ssisdest = new SSISExcelDestination(dst, pipe, src, output.ID, cm, logger);

                    }


                    // Create SharePointDestinations
                    if (((IDeDestination)odst).Type == DestinationType.SPList)
                    {
                        SharePointDestination dst = (SharePointDestination)odst;
                        IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                        SSISSharePointDestination ssisdest = new SSISSharePointDestination(dst, pipe, src, output.ID, logger);
                    }

                    // Ado Net Destinations
                    if (((IDeDestination)odst).Type == DestinationType.AdoNet)
                    {

                        AdoNetDestination dst = (AdoNetDestination)odst;
                        IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("ADO.NET");
                        //cm.Name = String.Format(CultureInfo.InvariantCulture, "ADONET Destination Connection Manager {0}", output.ID);
                        SSISAdoNetDestination ssisdest = new SSISAdoNetDestination(dst, pipe, src, output.ID, cm, logger);
                    }

                    // Odbc Destinations
                    if (((IDeDestination)odst).Type == DestinationType.Odbc)
                    {

                        OdbcDestination dst = (OdbcDestination)odst;
                        IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("ODBC");
                        //cm.Name = String.Format(CultureInfo.InvariantCulture, "ODBC Destination Connection Manager {0}", output.ID);
                        SSISOdbcDestination ssisdest = new SSISOdbcDestination(dst, pipe, src, output.ID, cm, logger);
                    }

                    // SqlBulk Destinations
                    if (((IDeDestination)odst).Type == DestinationType.SqlBulk)
                    {

                        SqlBulkDestination dst = (SqlBulkDestination)odst;
                        IDTSOutput100 output = ConfigureOutput(src, dst, dsrc);
                        //Connection manager
                        ConnectionManager cm = package.Connections.Add("OLEDB");
                        //cm.Name = String.Format(CultureInfo.InvariantCulture, "OLEDB Destination Connection Manager {0}", output.ID);
                        SSISSqlBulkDestination ssisdest = new SSISSqlBulkDestination(dst, pipe, src, output.ID, cm, logger);
                    }

                }

                logger.WriteDebug("DE Package is ready");

            }
            //catch (COMException cexp)
            catch (Exception cexp)
            {
                logger.WriteError("Exception occured : " + cexp.TargetSite + cexp, cexp.HResult);
                //StringBuilder dtserrors = new StringBuilder();
                foreach (DtsError error in package.Errors)
                {
                    logger.WriteError(error.Description, error.ErrorCode);
                    //dtserrors.AppendLine(error.Description);
                }
                if (package != null) package.Dispose();
                throw new UnexpectedSsisException("Failed to build SSIS package.");
            }
            finally
            {
                //Save the Package to XML
                if (!(m_movedata.SavePackage == null) && m_movedata.SavePackage.Save)
                {
                    if (String.IsNullOrEmpty(m_movedata.SavePackage.File))
                    {
                        logger.Write("No location to save the package was supplied. Package will not be saved to XML.");
                    }
                    else
                    {
                        app.SaveToXml(m_movedata.SavePackage.File, package, null);
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
            if (m_movedata.Partition == null || String.IsNullOrEmpty(m_movedata.Partition.Function) || m_movedata.Partition.Function == "NONE")
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
                    dcomp.SetOutputProperty(output.ID, "FriendlyExpression", String.Format(CultureInfo.InvariantCulture, "[{0}] == {1}", m_movedata.Partition.Output, dst.PartitionRange.Min));
                else
                    dcomp.SetOutputProperty(output.ID, "FriendlyExpression", String.Format(CultureInfo.InvariantCulture, "[{0}] >= {1} && [{0}] <= {2}", m_movedata.Partition.Output, dst.PartitionRange.Min, dst.PartitionRange.Max));
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
