using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using BIAS.Framework.DeltaExtractor.Properties;

namespace BIAS.Framework.DeltaExtractor
{
    public enum HeaderType
    {
        UnknownHeader,
        ETLHeader,
        BIASHeader
    }
    
    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class DBConnection
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string ConnectionString { get; set; }
        public int QueryTimeout { get; set; }
        public string Qualifier { get; set; }
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class Parameters
    {
        private MoveData m_MoveData;
        private RunPackage m_RunPackage;
        private string m_Action;
        private bool m_Debug = false;
        private ETLHeader m_ETLHeader;
        private BIASHeader m_BIASHeader;
        private HeaderType m_HeaderType = HeaderType.UnknownHeader;
        private int m_RunID;

        public bool Debug { get {return m_Debug;} }
        public int RunID { get { return m_RunID; } }
        public HeaderType HeaderType { get { return m_HeaderType; } }


        public ETLHeader ETLHeader
        {
            get { return m_ETLHeader; }
            set
            {
                m_HeaderType = HeaderType.ETLHeader;
                m_ETLHeader = value;
                m_Debug = ((value.Options & 1) == 1);
                m_RunID = value.RunID;
            }
        }

        public BIASHeader BIASHeader
        {
            get { return m_BIASHeader; }
            set
            {
                m_HeaderType = HeaderType.BIASHeader;
                m_BIASHeader = value;
                m_Debug = String.IsNullOrEmpty(value.UserOptions) ? false : value.UserOptions.ToLower().Contains("debug");
                m_RunID = value.RunID;
            }
        }

        public MoveData MoveData
        {
            get { return m_MoveData; }
            set { m_MoveData = value; m_Action = "MoveData"; }
        }


        public RunPackage RunPackage
        {
            get { return m_RunPackage; }
            set { m_RunPackage = value; m_Action = "RunPackage"; }
        }

        
        public string Action
        {
            get { return m_Action; }
        }

        public static Parameters DeSerializefromXml(string XMLString)
        {
            Parameters parameters = new Parameters();

            XMLValidator validator = new XMLValidator();
            if (validator.ValidatingProcess(Resources.ParametersXSD, XMLString))
            {

                XmlSerializer serparam = new XmlSerializer(typeof(Parameters));
                try
                {
                    parameters = (Parameters)serparam.Deserialize(new StringReader(XMLString));
                }
                catch (Exception ex)
                {
                    throw (ex);
                }
            }
            else
            {
                throw new Exception("Parameter Xml was not in the correct format");
            }
            return parameters;

        }
        
    }
}
