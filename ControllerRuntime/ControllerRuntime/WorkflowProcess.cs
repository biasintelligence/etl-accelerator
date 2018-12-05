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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            set
            {
                this.param = value;
                if (!string.IsNullOrEmpty(value) && value.Trim().StartsWith("[{"))
                {
                    try
                    {
                        JArray p = JArray.Parse(value);
                        if (p.HasValues)
                            Parameters = JsonConvert.DeserializeObject<List<WorkflowParameter>>(value);

                    }
                    catch
                    {
                        //ignore
                    }
                }
                else
                {
                    Parameters = LegacyDeserialize(value);
                }

                if (Parameters == null)
                    Parameters = new List<WorkflowParameter>();

            }
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

        /// <summary>
        /// parsed process param
        /// legacy format attr=>attr1,attr2;...
        /// json array format [{Name:attr,Override:[attr1,attr2],Default:Value}...]
        /// </summary>
        /// <returns>list</returns>
        public List<WorkflowParameter> Parameters
        { get; private set; } = new List<WorkflowParameter>();
        #endregion

        private List<WorkflowParameter> LegacyDeserialize(string param)
        {
            List<WorkflowParameter> list = new List<WorkflowParameter>();
            Dictionary<string, string[]> dic = ParseParameterString(param);
            foreach(var kvp in dic)
            {
                WorkflowParameter p = new WorkflowParameter();
                p.Name = kvp.Key;
                p.Override = new List<string>(kvp.Value);
                list.Add(p);
            }

            return list;
        }

        /// <summary>
        /// parse param astring to the param map
        /// </summary>
        /// <param name="param">attr=>attr1,attr2;</param>
        /// <returns>attribute map</returns>
        private Dictionary<string, string[]> ParseParameterString(string param)
        {
            Dictionary<string, string[]> dic = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);

            if (String.IsNullOrEmpty(param))
                return dic;

            string[] map = param.Split(';');
            foreach (string pair in map)
            {
                string[] kvp = pair.Split(new string[] { "=>" }, StringSplitOptions.None);
                if (kvp.Length == 2
                    && !String.IsNullOrEmpty(kvp[0])
                    && !String.IsNullOrEmpty(kvp[1])
                    )
                {
                    dic.Add(kvp[0], kvp[1].Split(','));
                }
            }
            return dic;
        }

    }
}
