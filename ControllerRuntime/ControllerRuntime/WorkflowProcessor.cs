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

using ControllerRuntime.Logging;
using Serilog;
using Serilog.Events;

namespace ControllerRuntime
{
    
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

        //<stepKey,Task>
        Dictionary<string, Task> _tasks = new Dictionary<string, Task>();
        Dictionary<string, WorkflowStepProcessor> _step_command = new Dictionary<string, WorkflowStepProcessor>();
        Task cancel_task;

        private bool _is_initialized = false;
        private bool _debug = false;
        private bool _verbose = false;
        private bool _forcestart = false;

        private ILogger _logger;

        public WorkflowProcessor ()
        {
            _logger = Log.Logger;
        }
        public string ProcessorName
        { get; private set; } = "Default";

        public DBController DBController
        { get { return _db; } }


        public string ConnectionString
        { get; private set; } = string.Empty;

        public string WorkflowName
        { get; private set; } = string.Empty;

        public IWorkflowCommand GetStepCommand(string Key)
        {
            if (_step_command.ContainsKey(Key))
                return (IWorkflowCommand) _step_command[Key];

            return null;
        }

        private readonly WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        public WorkflowAttributeCollection Attributes
        {
            get {return _attributes; }
        }

        public WfResult Run()
        {

            WfResult result = WfResult.Unknown;
            SetOptions();
            WorkflowStep step = null;
            try
            {
                _db = DBController.Create(ConnectionString, _debug, _verbose);
                _wf = _db.WorkflowMetadataGet(WorkflowName);


                _logger = _logger.ForContext("WorkflowId", _wf.WorkflowId);
 
                _is_initialized = _db.WorkflowInitialize(ProcessorName, _wf, _forcestart);

                _logger = _logger.ForContext("RunId", _wf.RunId);

                //now we can start logging to the Controller

                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                _logger.Information("Workflow Runner v.{Version} ({Bit} bit)", v.ToString(), 8 * IntPtr.Size);
                _logger.Information("Start Processing Workflow {ItemName}", _wf.WorkflowName);

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
                        var stepProcessor = new WorkflowStepProcessor(step, this);
                        _step_command.Add(step.Key, stepProcessor);
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
                _logger.Error(aex,"AggregateException: ",aex.Message);
                foreach (var ex in aex.InnerExceptions)
                {
                    _logger.Error(ex,"InnerException: {Message}", ex.Message);
                }
                result = WfResult.Create(WfStatus.Failed, aex.Message, -10);
            }
            catch (Exception ex)
            {
                _logger.Error(ex,"Exception: {Message}", ex.Message);
                result = WfResult.Create(WfStatus.Failed, ex.Message, -10);
            }
            finally
            {
                if (_db != null && _is_initialized)
                {
                    _db.WorkflowFinalize(_wf, _wfg.WorkflowCompleteStatus);
                    _logger.Information("Finish Processing Workflow {ItemName} with result - {WfStatus}"
                        , _wf.WorkflowName, _wfg.WorkflowCompleteStatus.StatusCode.ToString());
                }
            }

            return (_wfg == null) ? result : _wfg.WorkflowCompleteStatus;
        }
        private void SetOptions()
        {

            string attributeValue;
            if (_attributes.TryGetValue(WorkflowConstants.ATTRIBUTE_PROCESSOR_NAME, out attributeValue))
            {
                ProcessorName = attributeValue;
            }

            if (_attributes.TryGetValue(WorkflowConstants.ATTRIBUTE_DEBUG, out attributeValue))
            {
                _debug = Boolean.Parse(attributeValue);
            }

            if (_attributes.TryGetValue(WorkflowConstants.ATTRIBUTE_VERBOSE, out attributeValue))
            {
                _verbose = Boolean.Parse(attributeValue);
            }

            if (_attributes.TryGetValue(WorkflowConstants.ATTRIBUTE_FORCESTART, out attributeValue))
            {
                _forcestart = Boolean.Parse(attributeValue);
            }

            if (_attributes.TryGetValue(WorkflowConstants.ATTRIBUTE_WORKFLOW_NAME, out attributeValue))
            {
                WorkflowName = attributeValue;
            }

            if (_attributes.TryGetValue(WorkflowConstants.ATTRIBUTE_CONTROLLER_CONNECTIONSTRING, out attributeValue))
            {
                ConnectionString = attributeValue;
            }

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
