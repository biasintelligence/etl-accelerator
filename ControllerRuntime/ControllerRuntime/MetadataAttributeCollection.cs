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
    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "Attributes")]
    public class MetadataAttributeCollection
    {
        [XmlElement("Attribute")]
        public MetadataAttribute[] Attributes
        {
            get { return this.attributes; }
            set { this.attributes = value; }
        }
        private MetadataAttribute[] attributes;

        public static MetadataAttributeCollection DeSerializefromXml(string XMLString)
        {
            MetadataAttributeCollection attributes = new MetadataAttributeCollection();

            //XMLValidator validator = new XMLValidator();
            //if (validator.ValidatingProcess(Resources.ETLControllerXSD, XMLString))
            //{

            try
            {
                XmlSerializer wfser = new XmlSerializer(typeof(MetadataAttributeCollection));
                attributes = (MetadataAttributeCollection)wfser.Deserialize(new StringReader(XMLString));
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
}
