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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControllerRuntime
{
    /// <summary>
    /// Builds sequence of execution steps based on
    /// workflow definition and prior execution results
    /// dispatch steps for execution based on the build sequence and step execution dependency chain
    /// breaks on timeout, fail condition and on completion
    /// culculate workflow run and compete statuses
    /// </summary>

    public class WorkflowGraph : IWorkflowCommand, IDisposable
    {

        private Object lock_object = new Object();

        //private AutoResetEvent autoEvent;
        //private Timer exit_timer;

        Workflow _workflow;
        DBController _db;
        BlockingCollection<WorkflowStep> base_queue = new BlockingCollection<WorkflowStep>();
        List<WorkflowStep> base_node_set = new List<WorkflowStep>();
        Dictionary<string, WfResult> base_status_set = new Dictionary<string, WfResult>();
        Dictionary<string, List<string>> base_loop_set = new Dictionary<string, List<string>>();

        WfResult run_status = WfResult.Unknown;
        WfResult wf_status = WfResult.Unknown;

        protected WorkflowGraph(Workflow wf, DBController db)
        {
            _workflow = wf;
            _db = db;
            Build(wf);
        }
        #region Public Methods
        public static WorkflowGraph Create(Workflow wf, DBController db)
        {
            return new WorkflowGraph(wf, db);
        }

        public Workflow Workflow
        {
            get
            {
                return _workflow;
            }
        }


        public WfResult WorkflowRunStatus
        {
            get
            {
                return run_status;
            }
        }


        public WfResult WorkflowCompleteStatus
        {
            get
            {
                return wf_status;
            }
        }

        public int Count
        { get { return base_node_set.Count; } }

        /// <summary>
        /// step dispatcher function.
        /// Workflow Executor will call this function to receive\wait for the next available step.
        /// </summary>
        /// <param name="step"></param>
        /// <returns>next step scheduled for execution</returns>
        public bool TryTake(out WorkflowStep step, TimeSpan timeout)
        {
            //here the take will timeout on workflow Timeout if set
            //after that no new steps will be dispatched
            //running steps need to timeout on its own accord
            if (!base_queue.TryTake(out step, timeout))
            {
                //no more steps or fail dispatcher on timeout
                if (!base_queue.IsCompleted)
                {
                    //clean up on timeout
                    lock (lock_object)
                    {
                        base_queue.CompleteAdding();
                        run_status.SetTo(WfResult.Create(WfStatus.Failed, "Timeout", -1));
                        wf_status.SetTo(run_status);
                    }
                }
                return false;
            }

            return true;
        }

        public WfResult Start()
        {
            if (wf_status.StatusCode != WfStatus.Unknown)
                return wf_status;

            lock (lock_object)
            {
                run_status.SetTo(WfResult.Started);
                wf_status.SetTo(run_status); ;
                PushWorkToOutputQueue();
            }

            //start ExitEvent timer
            //exit_timer.Change(0, _workflow.Ping * 1000);
            return wf_status;
        }
        public WfResult Stop()
        {
            if (!(wf_status.StatusCode == WfStatus.Running
                || wf_status.StatusCode == WfStatus.Suspended))
                return wf_status;

            lock (lock_object)
            {
                base_queue.CompleteAdding();
                wf_status.SetTo(WfResult.Create(WfStatus.Failed, "Canceled", -2));
            }
            //Pause ExitEvent timer
            //exit_timer.Change(-1, -1);
            return wf_status;
        }
        public WfResult Pause()
        {
            if (wf_status.StatusCode != WfStatus.Running)
                return wf_status;

            lock (lock_object)
            {
                wf_status.SetTo(WfResult.Create(WfStatus.Suspended, "Paused", 0));
            }
            //Pause ExitEvent timer
            //exit_timer.Change(-1, -1);
            return wf_status;
        }

        public WfResult Resume()
        {
            if (wf_status.StatusCode != WfStatus.Suspended)
                return wf_status;

            lock (lock_object)
            {
                wf_status.SetTo(WfResult.Started);
                PushWorkToOutputQueue();
            }
            //Resume ExitEvent timer
            //exit_timer.Change(0, _workflow.Ping * 1000);
            return wf_status;
        }

        public WfResult Status
        { get { return run_status; } }

        /// <summary>
        /// Workflow  Step Processors will call this function on every step execution exit
        /// every dispetched step execution result must be reported back here
        /// for step dispatch operation to progress without hanging.
        /// Use Workflow.Timeout to cancel long running workflows if needed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        public void SetNodeExecutionResult(string key, WfResult result)
        {
            if (!base_status_set.ContainsKey(key))
                throw new ArgumentException(String.Format("Invalid base key {0}", key));

            if (base_status_set[key].StatusCode == result.StatusCode)
                return;

            lock (lock_object)
            {
                base_status_set[key].SetTo(result);
                SetRunStatus(key, result);
                if (wf_status.StatusCode == WfStatus.Running)
                {
                    CheckLoopStatus(key);
                    PushWorkToOutputQueue();
                    SetCompleteStatus();
                }
            }

        }

        #endregion

        private void Build(Workflow wf)
        {
            WorkflowStep wfcs;
            //process WF constraints
            wfcs = new WorkflowStep("WFC");
            wfcs.RunId = wf.RunId;
            wfcs.WorkflowId = _workflow.WorkflowId;
            wfcs.StepId = 0;
            wfcs.StepName = "STARTER";
            wfcs.PriorityGroup = String.Empty;
            wfcs.SequenceGroup = "0";
            wfcs.StepConstraints = wf.WorkflowConstraints;
            wfcs.StepAttributes = wf.WorkflowAttributes;

            base_node_set.Add(wfcs);
            base_status_set.Add(wfcs.Key, WfResult.Unknown);

            //process WF steps
            if (!(wf.WorkflowSteps == null))
            {
                foreach (WorkflowStep wfs in wf.WorkflowSteps.OrderBy(key1 => key1.PriorityGroup)
                    .ThenBy(key2 => key2.SequenceGroup)
                    .ThenBy(key3 => key3.StepOrder))
                {
                    if (wfs.IsDisabled)
                        continue;

                    if (!wfs.IsSetToRun)
                        continue;

                    base_node_set.Add(wfs);
                    base_status_set.Add(wfs.Key, WfResult.Unknown);

                    if (String.IsNullOrEmpty(wfs.LoopGroup))
                        continue;

                    if (!base_loop_set.ContainsKey(wfs.LoopGroup))
                        base_loop_set.Add(wfs.LoopGroup, new List<string>());

                    base_loop_set[wfs.LoopGroup].Add(wfs.Key);
                }
            }

            wfcs = new WorkflowStep("WFF");
            wfcs.RunId = _workflow.RunId;
            wfcs.WorkflowId = _workflow.WorkflowId;
            wfcs.StepId = 0;
            wfcs.StepName = "FINISHER";
            wfcs.PriorityGroup = String.Empty;
            wfcs.SequenceGroup = "0";
            wfcs.StepOnSuccessProcess = wf.OnSuccessProcess;
            wfcs.StepOnFailureProcess = wf.OnFailureProcess;
            wfcs.StepAttributes = wf.WorkflowAttributes;

            base_node_set.Add(wfcs);
            base_status_set.Add(wfcs.Key, WfResult.Unknown);

            //initiate the timer to check for ExitEvent
            //AutoResetEvent autoEvent = new AutoResetEvent(false);
            //TimerCallback tcb = OnExitTimer;
            //exit_timer = new Timer(tcb, autoEvent, -1, -1);

        }


        private void SetRunStatus(string key, WfResult result)
        {

            if (run_status.StatusCode == WfStatus.Running
                && result.StatusCode == WfStatus.Failed
                && !(_workflow.IgnoreError || base_node_set.Find(item => item.Key == key).IgnoreError))
                run_status.SetTo(WfResult.Failed);

            if (base_queue.IsCompleted
                && run_status.StatusCode == WfStatus.Running
                && (0 == base_status_set.Values.Count(kvp => kvp.StatusCode == WfStatus.Running)))
                run_status.SetTo(WfResult.Succeeded);

        }


        private void SetCompleteStatus()
        {
            if (!base_queue.IsCompleted)
                return;

            if (run_status.StatusCode == WfStatus.Failed)
            {
                wf_status.SetTo(run_status);
            }
            else if (run_status.StatusCode == WfStatus.Succeeded)
            {
                if (0 < base_status_set.Values.Count(kvp => kvp.StatusCode == WfStatus.Failed))
                    wf_status.SetTo(WfResult.Failed);
                else
                    wf_status.SetTo(run_status);
            }
            else
                wf_status.SetTo(run_status);
        }


        private void PushWorkToOutputQueue()
        {

            if (base_queue.IsAddingCompleted)
                return;


            if (run_status.StatusCode == WfStatus.Failed)
            {
                base_queue.CompleteAdding();
                return;
            }


            while (1 == 1)
            {
                //max thread constraint
                if (_workflow.MaxThreads < base_status_set.Count(kvp => kvp.Value.StatusCode == WfStatus.Running))
                    break;
                //all steps submitted
                if (0 == base_status_set.Count(kvp => kvp.Value.StatusCode == WfStatus.Unknown))
                {
                    base_queue.CompleteAdding();
                    break;
                }

                WorkflowStep step = base_node_set.FirstOrDefault(item =>
                    //not started
                    base_status_set[item.Key].StatusCode == WfStatus.Unknown
                    //priority group constraint
                    && (0 == base_node_set.Count(kvp =>
                        kvp.PriorityGroup != item.PriorityGroup
                        && base_status_set[kvp.Key].StatusCode == WfStatus.Running))
                    //sequence group constraint
                    && (String.IsNullOrEmpty(item.SequenceGroup) || (0 == base_node_set.Count(kvp =>
                        kvp.SequenceGroup == item.SequenceGroup
                        && base_status_set[kvp.Key].StatusCode == WfStatus.Running)))
                    );

                if (step == null)
                    break;

                base_status_set[step.Key].SetTo(WfResult.Started);
                base_queue.Add(step);
            }

        }

        private void CheckLoopStatus(string key)
        {
            if (run_status.StatusCode == WfStatus.Failed)
                return;

            WorkflowStep step = base_node_set.FirstOrDefault(item => item.Key == key);
            if (String.IsNullOrEmpty(step.LoopGroup))
                return;

            if (!base_loop_set.ContainsKey(step.LoopGroup))
                return;

            //exit the loop on the BreakEvent
            if (WfStatus.Succeeded == _db.WorkflowLoopBreak(_workflow.WorkflowId, _workflow.RunId, step.LoopGroup))
                return;

            WfStatus[] status_set = new WfStatus[2] { WfStatus.Running, WfStatus.Unknown };
            if (0 == base_loop_set[step.LoopGroup].Count(item => status_set.Contains(base_status_set[item].StatusCode)))
            {
                //reset all loop steps
                base_loop_set[step.LoopGroup].ForEach(item => base_status_set[item] = WfResult.Unknown);
                _db.WorkflowLoopStepsReset(_workflow.WorkflowId, _workflow.RunId, step.LoopGroup);
            }
        }

        //private void OnExitTimer(object stateInfo)
        //{
        //    AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
        //    WfResult result = _db.WorkflowExitEventCheck(_workflow.WorkflowId,0,_workflow.RunId);
        //    if (result.StatusCode != WfStatus.Running)
        //    {
        //        lock(lock_object)
        //        {
        //           foreach( WfResult item_result in base_status_set.Values.Where(val => val.StatusCode == WfStatus.Unknown))
        //           {
        //               item_result.SetTo(result);
        //           }
        //        }
        //    }

        //    return;
        //}

        void IDisposable.Dispose()
        {
            //if (exit_timer != null)
            //    exit_timer.Dispose();
        }

    }
}
