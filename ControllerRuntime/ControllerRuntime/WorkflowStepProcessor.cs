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
// 2017-01-25       andrey              add delay logic to retries
// 2018-02-03       andrey              allow to skip Step process on processId = 0
//                                      to enable attribute re-evaluation only (set batch attributes from counters for example) 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using Serilog.Events;
using ControllerRuntime.Logging;

namespace ControllerRuntime
{
    /// <summary>
    /// Step Processor.
    /// Run all Step Constraints if any
    /// Run Step process
    /// Run OnSuccess\OnFailure process if defined
    /// </summary>
    public class WorkflowStepProcessor
    {
        private WorkflowProcessor _wfp;
        private DBController _db;
        private WorkflowStep _step;
        private ILogger _logger;

        public WorkflowStepProcessor(WorkflowStep item, WorkflowProcessor wfp)
        {
            _wfp = wfp;
            _db = wfp.DBController;
            _step = item;

            _logger = wfp.Logger.ForContext("StepId", item.StepId);

        }
        public WfResult Run(CancellationToken extToken)
        {

            _logger.Information("Start Processing Workflow step {ItemKey}:{ItemName}", _step.Key, _step.StepName);
            WfResult result = WfResult.Succeeded;
            try
            {
                //Step Constraints
                if (_step.StepConstraints != null)
                {
                    foreach (WorkflowConstraint wfc in _step.StepConstraints)
                    {
                        if (wfc.IsDisabled || wfc.Process.ProcessId == 0)
                        {
                            _logger.Debug("Constraint process is not defined or disabled {ItemKey}:{ItemName}", _step.Key, _step.StepName);
                            continue;
                        }

                        if ((wfc.Process.ScopeId & 12) == 0)
                            throw new ArgumentException(String.Format("Constraint Process is not of correct scope 0011 = {0}", wfc.Process.ScopeId));


                        wfc.WorkflowId = _step.WorkflowId;
                        wfc.StepId = _step.StepId;
                        wfc.RunId = _step.RunId;
                        WorkflowConstraintProcessor wcp = new WorkflowConstraintProcessor(wfc, _wfp);
                        result = wcp.Run(extToken);
                        if (result.StatusCode == WfStatus.Failed)
                        {
                            _logger.Error("Workflow step {ItemKey}:{ItemName} failed with: {ErrorCode}", _step.Key, _step.StepName, result.Message, result.ErrorCode);
                            return result;
                        }

                    }
                }

                WorkflowAttributeCollection attributes = null;
                if (_step.StepProcess != null)
                {
                    //always evaluate step attributes
                    attributes = _db.WorkflowAttributeCollectionGet(_step.WorkflowId, _step.StepId, 0, _step.RunId);
                    attributes.Merge(_wfp.Attributes);

                    if (_step.StepProcess.ProcessId != 0)
                    {

                        if ((_step.StepProcess.ScopeId & 3) == 0)
                            throw new ArgumentException(String.Format("Step Process is not of correct scope 1100 = {0}", _step.StepProcess.ScopeId));

                        //Run Step Activity here
                        WorkflowActivity step_activity = new WorkflowActivity(_step.StepProcess, attributes, _logger);
                        IWorkflowActivity step_runner = step_activity.Activate();
                        result = (ProcessRunAsync(step_runner, extToken, _step.StepRetry, _step.StepDelayOnRetry, _step.StepTimeout, _logger)).Result;
                    }
                    else
                    {
                        _logger.Debug("Step process is not defined. Skipped {ItemKey}", _step.Key);
                    }
                }



                if (result.StatusCode == WfStatus.Succeeded
                    && _step.StepOnSuccessProcess != null)
                {

                    _logger.Information("On step success");
                    _logger.Debug("On success process - {ProcessName}", _step.StepOnSuccessProcess.Process);

                    if ((_step.StepOnSuccessProcess.ScopeId & 3) == 0)
                        throw new ArgumentException(String.Format("OnSuccess Process is not of correct scope 1100 = {0}", _step.StepOnSuccessProcess.ScopeId));

                    //Run OnSuccess Activity here
                    //re-evaluate attribites
                    //if (attributes == null)
                    attributes = _db.WorkflowAttributeCollectionGet(_step.WorkflowId, _step.StepId, 0, _step.RunId);
                    WorkflowActivity success_activity = new WorkflowActivity(_step.StepOnSuccessProcess, attributes, _logger);
                    IWorkflowActivity success_runner = success_activity.Activate();

                    WfResult task_result = (ProcessRunAsync(success_runner, extToken, 0, 0, _step.StepTimeout, _logger)).Result;
                    if (task_result.StatusCode == WfStatus.Failed)
                        result = task_result;
                }
                else if (result.StatusCode == WfStatus.Failed
                   && _step.StepOnFailureProcess != null)
                {

                    _logger.Information("On step failure");
                    _logger.Debug("On failure process - {ProcessName}", _step.StepOnFailureProcess.Process);

                    if ((_step.StepOnFailureProcess.ScopeId & 3) == 0)
                        throw new ArgumentException(String.Format("OnFailure Process is not of correct scope 1100 = {0}", _step.StepOnFailureProcess.ScopeId));

                    //Run OnFailure Activity here
                    //re-evaluate attribites
                    //if (attributes == null)
                    attributes = _db.WorkflowAttributeCollectionGet(_step.WorkflowId, _step.StepId, 0, _step.RunId);
                    WorkflowActivity failure_activity = new WorkflowActivity(_step.StepOnFailureProcess, attributes, _logger);
                    IWorkflowActivity failure_runner = failure_activity.Activate();

                    WfResult task_result = (ProcessRunAsync(failure_runner, extToken, 0, 0, _step.StepTimeout, _logger)).Result;
                    if (task_result.StatusCode == WfStatus.Failed)
                        result = task_result;
                }
            }
            catch (AggregateException aex)
            {
                _logger.Error(aex, "AggregateException: {Message}", aex.Message);
                foreach (var ex in aex.InnerExceptions)
                {
                    _logger.Error(ex, "InnerException: {Message}", ex.Message);
                }
                result = WfResult.Create(WfStatus.Failed, aex.Message, -10);
            }
            catch (Exception ex)
            {

                _logger.Error(ex, "Exception: {0}", ex.Message);
                result = WfResult.Create(WfStatus.Failed, ex.Message, -10);
            }

            _logger.Information("Workflow step {ItemKey}:{ItemName} finished with result {WfStatus}", _step.Key, _step.StepName, result.StatusCode.ToString());
            return result;
        }

        private async Task<WfResult> ProcessRunAsync(IWorkflowActivity runner, CancellationToken token, int retry, int delay,int timeout, ILogger logger)
        {
            return await Task.Factory.StartNew(() =>
            {
                WfResult result = WfResult.Failed;
                //do thread hard abort if it is stuck on Run
                //using (token.Register(Thread.CurrentThread.Abort))
                //{
                for (int i = 0; i <= retry; i++)
                {
                    using (CancellationTokenSource timeoutCts = new CancellationTokenSource())
                    using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token))
                    {
                        if (timeout > 0)
                            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout));

                        try
                        {

                            token.ThrowIfCancellationRequested();
                            if (i > 0)
                            {
                                logger.Information("Retry attempt {Count} on: {Message}", i, result.Message);

                                if (delay > 0)
                                    Task.Delay(TimeSpan.FromSeconds(delay),token).Wait();
                            }

                            result = runner.Run(linkedCts.Token);
                        }
                        catch (ThreadAbortException ex)
                        {
                            logger.Error(ex, "ThreadAbortException: {0}", ex.Message);
                            result = WfResult.Create(WfStatus.Failed, ex.Message, -10);
                        }
                        catch (AggregateException aex)
                        {

                            logger.Error(aex, "AggregateException: {Message}", aex.Message);
                            foreach (var ex in aex.InnerExceptions)
                            {
                                logger.Error(ex, "InnerException: {Message}", ex.Message);
                            }

                            result = WfResult.Failed;
                            if (timeoutCts.IsCancellationRequested)
                            {
                                result = WfResult.Create(WfStatus.Failed,"Step was cancelled on timeout",-10);
                                logger.Error(aex, result.Message);
                            }

                            if (token.IsCancellationRequested)
                            {
                                logger.Error(aex, "Step was cancelled");
                                result = WfResult.Canceled;
                            }

                        }
                        catch (Exception ex)
                        {

                            logger.Error(ex, "Exception: {Message}", ex.Message);
                            result = WfResult.Create(WfStatus.Failed, ex.Message, -10);
                        }

                        if (result.StatusCode == WfStatus.Succeeded
                        || token.IsCancellationRequested)
                            break;
                    }
                }

                return result;

                //}
            });//, token);

        }

    }
}
