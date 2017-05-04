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
// 2017-01-25       andrey              fix cancel ping logic
// 2017-05-03       andrey              do not wait for cancel thread.
//                                      Use Task.Delay instead of thread.sleep

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControllerRuntime
{

    public enum WfStatus
    {
        Unknown,
        Running,
        Suspended,
        Succeeded,
        Failed,
        Disabled
    }
    
    /// <summary>
    /// Workflow Result class. Workflow Status communication token. 
    /// </summary>
    public class WfResult
    {

        protected WfResult (WfStatus Status, string Message, int Error)
        {
            this.StatusCode = Status;
            this.Message = Message;
            this.ErrorCode = Error;
        }

        public static WfResult Create(WfStatus Status, string Message, int Error)
        {
            return new WfResult (Status, Message, Error);
        }

         public static WfResult Create(WfResult result)
        {
            return new WfResult (result.StatusCode, result.Message, result.ErrorCode);
        }

        public static WfResult Started
        { get { return new WfResult(WfStatus.Running, "Started", 0); } }

        public static WfResult Succeeded
        { get { return new WfResult(WfStatus.Succeeded, "Succeeded", 0); } }

        public static WfResult Canceled
        { get { return new WfResult(WfStatus.Failed, "Canceled", 0); } }

        public static WfResult Failed
        { get { return new WfResult(WfStatus.Failed, "Failed", -1); } }

        public static WfResult Paused
        { get { return new WfResult(WfStatus.Suspended, "Paused", 0); } }
        public static WfResult Unknown
        { get { return new WfResult(WfStatus.Unknown, "Not Started", 0); } }

        public void SetTo(WfResult result)
        {
            this.StatusCode = result.StatusCode;
            this.Message = result.Message;
            this.ErrorCode = result.ErrorCode;
        }


        public WfStatus StatusCode
        {
            get{return wf_status;}
            set{wf_status = value;}
        }
        private WfStatus wf_status = WfStatus.Unknown;

        public string Message
        { get; set; }

        public int ErrorCode
        { get; set; }

        public void Clear()
        {
            ErrorCode = 0;
            Message = String.Empty;
            StatusCode = WfStatus.Unknown;
        }

    }
    /// <summary>
    /// Workflow Processor.
    /// Request steps from Workflow Step dispatcher (WorkflowGrath)
    /// and submit them for execution
    /// </summary>
    public class WorkflowProcessor :IWorkflowCommand, IDisposable
    {
        private DBController _db;
        private WorkflowGraph _wfg;
        private Workflow _wf;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private IWorkflowLogger _logger;

        //<stepKey,Task>
        Dictionary<string, Task> _tasks = new Dictionary<string, Task>();
        Dictionary<string, WorkflowStepProcessor> _step_command = new Dictionary<string, WorkflowStepProcessor>();
        Task cancel_task;

        private bool _is_initialized = false;
        private bool _debug = false;
        private bool _verbose = false;
        private bool _forcestart = false;

        public WorkflowProcessor (string Name)
        {
            _processor_name = Name;
            //before db logger is available we log just to console
            _logger = new WorkflowConsoleLogger(true, true);
        }

        private string _processor_name = "Default";
        public string ProcessorName
        { get { return _processor_name; } }

        public string ConnectionString
        { get; set;}

        public string WorkflowName
        { get; set; }

        public IWorkflowCommand GetStepCommand(string Key)
        {
            if (_step_command.ContainsKey(Key))
                return (IWorkflowCommand) _step_command[Key];

            return null;
        }

        public WfResult Run(string[] Options)
        {

            SetOptions(Options);
            WorkflowStep step = null;
            try
            {
                _db = DBController.Create(ConnectionString, _debug, _verbose);
                _wf = _db.WorkflowMetadataGet(WorkflowName);
                _is_initialized = _db.WorkflowInitialize(_processor_name, _wf, _forcestart);

                //now we can start logging to the Controller
                _logger = _db.GetLogger(_wf.WorkflowId, 0, 0, _wf.RunId);

                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                _logger.Write(String.Format("Workflow Runner v.{0} ({1})bit", v.ToString(), 8 * IntPtr.Size));
                _logger.Write(String.Format("Start Processing Workflow {0}", _wf.WorkflowName));

                _wfg = WorkflowGraph.Create(_wf, _db);
                _wfg.Start();

                //timeout settings
                DateTime start_dt = DateTime.Now;
                TimeSpan lifetime = TimeSpan.FromSeconds(_wf.Timeout);
                TimeSpan timeout = lifetime;

                //start Workflow cancellation listener
                cancel_task = 
                Task.Factory.StartNew(() =>
                {

                    while (!_cts.IsCancellationRequested)
                    {
                        Task.Delay(TimeSpan.FromSeconds(_wf.Ping), _cts.Token).Wait();
                        if (_cts.IsCancellationRequested) break;

                        //Thread.Sleep(TimeSpan.FromSeconds(_wf.Ping));
                        WfResult cancel_result = _db.WorkflowExitEventCheck(_wf.WorkflowId, 0, _wf.RunId);
                        if (cancel_result.StatusCode != WfStatus.Running)
                        {
                            _cts.Cancel();
                            if (_cts.IsCancellationRequested) break;
                            //_cts.Token.ThrowIfCancellationRequested();
                        }
                    }

                },_cts.Token);

                while (_wfg.TryTake(out step, timeout))
                {
                    //tell dispatcher we are starting the step execution
                    ReportStepResult(step, WfResult.Started);
                    //loop steps
                    if (_tasks.ContainsKey(step.Key))
                    {
                        if (!_tasks[step.Key].IsCompleted)
                            throw new InvalidOperationException(String.Format("Task {0} is not completed",step.Key));

                        _tasks[step.Key].Dispose();
                        _tasks.Remove(step.Key);
                    }

                    if (!_step_command.ContainsKey(step.Key))
                    {
                        _step_command.Add(step.Key, new WorkflowStepProcessor(step, _db));
                    }

                    _tasks.Add(step.Key,
                    Task.Factory.StartNew((object obj) =>
                    {
                        WorkflowStep iter_step = obj as WorkflowStep;
                        WorkflowStepProcessor sp = _step_command[iter_step.Key];
                        //check for the step cancelation condition from the runtime before starting
                        WfResult cancel_result = _db.WorkflowExitEventCheck(iter_step.WorkflowId, iter_step.StepId, iter_step.RunId);
                        if (cancel_result.StatusCode == WfStatus.Running)
                        {
                            WfResult step_result = sp.Run(_cts.Token);
                            ReportStepResult(iter_step, step_result);
                        }
                        else
                        {
                            ReportStepResult(iter_step, cancel_result);
                        }

                    }, step,_cts.Token));

                    timeout = lifetime - DateTime.Now.Subtract(start_dt);
                }

                //after workflow Dispatcher is done, we wait for the still running steps completion.
                //or exit on timeout
                timeout = lifetime - DateTime.Now.Subtract(start_dt);
                if (timeout <= TimeSpan.Zero || !Task.WaitAll(_tasks.Values.ToArray(), timeout))
                {
                    _cts.Cancel();
                    Task.WaitAll(_tasks.Values.ToArray());
                    //cancel_task.Wait();
                    throw new TimeoutException("Timeout was reached");
                }
                else
                {
                    _cts.Cancel();
                    //cancel_task.Wait();
                }

            }
            catch (AggregateException aex)
            {
                _logger.WriteError(String.Format("AggregateException: {0}", aex.Message), aex.HResult);
                foreach (var ex in aex.InnerExceptions)
                {
                    _logger.WriteError(String.Format("InnerException: {0}", ex.Message), ex.HResult);
                }
                //return WfResult.Create(WfStatus.Failed, aex.Message, -10);
            }
            catch (Exception ex)
            {
                _logger.WriteError(String.Format("Exception: {0}", ex.Message), ex.HResult);
                //return WfResult.Create(WfStatus.Failed, ex.Message, -10);
            }
            finally
            {
                if (_db != null && _is_initialized)
                {
                    _db.WorkflowFinalize(_wf, _wfg.WorkflowCompleteStatus);
                    _logger.Write(String.Format("Finish Processing Workflow {0} with result - {1}"
                        , _wf.WorkflowName, _wfg.WorkflowCompleteStatus.StatusCode.ToString()));
                }
            }

            return (_wfg == null) ? WfResult.Failed : _wfg.WorkflowCompleteStatus;
        }


        private void SetOptions(string[] options)
        {
            List<string> option_list = new List<string>(options);
            if (option_list.Contains("debug", StringComparer.InvariantCultureIgnoreCase))
                _debug = true;
            if (option_list.Contains("verbose", StringComparer.InvariantCultureIgnoreCase))
                _verbose = true;
            if (option_list.Contains("forcestart",StringComparer.InvariantCultureIgnoreCase))
                _forcestart = true;

        }

        private void ReportStepResult(WorkflowStep step, WfResult result)
        {
            _db.WorkflowStepStatusSet(step, result);
            _wfg.SetNodeExecutionResult(step.Key, result);
        }

        #region IWorkflowCommand
        public WfResult Start()
        {
            return WfResult.Create(WfStatus.Failed, "Not Implemented",-1);
        }
        public WfResult Stop()
        {
            return WfResult.Create(WfStatus.Failed, "Not Implemented", -1);
        }
        public WfResult Pause()
        {
            return WfResult.Create(WfStatus.Failed, "Not Implemented", -1);
        }
        public WfResult Resume()
        {
            return WfResult.Create(WfStatus.Failed, "Not Implemented", -1);
        }

        public WfResult Status
        {
            get
            { 
                if (_wfg == null)
                    return WfResult.Unknown;

                return _wfg.WorkflowRunStatus;
            }
        }

        #endregion

        void IDisposable.Dispose()
        {
            if (!_cts.IsCancellationRequested)
                _cts.Cancel();

            Task.WaitAll(_tasks.Values.ToArray());
            cancel_task.Wait();
            _cts.Dispose();
        }

    }
}
