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

using System.Xml.Serialization;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Collections;
using System.Data;
using System.Globalization;

namespace ControllerRuntime
{
    /// <summary>
    /// Workflow Step serializer
    /// </summary>
    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "Step")]
    public class WorkflowStep
    {

        public WorkflowStep()
        {

        }

        public WorkflowStep(string KeyCode)
        {
            key_code = KeyCode;
        }

        private string key_code = "WS";
        public string Key
        { get { return String.Format("{0}_{1}_{2}_{3}",key_code, WorkflowId, StepId, RunId); } }

        public int WorkflowId
        { get; set; }

        public int RunId
        { get; set; }


        private int step_id = 0;
        [XmlAttribute("StepID")]
        public int StepId
        {
            get { return this.step_id; }
            set { this.step_id = value; }
        }

        private bool is_set_to_run = false;
        public bool IsSetToRun
        {
            get { return this.is_set_to_run; }
            set { this.is_set_to_run = value; }
        }

        private bool step_ignore_err;
        [XmlAttribute("IgnoreErr")]
        public bool IgnoreError
        {
            get { return this.step_ignore_err; }
            set { this.step_ignore_err = value; }
        }

        private bool step_restart_on_err;
        [XmlAttribute("Restart")]
        public bool RestartOnError
        {
            get { return this.step_restart_on_err; }
            set { this.step_restart_on_err = value; }
        }

        private string sequence_group = String.Empty;
        [XmlAttribute("SeqGroup")]
        public string SequenceGroup
        {
            get { return this.sequence_group; }
            set { this.sequence_group = value; }
        }

        private string priority_group = "ZZZ";
        [XmlAttribute("PriGroup")]
        public string PriorityGroup
        {
            get { return this.priority_group; }
            set { this.priority_group = value; }
        }

        private string loop_group = String.Empty;
        [XmlAttribute("LoopGroup")]
        public string LoopGroup
        {
            get { return this.loop_group; }
            set { this.loop_group = value; }
        }


        private WorkflowProcess step_process;
        [XmlElement("Process")]
        public WorkflowProcess StepProcess
        {
            get { return this.step_process; }
            set { this.step_process = value; }
        }

        private WorkflowProcess step_on_success_process;
        [XmlElement("OnSuccess")]
        public WorkflowProcess StepOnSuccessProcess
        {
            get { return this.step_on_success_process; }
            set { this.step_on_success_process = value; }
        }

        private WorkflowProcess step_on_failure_process;
        [XmlElement("OnFailure")]
        public WorkflowProcess StepOnFailureProcess
        {
            get { return this.step_on_failure_process; }
            set { this.step_on_failure_process = value; }
        }

        [XmlArray("Constraints")]
        [XmlArrayItem("Constraint", typeof(WorkflowConstraint))]
        public WorkflowConstraint[] StepConstraints
        {
            get { return this.step_constraints; }
            set { this.step_constraints = value; }
        }
        private WorkflowConstraint[] step_constraints;

        [XmlArray("Attributes")]
        [XmlArrayItem("Attribute", typeof(MetadataAttribute))]
        public MetadataAttribute[] StepAttributes
        {
            get { return this.step_attributes; }
            set { this.step_attributes = value; }
        }
        private MetadataAttribute[] step_attributes;

        private string step_name;
        [XmlAttribute("StepName")]
        public string StepName
        {
            get { return this.step_name; }
            set { this.step_name = value; }
        }

        private string step_desc;
        [XmlAttribute("StepDesc")]
        public string StepDesc
        {
            get { return this.step_desc; }
            set { this.step_desc = value; }
        }

        private string step_order;
        [XmlAttribute("StepOrder")]
        public string StepOrder
        {
            get { return this.step_order; }
            set { this.step_order = value; }
        }


        private bool is_disabled;
        [XmlAttribute("Disabled")]
        public bool IsDisabled
        {
            get { return this.is_disabled; }
            set { this.is_disabled = value; }
        }


        private int step_retry;
        [XmlAttribute("Retry")]
        public int StepRetry
        {
            get { return this.step_retry; }
            set { this.step_retry = value; }
        }

        private int step_delay_on_retry = 10; //sec
        [XmlAttribute("Delay")]
        public int StepDelayOnRetry
        {
            get { return this.step_delay_on_retry; }
            set { this.step_delay_on_retry = (value <= 0) ? 10 : value; ; }
        }
        private int wf_timeout = 72000; //sec
        [XmlAttribute("Timeout")]
        public int StepTimeout
        {
            get { return this.wf_timeout; }
            set { this.wf_timeout = (value <= 0) ? 72000 : value; }
        }

    }
}
