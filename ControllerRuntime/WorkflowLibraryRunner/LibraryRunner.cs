/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Curtis Deems
**Date:     11/12/2015
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
*******************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using ControllerRuntime;


namespace WorkflowLibraryRunner
{
    public class HaveOutputEventArgs : EventArgs
    {
        private readonly string output;
        private readonly int err;
        public HaveOutputEventArgs(string Output, int Err)
        {
            this.output = Output;
            this.err = Err;
        }

        public string Output
        {
            get { return this.output; }
        }
        public int Err
        {
            get { return this.err; }
        }

    }

    public delegate void OutputEventHandler(object sender, HaveOutputEventArgs e);

    public class LibraryRunner : IDisposable
    {
        private bool disposed = false;
        private Process process = new Process();
        //private bool stdready = false;

        private StringBuilder sbstd = new StringBuilder("");
        private StringBuilder sberr = new StringBuilder("");
        public string stdOutput { get { return sbstd.ToString(); } }
        public string errOutput { get { return sberr.ToString(); } }
        public int Timeout { get; set; }

        public event OutputEventHandler haveOutput;
        protected virtual void OnHaveOutput(HaveOutputEventArgs e)
        {
            if (haveOutput != null)
                haveOutput(this, e);
        }

        public WfResult Execute(IWorkflowRunner runner, WorkflowActivityParameters args, IWorkflowLogger logger)
        {
            WfResult ExitCode;

            try
            {
                ExitCode = runner.Start(args, logger);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ExitCode;
        }

        private void PostMessage(string message, int err)
        {
            HaveOutputEventArgs e = new HaveOutputEventArgs(message, err);
            OnHaveOutput(e);
        }

        //private void OutputHandler(object process,DataReceivedEventArgs outLine)
        //{
        //    if (outLine.Data == null)
        //    { stdready = true; }

        //    sb.AppendLine(outLine.Data);
        //    //PostMessage(outLine.Data);
        //}
        private void ErrorHandler(object process, DataReceivedEventArgs outLine)
        {
            //ExitCode = 1;
            sberr.AppendLine(outLine.Data);
            //PostMessage(outLine.Data,1);
        }
        //private void ExitHandler(object process, EventArgs e)
        //{
        //    output.AppendLine("Process Exited");
        //}


        private void Close()
        {
            //if (!process.HasExited)
            //{
            //    process.CancelErrorRead();
            //    process.Kill();
            //}

            process.Close();
            process.Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Close();
                disposed = true;
            }
        }
    }


}
