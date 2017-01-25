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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControllerRuntime
{
    /// <summary>
    /// Step Processor.
    /// Run all Step Constraints if any
    /// Run Step process
    /// Run OnSuccess\OnFailure process if defined
    /// </summary>
    public class WorkflowStepProcessor : IWorkflowCommand, IDisposable
    {
        private DBController _db;
        private WorkflowStep _step;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private WfResult _status = WfResult.Unknown;
        private IWorkflowLogger _logger;
        public WorkflowStepProcessor(WorkflowStep item, DBController db)
        {
            _db = db;
            _step = item;
            _logger = _db.GetLogger(_step.WorkflowId, _step.StepId, 0, _step.RunId);
        }
        public WfResult Run(CancellationToken extToken)
        {

            _logger.Write(String.Format("Start Processing Workflow step {0}:{1}", _step.Key, _step.StepName));

            WfResult result = WfResult.Succeeded;
            try
            {
                //Step Constraints
                if (_step.StepConstraints != null)
                {
                    foreach (WorkflowConstraint wfc in _step.StepConstraints)
                    {
                        if (wfc.IsDisabled)
                            continue;

                        if ((wfc.Process.ScopeId & 12) == 0)
                            throw new ArgumentException(String.Format("Constraint Process is not of correct scope 0011 = {0}", wfc.Process.ScopeId));


                        wfc.WorkflowId = _step.WorkflowId;
                        wfc.RunId = _step.RunId;
                        WorkflowConstraintProcessor wcp = new WorkflowConstraintProcessor(wfc, _db);

                        using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(extToken, _cts.Token))
                        {
                            result = wcp.Run(linkedCts.Token);
                        }

                        if (result.StatusCode == WfStatus.Failed)
                        {
                            _logger.WriteError(String.Format("Workflow step {0}:{1} failed with: {2}", _step.Key, _step.StepName, result.Message), result.ErrorCode);
                            _status.SetTo(result);
                            return result;
                        }

                    }
                }

                WorkflowAttribute[] attributes = null;
                if (_step.StepProcess != null)
                {
                    if ((_step.StepProcess.ScopeId & 3) == 0)
                        throw new ArgumentException(String.Format("Step Process is not of correct scope 1100 = {0}", _step.StepProcess.ScopeId));

                    //Run Step Activity here
                    attributes = _db.WorkflowAttributeCollectionGet(_step.WorkflowId, _step.StepId, 0, _step.RunId);
                    WorkflowActivity step_activity = new WorkflowActivity(_step.StepProcess, attributes, _logger);
                    IWorkflowActivity step_runner = step_activity.Activate();
                    using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(extToken, _cts.Token))
                    {
                        result = (ProcessRunAsync(step_runner, linkedCts.Token, _step.StepRetry, _step.StepDelayOnRetry, _logger)).Result;
                    }

                }



                if (result.StatusCode == WfStatus.Succeeded
                    && _step.StepOnSuccessProcess != null)
                {

                    _logger.Write(String.Format("On step success"));
                    _logger.WriteDebug(String.Format("On success process - {0}", _step.StepOnSuccessProcess.Process));

                    if ((_step.StepOnSuccessProcess.ScopeId & 3) == 0)
                        throw new ArgumentException(String.Format("OnSuccess Process is not of correct scope 1100 = {0}", _step.StepOnSuccessProcess.ScopeId));

                    //Run OnSuccess Activity here
                    if (attributes == null)
                        attributes = _db.WorkflowAttributeCollectionGet(_step.WorkflowId, _step.StepId, 0, _step.StepId);
                    WorkflowActivity success_activity = new WorkflowActivity(_step.StepOnSuccessProcess, attributes, _logger);
                    IWorkflowActivity success_runner = success_activity.Activate();

                    using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(extToken, _cts.Token))
                    {
                        WfResult task_result = (ProcessRunAsync(success_runner, linkedCts.Token, 0, 0, _logger)).Result;
                        if (task_result.StatusCode == WfStatus.Failed)
                            result = task_result;
                    }

                }
                else if (result.StatusCode == WfStatus.Failed
                   && _step.StepOnFailureProcess != null)
                {

                    _logger.Write(String.Format("On step failure"));
                    _logger.WriteDebug(String.Format("On failure process - {0}", _step.StepOnFailureProcess.Process));

                    if ((_step.StepOnFailureProcess.ScopeId & 3) == 0)
                        throw new ArgumentException(String.Format("OnFailure Process is not of correct scope 1100 = {0}", _step.StepOnFailureProcess.ScopeId));

                    //Run OnFailure Activity here
                    if (attributes == null)
                        attributes = _db.WorkflowAttributeCollectionGet(_step.WorkflowId, _step.StepId, 0, _step.StepId);
                    WorkflowActivity failure_activity = new WorkflowActivity(_step.StepOnFailureProcess, attributes, _logger);
                    IWorkflowActivity failure_runner = failure_activity.Activate();

                    using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(extToken, _cts.Token))
                    {
                        WfResult task_result = (ProcessRunAsync(failure_runner, linkedCts.Token, 0, 0, _logger)).Result;
                        if (task_result.StatusCode == WfStatus.Failed)
                            result = task_result;
                    }

                }
            }
            catch (AggregateException aex)
            {
                _logger.WriteError(String.Format("AggregateException: {0}", aex.Message), aex.HResult);
                foreach (var ex in aex.InnerExceptions)
                {
                    _logger.WriteError(String.Format("InnerException: {0}", ex.Message), ex.HResult);
                }
                result = WfResult.Create(WfStatus.Failed, aex.Message, -10);
            }
            catch (Exception ex)
            {

                _logger.WriteError(String.Format("Exception: {0}", ex.Message), ex.HResult);
                result = WfResult.Create(WfStatus.Failed, ex.Message, -10);
            }

            _logger.Write(String.Format("Workflow step {0}:{1} finished with result {2}", _step.Key, _step.StepName, result.StatusCode.ToString()));
            _status.SetTo(result);
            return result;
        }

        private async Task<WfResult> ProcessRunAsync(IWorkflowActivity runner, CancellationToken token, int retry, int delay, IWorkflowLogger logger)
        {
            return await Task.Factory.StartNew(() =>
            {
                WfResult result = WfResult.Failed;
                using (token.Register(Thread.CurrentThread.Abort))
                {
                    try
                    {
                        for (int i = 0; i <= retry && result.StatusCode != WfStatus.Succeeded; i++)
                        {

                            if (i > 0 && delay > 0)
                                Task.Delay(TimeSpan.FromSeconds(delay)).Wait();

                            //do thread hard abort if it is stuck on Run
                            result = runner.Run(token);
                            token.ThrowIfCancellationRequested();
                            if (i > 0)
                                logger.Write(String.Format("Retry attempt {0} on: {1}", i, result.Message));
                        }
                    }
                    catch (ThreadAbortException ex)
                    {
                        logger.WriteError(String.Format("ThreadAbortException: {0}", ex.Message), ex.HResult);
                    }
                    catch (AggregateException aex)
                    {
                        logger.WriteError(String.Format("AggregateException: {0}", aex.Message), aex.HResult);
                        foreach (var ex in aex.InnerExceptions)
                        {
                            logger.WriteError(String.Format("InnerException: {0}", ex.Message), ex.HResult);
                        }
                        result = WfResult.Create(WfStatus.Failed, aex.Message, -10);
                    }
                    catch (Exception ex)
                    {

                        logger.WriteError(String.Format("Exception: {0}", ex.Message), ex.HResult);
                        result = WfResult.Create(WfStatus.Failed, ex.Message, -10);
                    }
                    return result;
                }
            }, token);

        }

        #region IWorkflowCommand
        public WfResult Start()
        {
            return WfResult.Create(WfStatus.Failed, "Not Implemented", -1);
        }
        public WfResult Stop()
        {
            //use _cts.Cancel();
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
        { get { return _status; } }
        #endregion
        void IDisposable.Dispose()
        {
            if (!_cts.IsCancellationRequested)
                _cts.Cancel();

            _cts.Dispose();
        }

    }
}
