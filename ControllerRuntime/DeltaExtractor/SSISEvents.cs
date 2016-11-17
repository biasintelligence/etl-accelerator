using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{
    class SSISEvents:DefaultEvents
    {
        IWorkflowLogger _logger;

        public SSISEvents(IWorkflowLogger logger)
        {
            _logger = logger;
        }

        //public override void OnPreExecute(Executable exec, ref bool fireAgain)
        //{
        //    // TODO: Add custom code to handle the event.
        //    PrintOutput.PrintToOutput("Package Execution phase started... ",DERun.Debug);
        //}
        //public override void OnPreValidate(Executable exec, ref bool fireAgain)
        //{
        //    // TODO: Add custom code to handle the event.
        //    PrintOutput.PrintToOutput("Validation phase started...", DERun.Debug);
        //}
        //public override void OnPostValidate(Executable exec, ref bool fireAgain)
        //{
        //    // TODO: Add custom code to handle the event.
        //    PrintOutput.PrintToOutput("Validation phase finished...", DERun.Debug);
        //}
        public override void OnInformation(DtsObject source, int informationCode, string subComponent, string description, string helpFile, int helpContext, string idofInterfaceWithError, ref bool fireAgain)
        {
            // TODO: Add custom code to handle the event.
            _logger.WriteDebug(String.Format(CultureInfo.InvariantCulture, "Information: {0} - {1}", informationCode, description));
        }
        public override void OnWarning(DtsObject source,int warningCode,string subComponent,string description,string helpFile,int helpContext,string idofInterfaceWithError)
        {
            // TODO: Add custom code to handle the event.
            _logger.WriteDebug(String.Format(CultureInfo.InvariantCulture, "Warning: {0} - {1}", warningCode, description));
        }
        public override bool OnError(DtsObject source,int errorCode,string subComponent,string description,string helpFile,int helpContext,string idofInterfaceWithError)
        {
            // TODO: Add custom code to handle the event.
            _logger.WriteDebug(String.Format(CultureInfo.InvariantCulture,"Error: {0} - {1}", errorCode, description));
            return true;
        }
        //public override void OnCustomEvent(TaskHost taskHost,string eventName,string eventText,ref Object[] arguments,string subComponent,ref bool fireAgain)
        //{
        //    // TODO: Add custom code to handle the event.
        //    PrintOutput.PrintToOutput(String.Format(CultureInfo.InvariantCulture, "Error: {0} - {1}", eventName, eventText), DERun.Debug);
        //    return;
        //}

    }

    class SSISEventHandler : IDTSComponentEvents
    {
        private void HandleEvent(string type, string subComponent, string description)
        {
            //PrintOutput.PrintToOutput(String.Format(CultureInfo.InvariantCulture, "[{0}] {1}: {2}", type, subComponent, description), DERun.Debug);
        }

        private void HandleError(string type, int errorCode, string subComponent, string description)
        {
            //PrintOutput.PrintToError(String.Format(CultureInfo.InvariantCulture, "[{0}-{1}] {2}: {3}", type, errorCode, subComponent, description), DERun.Debug);
        }

        #region IDTSComponentEvents Members

        public void FireBreakpointHit(BreakpointTarget breakpointTarget)
        {
        }

        public void FireCustomEvent(string eventName, string eventText, ref object[] arguments, string subComponent, ref bool fireAgain)
        {
        }

        public bool FireError(int errorCode, string subComponent, string description, string helpFile, int helpContext)
        {
            HandleError("Error", errorCode, subComponent, description);
            return true;
        }

        public void FireInformation(int informationCode, string subComponent, string description, string helpFile, int helpContext, ref bool fireAgain)
        {
            HandleEvent("Information", subComponent, description);
        }

        public void FireProgress(string progressDescription, int percentComplete, int progressCountLow, int progressCountHigh, string subComponent, ref bool fireAgain)
        {
        }

        public bool FireQueryCancel()
        {
            return true;
        }

        public void FireWarning(int warningCode, string subComponent, string description, string helpFile, int helpContext)
        {
            HandleEvent("Warning", subComponent, description);
        }
        #endregion
    }

}

