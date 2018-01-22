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
    //[XmlRoot(Namespace = "ETLController.XSD", ElementName = "Attributes")]
    public class WorkflowAttributeCollection : Dictionary<string,string>
    {

        public WorkflowAttributeCollection()
            : base (StringComparer.InvariantCultureIgnoreCase)
            {
            }


        public WorkflowAttributeCollection(MetadataAttributeCollection collection)
            : base(StringComparer.InvariantCultureIgnoreCase)
        {
            foreach (var kvp in collection.Attributes)
                Add(kvp.Key, kvp.Value);
        }


        public void Merge(IDictionary<string,string> collection)
        {
            foreach (var attribute in collection)
            {
                if (this.ContainsKey(attribute.Key))
                    continue;

                this.Add(attribute.Key, attribute.Value);
            }
        }

    }


}
