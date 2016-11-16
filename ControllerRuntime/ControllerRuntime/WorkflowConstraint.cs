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
    /// Workflow Constraint serializer
    /// </summary>
    [XmlRoot(Namespace = "ETLController.XSD",ElementName = "Constraint")]
    public class WorkflowConstraint
    {

        public string Key
        { get { return String.Format("WC_{0}_{1}_{2}_{3}", WorkflowId, StepId, ConstId, RunId); } }
        public int RunId
        { get; set; }
        public int WorkflowId
        { get; set; }
        public int StepId
        { get; set; }



        private int const_id = 0;
        [XmlAttribute("ConstID")]
        public int ConstId
        {
            get { return this.const_id; }
            set { this.const_id = value; }
        }

        private WorkflowProcess const_process;
        [XmlElement("Process")]
        public WorkflowProcess Process
        {
            get { return this.const_process; }
            set { this.const_process = value; }
        }

        [XmlArray("Attributes")]
        [XmlArrayItem("Attribute", typeof(WorkflowAttribute))]
        public WorkflowAttribute[] Attributes
        {
            get { return this.const_attributes; }
            set { this.const_attributes = value; }
        }
        private WorkflowAttribute[] const_attributes;

        private string const_order;
        [XmlAttribute("ConstOrder")]
        public string ConstOrder
        {
            get { return this.const_order; }
            set { this.const_order = value; }
        }

        private int wait_period = 10; //sec
        [XmlAttribute("WaitPeriod")]
        public int WaitPeriod
        {
            get { return this.wait_period; }
            set { this.wait_period = (value < 0) ? 10 : value; }
        }

        private bool is_disabled;
        [XmlAttribute("Disabled")]
        public bool IsDisabled
        {
            get { return this.is_disabled; }
            set { this.is_disabled = value; }
        }

        private int ping_interval = 1; //sec
        [XmlAttribute("Ping")]
        public int Ping
        {
            get { return this.ping_interval; }
            set { this.ping_interval = (value <= 0) ? 1 : value; }
        }


    }
}
