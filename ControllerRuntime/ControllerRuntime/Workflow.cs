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
    /// Workflow metadata serializer
    /// </summary>
    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "Context")]
    public class Workflow
    {

        public string Key
        { get { return String.Format("WF_{0}_{1}", WorkflowId, RunId); } }

        public int RunId
        { get; set; }

        [XmlArray("Attributes")]
        [XmlArrayItem("Attribute", typeof(MetadataAttribute))]
        public MetadataAttribute[] WorkflowAttributes
        {
            get { return this.wf_attributes; }
            set { this.wf_attributes = value; }
        }
        private MetadataAttribute[] wf_attributes;

        private WorkflowProcess wf_on_success_process;
        [XmlElement("OnSuccess")]
        public WorkflowProcess OnSuccessProcess
        {
            get { return this.wf_on_success_process; }
            set { this.wf_on_success_process = value; }
        }

        private WorkflowProcess wf_on_failure_process;
        [XmlElement("OnFailure")]
        public WorkflowProcess OnFailureProcess
        {
            get { return this.wf_on_failure_process; }
            set { this.wf_on_failure_process = value; }
        }

        [XmlArray("Constraints")]
        [XmlArrayItem("Constraint", typeof(WorkflowConstraint))]
        public WorkflowConstraint[] WorkflowConstraints
        {
            get { return this.wf_constraints; }
            set { this.wf_constraints = value; }
        }
        private WorkflowConstraint[] wf_constraints;


        [XmlArray("Steps")]
        [XmlArrayItem("Step", typeof(WorkflowStep))]
        public WorkflowStep[] WorkflowSteps
        {
            get { return this.wf_steps; }
            set { this.wf_steps = value; }
        }
        private WorkflowStep[] wf_steps;

        private int wf_id;
        [XmlAttribute("BatchID")]
        public int WorkflowId
        {
            get { return this.wf_id; }
            set { this.wf_id = value; }
        }

        private string wf_name;
        [XmlAttribute("BatchName")]
        public string WorkflowName
        {
            get { return this.wf_name; }
            set { this.wf_name = value; }
        }

        private string wf_desc;
        [XmlAttribute("BatchDesc")]
        public string WorkflowDesc
        {
            get { return this.wf_desc; }
            set { this.wf_desc = value; }
        }

        private bool wf_ignore_err;
        [XmlAttribute("IgnoreErr")]
        public bool IgnoreError
        {
            get { return this.wf_ignore_err; }
            set { this.wf_ignore_err = value; }
        }

        private bool wf_restart_on_err;
        [XmlAttribute("Restart")]
        public bool RestartOnError
        {
            get { return this.wf_restart_on_err; }
            set { this.wf_restart_on_err = value; }
        }

        private bool is_disabled;
        [XmlAttribute("Disabled")]
        public bool IsDisabled
        {
            get { return this.is_disabled; }
            set { this.is_disabled = value; }
        }

        private int wf_max_thread = 4;
        [XmlAttribute("MaxThread")]
        public int MaxThreads
        {
            get { return this.wf_max_thread; }
            set { this.wf_max_thread = (value <= 0) ? 4 : value; }
        }

        private int wf_timeout = 72000; //sec
        [XmlAttribute("Timeout")]
        public int Timeout
        {
            get { return this.wf_timeout; }
            set { this.wf_timeout = (value <= 0)? 72000 : value; }
        }

        private int wf_lifetime = 72000; //sec
        [XmlAttribute("Lifetime")]
        public int Lifetime
        {
            get { return this.wf_lifetime; }
            set { this.wf_lifetime = (value <= 0)? 72000 : value; }
        }

        private int wf_ping = 10; //sec
        [XmlAttribute("Ping")]
        public int Ping
        {
            get { return this.wf_ping; }
            set { this.wf_ping = (value <= 0) ? 10 : value; }
        }

        private int wf_history_retention = 30;
        [XmlAttribute("HistRet")]
        public int HistoryRetention
        {
            get { return this.wf_history_retention; }
            set { this.wf_history_retention = (value <= 0) ? 30 : value; }
        }

        private int wf_retry = 0;
        [XmlAttribute("Retry")]
        public int Retry
        {
            get { return this.wf_retry; }
            set { this.wf_retry = (value < 0) ? 0 : value; ; }
        }

        private int wf_delay_on_retry = 10; //sec
        [XmlAttribute("Delay")]
        public int DelayOnRetry
        {
            get { return this.wf_delay_on_retry; }
            set { this.wf_delay_on_retry = (value <= 0) ? 10 : value; ; }
        }

        public static Workflow DeSerializefromXml(string XMLString)
        {
            Workflow wf = new Workflow();

            //XMLValidator validator = new XMLValidator();
            //if (validator.ValidatingProcess(Resources.ETLControllerXSD, XMLString))
            //{

                try
                {
                    XmlSerializer wfser = new XmlSerializer(typeof(Workflow));
                    wf = (Workflow)wfser.Deserialize(new StringReader(XMLString));
                }
                catch (Exception ex)
                {
                    throw (ex);
                }
            //}
            //else
            //{
            //    throw new Exception("Workflow Xml was not in the correct format");
            //}
            return wf;

        }


    }
}
