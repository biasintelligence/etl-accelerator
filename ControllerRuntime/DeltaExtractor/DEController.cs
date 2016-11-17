using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Runtime.InteropServices;
using System.Linq;

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

        public delegate void DEAction<in T, in L>(T obj, L logger);

        public static void Execute(Parameters p, IWorkflowLogger logger)
        {
           System.Diagnostics.Debug.Assert(p != null);

           Dictionary<string, DEAction<Parameters, IWorkflowLogger>> actions =
                new Dictionary<string, DEAction<Parameters, IWorkflowLogger>>(StringComparer.OrdinalIgnoreCase) {
                {"MoveData", MoveDataRun },
                {"RunPackage", ExecPackageRun }
                };

           if (actions.ContainsKey(p.Action))
           {
               actions[p.Action](p, logger);
           }
           else
           {
               DERun.DisplayHelp(logger);
           }

        }

        private static void MoveDataRun(Parameters p, IWorkflowLogger logger)
       {

            System.Diagnostics.Debug.Assert(p != null);

            MoveData action = p.MoveData;
            //Check the Source 
            if (action.DataSource.Type == SourceType.Unknown)
            {
                    throw new UnknownSourceType();
            }
            logger.Write(action.DataSource.Description);

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

            ExecutePackageWithEvents(pkg, logger);

            int rowCount = Convert.ToInt32(pkg.Variables["RowCount"].Value, CultureInfo.InvariantCulture);
            logger.Write("DE extracted " + rowCount.ToString() + " rows from the Source.");
            logger.Write("DE Package completed.");
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


        private static void ExecPackageRun(Parameters p, IWorkflowLogger logger)
       {

            System.Diagnostics.Debug.Assert(p != null);

            RunPackage action = p.RunPackage;
            //Check the Source 
            if (action == null || String.IsNullOrEmpty(action.File) )
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
                logger.Write(String.Format(CultureInfo.InvariantCulture, "DE trying to load the Package {0}", action.File));
                try
                {
                    pkg.LoadFromXML(action.File,null);
                    ExecutePackageWithEvents(pkg, logger);
                }
                catch (COMException cexp)
                {
                    logger.WriteError("Exception occured : " + cexp.TargetSite + cexp, cexp.HResult);
                    throw;
                }
            }

            //int rowCount = Convert.ToInt32(pkg.Variables["RowCount"].Value, CultureInfo.InvariantCulture);
            //PrintOutput.PrintToOutput("DE extracted " + rowCount.ToString() + " rows from the Source.");
            //PrintOutput.PrintToOutput("DE Package completed.");
            //ETLController.CounterSet("RowsExtracted", rowCount.ToString());

       }

       private static void ExecutePackageWithEvents(Package pkg, IWorkflowLogger logger)
       {
           // Throw an exception if we get an error

           logger.Write("Executing DE Package...");
           SSISEvents ev = new SSISEvents(logger);
           DTSExecResult rc = pkg.Execute(null, null,ev, null, null);
           if (rc != DTSExecResult.Success)
           {
               logger.WriteDebug("Error: the DE failed to complete successfully.");
               StringBuilder dtserrors = new StringBuilder();
               foreach (DtsError error in pkg.Errors)
               {
                   logger.WriteDebug(error.Description);
                   dtserrors.AppendLine(error.Description);
               }
               throw new UnexpectedSsisException(dtserrors.ToString());
               //return false;
           }
       }
   
        #endregion

    }
}