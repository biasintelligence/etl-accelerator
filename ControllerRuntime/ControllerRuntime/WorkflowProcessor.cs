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
    public class WorkflowProcessor
    {
        private bool _debug = false;
        private bool _verbose = false;
        private bool _forcestart = false;

        private DBController _db;
        private WorkflowGraph _wfg;
        private Workflow _wf;

        private ILogger _logger;

        public WorkflowProcessor (WorkflowAttributeCollection attributes)
        {
            _logger = Log.Logger;
            _attributes = attributes;
            Initialize();
            
        }
        public string ProcessorName
        { get; private set; } = "Default";

        public DBController DBController
        { get { return _db; } }

        public Workflow Workflow
        { get { return _wf; } }

        public string ConnectionString
        { get; private set; } = string.Empty;

        public string WorkflowName
        { get; private set; } = string.Empty;


        private readonly WorkflowAttributeCollection _attributes = new WorkflowAttributeCollection();
        public WorkflowAttributeCollection Attributes
        {
            get {return _attributes; }
        }

        public WfResult Run(CancellationToken extToken)
        {
            //<stepKey,Task>
            var tasks = new Dictionary<string, Task>();
            var step_command = new Dictionary<string, WorkflowStepProcessor>();

            bool is_initialized = false;

            WfResult result = WfResult.Unknown;
            WorkflowStep step = null;
            try
            {
                _db = DBController.Create(ConnectionString, _debug, _verbose);
                _wf = _db.WorkflowMetadataGet(WorkflowName);
                _logger = _logger.ForContext("WorkflowId", _wf.WorkflowId);

                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                //workflow level retries
                for (int retryCount = 0;retryCount <= _wf.Retry; retryCount++)
                {

                    if (retryCount > 0)
                        _logger.Information("WF retry attempt {Count} on: {Message}", retryCount, result.Message);

                    //workflow timeout
                    TimeSpan lifetime = (_wf.Timeout == 0) ? TimeSpan.MaxValue : TimeSpan.FromSeconds(_wf.Timeout);

                    using (CancellationTokenSource finishCts = new CancellationTokenSource())
                    using (CancellationTokenSource cancelCts = new CancellationTokenSource())
                    using (CancellationTokenSource timeoutCts = new CancellationTokenSource(lifetime))
                    using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(extToken, timeoutCts.Token,cancelCts.Token,finishCts.Token))
                    {
                        try
                        {

                            if (retryCount > 0 && _wf.DelayOnRetry > 0)
                            {
                                try
                                {
                                    Task.Delay(TimeSpan.FromSeconds(_wf.DelayOnRetry), linkedCts.Token).Wait();
                                }
                                catch { }
                            }



                            is_initialized = _db.WorkflowInitialize(ProcessorName, _wf, _forcestart);
                            _logger = _logger.ForContext("RunId", _wf.RunId);

                            //now we can start logging to the Controller
                            _logger.Information("Workflow Runner v.{Version} ({Bit} bit)", v.ToString(), 8 * IntPtr.Size);
                            _logger.Information("Start Processing Workflow {ItemName}", _wf.WorkflowName);

                            _wfg = WorkflowGraph.Create(_wf, _db);
                            _wfg.Start();

                            //timeout settings
                            DateTime start_dt = DateTime.Now;
                            TimeSpan timeout = lifetime;

                            //start Workflow cancellation listener
                            Task cancel_task =
                            Task.Factory.StartNew(() =>
                            {

                                while (!linkedCts.IsCancellationRequested)
                                {
                                    try
                                    {
                                        Task.Delay(TimeSpan.FromSeconds(_wf.Ping), linkedCts.Token).Wait();
                                    }
                                    catch { }

                                    if (linkedCts.IsCancellationRequested) break;

                                    WfResult cancel_result = _db.WorkflowExitEventCheck(_wf.WorkflowId, 0, _wf.RunId);
                                    if (cancel_result.StatusCode != WfStatus.Running)
                                    {
                                        cancelCts.Cancel();
                                        break;
                                    }
                                }

                            }, linkedCts.Token);

                            while (_wfg.TryTake(out step, timeout))
                            {
                                //tell dispatcher we are starting the step execution
                                ReportStepResult(step, WfResult.Started);
                                //loop steps
                                if (tasks.ContainsKey(step.Key))
                                {
                                    if (!tasks[step.Key].IsCompleted)
                                        throw new InvalidOperationException(String.Format("Task {0} is not completed", step.Key));

                                    tasks[step.Key].Dispose();
                                    tasks.Remove(step.Key);
                                }

                                if (!step_command.ContainsKey(step.Key))
                                {
                                    var stepProcessor = new WorkflowStepProcessor(step, this);
                                    step_command.Add(step.Key, stepProcessor);
                                }

                                tasks.Add(step.Key,
                                Task.Factory.StartNew((object obj) =>
                                {
                                    WorkflowStep iter_step = obj as WorkflowStep;
                                    WorkflowStepProcessor sp = step_command[iter_step.Key];

                                    //check for the step cancelation condition from the runtime before starting
                                    WfResult cancel_result = _db.WorkflowExitEventCheck(iter_step.WorkflowId, iter_step.StepId, iter_step.RunId);
                                    if (cancel_result.StatusCode == WfStatus.Running)
                                    {
                                        WfResult step_result = sp.Run(linkedCts.Token);
                                        ReportStepResult(iter_step, step_result);
                                    }
                                    else
                                    {
                                        ReportStepResult(iter_step, cancel_result);
                                    }

                                }, step, linkedCts.Token));

                                timeout = lifetime - DateTime.Now.Subtract(start_dt);
                            }

                            //after workflow Dispatcher is done, we wait for the still running steps completion.
                            //or exit on timeout
                            Task.WaitAll(tasks.Values.ToArray());
                            result = _wfg.WorkflowCompleteStatus;
                            if (result.StatusCode == WfStatus.Failed
                                && timeoutCts.IsCancellationRequested)
                            {
                                _logger.Error("Workflow timeout was reached {ErrorCode}", result.ErrorCode);
                            }

                        }
                        catch (AggregateException aex)
                        {

                            StringBuilder sb = new StringBuilder();
                            _logger.Error(aex, "AggregateException: ", aex.Message);
                            foreach (var ex in aex.InnerExceptions)
                            {
                                _logger.Error(ex, "InnerException: {Message}", ex.Message);
                                sb.Append(ex.Message);
                            }
                            result = WfResult.Failed;
                            if (timeoutCts.IsCancellationRequested)
                            {

                                result = WfResult.Create(WfStatus.Failed, "Workflow timeout was reached", -10);
                                _logger.Error(aex, result.Message);

                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Exception: {Message}", ex.Message);
                            result = WfResult.Create(WfStatus.Failed, ex.Message, -10);
                        }
                        finally
                        {
                            if (_db != null && is_initialized)
                            {
                                WfResult wfResult = _wfg.WorkflowCompleteStatus;
                                _db.WorkflowFinalize(_wf, _wfg.WorkflowCompleteStatus);
                                _logger.Information("Finish Processing Workflow {ItemName} with result - {WfStatus} {Message} ({ErrorCode})"
                                    , _wf.WorkflowName,wfResult.StatusCode.ToString(),wfResult.Message, wfResult.ErrorCode);
                            }

                        }

                        finishCts.Cancel();
                        cancelCts.Token.ThrowIfCancellationRequested();
                        extToken.ThrowIfCancellationRequested();

                        if (result.StatusCode == WfStatus.Succeeded)
                            break;

                    }
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception: {Message}", ex.Message);
                result = WfResult.Create(WfStatus.Failed, ex.Message, -11);
            }

            Task.WaitAll(tasks.Values.ToArray());
            return result;
        }
        private void Initialize()
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

    }
}
