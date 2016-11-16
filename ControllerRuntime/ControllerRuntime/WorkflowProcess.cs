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
    /// Workflow Process serializer
    /// </summary>
    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "Process")]
    public class WorkflowProcess
    {

        #region Elements
        private string process;
        [XmlElement("Process")]
        public string Process
        {
            get { return this.process; }
            set { this.process = value; }
        }

        
        private string param;
        [XmlElement("Param")]
        public string Param
        {
            get { return this.param; }
            set { this.param = value; }
        }
        #endregion
        #region Attributes

        private int process_id;
        [XmlAttribute("ProcessID")]
        public int ProcessId
        {
            get { return this.process_id; }
            set { this.process_id = value; }
        }

        private int scope_id;
        [XmlAttribute("ScopeID")]
        public int ScopeId
        {
            get { return this.scope_id; }
            set { this.scope_id = value; }
        }
        #endregion


    }
}
