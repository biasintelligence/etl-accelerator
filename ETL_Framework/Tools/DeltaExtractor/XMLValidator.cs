using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Collections;

namespace BIAS.Framework.DeltaExtractor
{
    public class XMLValidator
    {
        //public bool ValidatingProcess(string XMLString)
        public bool ValidatingProcess(string xsd,string XMLString)
        {
            UnicodeEncoding uniEncoding = new UnicodeEncoding();
            MemoryStream xmlStream = new MemoryStream(uniEncoding.GetBytes(XMLString));
            MemoryStream xsdStream = new MemoryStream(uniEncoding.GetBytes(xsd));

            XmlReaderSettings settings = new XmlReaderSettings();

            settings.Schemas.Add(null, XmlReader.Create(xsdStream));
            //settings.Schemas.Add(null, XmlReader.Create(XSDPath));
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += new ValidationEventHandler(settings_ValidationEventHandler);
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            using (XmlReader reader = XmlReader.Create(xmlStream, settings))
            {
                while (reader.Read())
                {
                    //Empty Loop
                }
            }
            return true;
        }
        void settings_ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e)
        {
            throw new InvalidArgumentException(e.Message);
        }
        
    }
}
