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
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using Serilog.Events;
using ControllerRuntime.Logging;

namespace ControllerRuntime
{
    /// <summary>
    /// Process constraint
    /// </summary>
    public class WorkflowConstraintProcessor
    {
        private DBController _db;
        private WorkflowProcessor _wfp;
        private WorkflowConstraint _item;

        private ILogger _logger;
        public WorkflowConstraintProcessor(WorkflowConstraint item, WorkflowProcessor wfp)
        {
            _wfp = wfp;
            _db = wfp.DBController;
            _item = item;

            _logger = Log.Logger
               .ForContext("ConstId", item.ConstId)
               .ForContext("WorkflowId", item.WorkflowId)
               .ForContext("StepId", item.StepId)
               .ForContext("RunId", item.RunId);


        }
        public WfResult Run(CancellationToken extToken)
        {

            _logger.Information("Start Processing Workflow Constraint {ItemKey}", _item.Key);

            WfResult result = WfResult.Unknown;
            var cts = new CancellationTokenSource();
            WorkflowAttributeCollection attributes = _db.WorkflowAttributeCollectionGet(_item.WorkflowId, _item.StepId, _item.ConstId, _item.RunId);
            attributes.Merge(_wfp.Attributes);

            WorkflowActivity activity = new WorkflowActivity(_item.Process, attributes, _logger);

            TimeSpan timeout = TimeSpan.FromSeconds((_item.WaitPeriod <= 0) ? 7200 : _item.WaitPeriod);
            TimeSpan sleep = TimeSpan.FromSeconds((_item.Ping <= 0) ? 30 : _item.Ping);
            try
            {

                using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(extToken, cts.Token))
                {

                    Task<WfResult> task = Task.Factory.StartNew(() =>
                    {
                        WfResult const_result = WfResult.Failed;
                        IWorkflowActivity runner = activity.Activate();
                        while (const_result.StatusCode == WfStatus.Failed)
                        {
                            //Run constraint activity                        
                            using (linkedCts.Token.Register(Thread.CurrentThread.Abort))
                            {
                                try
                                {
                                    //do thread hard abort if it is stuck on Run
                                    //constraint logic should never allow this to happen thou
                                    const_result = runner.Run(linkedCts.Token);
                                    _logger.Debug("Constraint {ItemKey} current status = {WfStatus}", _item.Key, const_result.StatusCode.ToString());

                                    if (const_result.StatusCode == WfStatus.Succeeded)
                                        break;
                                }
                                catch (ThreadAbortException ex)
                                {
                                    throw ex;
                                }
                            }

                            //cts.Token.ThrowIfCancellationRequested();
                            Task.Delay(sleep, linkedCts.Token).Wait();
                            if (linkedCts.IsCancellationRequested)
                                break;

                            //Thread.Sleep(sleep);
                        }
                        return const_result;
                    }, linkedCts.Token);

                    int id = Task.WaitAny(new Task[] { task, Task.Delay(timeout, linkedCts.Token) });
                    if (id == 1)
                        cts.Cancel();

                    result = task.Result;
                }
            }
            catch (AggregateException ex)
            {
                //result = WfResult.Create(WfStatus.Failed,"timeout",-3);
                throw ex;
            }
            finally
            {
                _logger.Information("Finish Processing Workflow Constraint {ItemKey} with result - {WfStatus}", _item.Key, result.StatusCode.ToString());
                cts.Dispose();
            }

            return result;
        }

    }
}
