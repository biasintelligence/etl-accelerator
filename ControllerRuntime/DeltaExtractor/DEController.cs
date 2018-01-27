using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Collections;
using System.Threading;
using task = System.Threading.Tasks;
using System.Data;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

using Serilog;
using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    /// <summary>
    /// The DEController class will execute parameters action
    /// only MoveData Action currently available
    /// </summary>
    public class DEController
    {

        #region Methods

        public delegate void DEAction<in T, in L, in C>(T obj, L logger, C CancellationToken);

        public void Execute(Parameters p, ILogger logger, CancellationToken token)
        {
            System.Diagnostics.Debug.Assert(p != null);

            Dictionary<string, DEAction<Parameters, ILogger, CancellationToken>> actions =
                 new Dictionary<string, DEAction<Parameters, ILogger,CancellationToken>>(StringComparer.InvariantCultureIgnoreCase) {
                {"MoveData", MoveDataRun },
                {"RunPackage", ExecPackageRun }
                 };

            if (actions.ContainsKey(p.Action))
            {
                actions[p.Action](p, logger, token);
            }

        }

        private void MoveDataRun(Parameters p, ILogger logger,CancellationToken token)
        {

            System.Diagnostics.Debug.Assert(p != null);

            MoveData action = p.MoveData;
            //Check the Source 
            if (action.DataSource.Type == SourceType.Unknown)
            {
                throw new UnknownSourceType();
            }
            logger.Information(action.DataSource.Description);

            //Check destinations
            int numValidDestinations = action.DataDestination.Test(logger);
            if (numValidDestinations == 0)
            {
                throw new InvalidDestinations("Error: No Valid destinations found");
            }
            if (numValidDestinations != action.DataDestination.Destinations.Count)
            {
                throw new InvalidDestinations("Error: Invalid destinations found");
            }

            //create and configure the package
            DESSISPackage Extractor = new DESSISPackage(action);
            Package pkg = Extractor.LoadPackage(logger);
            if (pkg == null)
            {
                throw new DeltaExtractorBuildException("Failed to Load or Build the SSIS Package");
            }

            ExecutePackageWithEvents(pkg, logger, token);

            int rowCount = Convert.ToInt32(pkg.Variables["RowCount"].Value, CultureInfo.InvariantCulture);
            logger.Information("DE extracted {Count} rows from the Source.", rowCount.ToString());
            logger.Information("DE Package completed.");
            //ETLController.CounterSet("RowsExtracted", rowCount.ToString());


            //if this is a staging extract, then call the upload sproc for each DB Destination
            //staging value defines which upsert type to use

            //IEnumerable<object> res = action.DataDestination.Destinations.Where(d => ((IDeDestination)d).Type == DestinationType.OleDb);
            foreach (object odest in action.DataDestination.Destinations)
            {
                IDeDestination dest = (IDeDestination)odest;
                if (dest.StagingBlock != null)
                {
                    if (dest.StagingBlock.Staging)
                    {
                        if (dest.DbSupportObject == null)
                        {
                            throw new DeltaExtractorBuildException("Staging support is not available for this destination");
                        }
                        IDeStagingSupport supp = (IDeStagingSupport)dest.DbSupportObject;
                        if (String.IsNullOrEmpty(dest.StagingBlock.StagingTableName))
                        {
                            if (!supp.CreateStagingTable(false, logger))
                            {
                                throw new CouldNotCreateStagingTableException(dest.StagingBlock.StagingTableName);
                            }
                        }
                        if (!supp.UploadStagingTable(p.RunID, logger))
                        {
                            throw new CouldNotUploadStagingTableException(dest.StagingBlock.StagingTableName);
                        }
                    }
                }
            }
        }


        private void ExecPackageRun(Parameters p, ILogger logger, CancellationToken token)
        {

            System.Diagnostics.Debug.Assert(p != null);

            RunPackage action = p.RunPackage;
            //Check the Source 
            if (action == null || String.IsNullOrEmpty(action.File))
            {
                throw new InvalidArgumentException("Error: No location to load the package from was supplied. Package can not be loaded.");
            }
            else if (!File.Exists(action.File))
            {
                throw new InvalidArgumentException(String.Format(CultureInfo.InvariantCulture, "Error: File not found {0}", action.File));
            }

            Application app = new Application();
            Package pkg = new Package();
            //load the package
            if (!File.Exists(action.File))
            {
                throw new InvalidArgumentException(String.Format(CultureInfo.InvariantCulture, "Error: File not found {0}", action.File));
            }
            else
            {
                logger.Information("DE trying to load the Package {File}", action.File);
                try
                {
                    pkg.LoadFromXML(action.File, null);
                    ExecutePackageWithEvents(pkg, logger, token);
                }
                catch (COMException cexp)
                {
                    logger.Error(cexp,"Exception occured: {Target}", cexp.TargetSite);
                    throw;
                }
            }

            //int rowCount = Convert.ToInt32(pkg.Variables["RowCount"].Value, CultureInfo.InvariantCulture);
            //PrintOutput.PrintToOutput("DE extracted " + rowCount.ToString() + " rows from the Source.");
            //PrintOutput.PrintToOutput("DE Package completed.");
            //ETLController.CounterSet("RowsExtracted", rowCount.ToString());

        }

        private void ExecutePackageWithEvents(Package pkg, ILogger logger, CancellationToken token)
        {
            try
            {

                var runPkg =
                task.Task.Factory.StartNew(() =>
                {
                    // Throw an exception if we get an error
                    logger.Information("Executing DE Package...");
                    SSISEvents ev = new SSISEvents(logger);
                    DTSExecResult rc = pkg.Execute(null, null, ev, null, null);
                    if (rc != DTSExecResult.Success)
                    {
                        logger.Error("Error: the DE failed to complete successfully {ErrorCode}.", 50000);
                        //StringBuilder dtserrors = new StringBuilder();
                        foreach (DtsError error in pkg.Errors)
                        {
                            logger.Error("Error: {Desc}, {ErrorCode}", error.Description, error.ErrorCode);
                            //dtserrors.AppendLine(error.Description);
                        }
                        throw new UnexpectedSsisException("SSIS Package execution failed");
                        //return false;
                    }


                }, token);

                runPkg.Wait();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                var app = new Application();
                var list = app.GetRunningPackages(".");

                logger.Debug("builed package: {PackageId}, {Name}", pkg.ID,pkg.Name);
                foreach (var p in list)
                {

                    logger.Debug("running package: {PackageId}, {Name}", p.PackageID,p.PackageName);
                    if (p.PackageID == Guid.Parse(pkg.ID))
                    {
                        p.Stop();
                        break;
                    }
                }
            }

        }   
        #endregion

    }
}