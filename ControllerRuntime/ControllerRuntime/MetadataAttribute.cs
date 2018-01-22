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
    [XmlRoot(Namespace = "ETLController.XSD", ElementName = "Attribute")]
    public class MetadataAttribute
    {

            [XmlAttribute("Name")]
            public string Key
            { get; set; }


            [XmlTextAttribute()]
            public string Value
            { get; set; }

    }


}
