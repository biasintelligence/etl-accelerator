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
    /// Workflow Attribute set serializer
    /// </summary>
    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "Attributes")]
    public class WorkflowAttributeCollection
    {
        [XmlElement("Attribute")]
        public WorkflowAttribute[] Attributes
        {
            get { return this.attributes; }
            set { this.attributes = value; }
        }
        private WorkflowAttribute[] attributes;

        public static WorkflowAttributeCollection DeSerializefromXml(string XMLString)
        {
            WorkflowAttributeCollection attributes = new WorkflowAttributeCollection();

            //XMLValidator validator = new XMLValidator();
            //if (validator.ValidatingProcess(Resources.ETLControllerXSD, XMLString))
            //{

            try
            {
                XmlSerializer wfser = new XmlSerializer(typeof(WorkflowAttributeCollection));
                attributes = (WorkflowAttributeCollection)wfser.Deserialize(new StringReader(XMLString));
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
            return attributes;

        }

    }

    
    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "Attribute")]
    public class WorkflowAttribute
    {

        public WorkflowAttribute() {}

        public WorkflowAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        [XmlAttribute("Name")]
        public string Name
        {get; set;}


        [XmlTextAttribute()]
        public string Value
        { get; set; }

        public KeyValuePair<string,string> Kvp
        { get { return new KeyValuePair<string, string>(Name, Value); } }
    }
}
