/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     02/20/2015
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

namespace WorkflowConsoleRunner
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

    public class ConsoleRunner : IDisposable
    {
        private bool disposed = false;
        private Process process = new Process();
        //private bool stdready = false;

        private int ExitCode = 1;
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

        public int Execute(string exePath, string args)
        {
            if (string.IsNullOrEmpty(exePath))
                throw new ArgumentException("Execute requires an executable file name", "exePath");

            string exe = Path.GetFileName(exePath);
            string wd = Path.GetDirectoryName(exePath);

            ProcessStartInfo ps = new ProcessStartInfo();
            ps.FileName = exePath;
            ps.Arguments = args;
            ps.WorkingDirectory = wd;
            ps.ErrorDialog = false;
            ps.CreateNoWindow = true;
            ps.UseShellExecute = false;
            ps.RedirectStandardOutput = true;
            ps.RedirectStandardError = true;
            process.StartInfo = ps;
            process.EnableRaisingEvents = true;
            int t = (Timeout <= 0) ? int.MaxValue - 1 : Timeout * 1000;

            // Set our event handler to asynchronously read the output.
            //process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            //process.Exited += new EventHandler(ExitHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorHandler);


            //p.Exited += delegate
            //{
            //    OnProcessExited(p);
            //    p.Dispose();
            //};
            try
            {
                process.Start();
                // Start the asynchronous read of the process output stream.
                //process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                //sb.Append(process.StandardOutput.ReadToEnd());
                //no timeout using this approach can be efficiently implemented
                string Result;
                while ((Result = process.StandardOutput.ReadLine()) != null)
                {
                    PostMessage(Result, 0);
                }

                process.WaitForExit(t);
                if (!process.HasExited)
                {
                    //process.CancelOutputRead();
                    process.CancelErrorRead();
                    process.Kill();
                    PostMessage("Process was killed due to timeout", 1);
                }
                //else
                //{
                //    while (!stdready)
                //    {
                //        System.Threading.Thread.Sleep(100);
                //    }
                //}

                //using (StringReader reader = new StringReader(sb.ToString()))
                //{
                //    string Result;
                //    while ((Result = reader.ReadLine()) != null)
                //    {
                //        PostMessage(Result);
                //    }
                //}

                ExitCode = process.ExitCode;
                if (ExitCode != 0)
                {
                    using (StringReader reader = new StringReader(sberr.ToString()))
                    {
                        while ((Result = reader.ReadLine()) != null)
                        {
                            PostMessage(Result, ExitCode);
                        }
                    }
                }

                //Close();
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
            Close();
        }

    }
}
