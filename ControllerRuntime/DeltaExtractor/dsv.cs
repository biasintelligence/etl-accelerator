using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Serialization;
using System.Data;
using System.IO;
using System.Globalization;
using Serilog;

namespace BIAS.Framework.DeltaExtractor
{

    public class Dsv
    {
        private string sa;
        private string doc;
        public bool Valid { get; set;}
        private DataTable dsvtable;
        private Dictionary<string, MyColumn> m_columns = new Dictionary<string, MyColumn>();
        private readonly ILogger _logger;

        public Dsv(string sa, string dsvfilename, ILogger logger)
        {
            this.sa = sa;
            this.doc = String.IsNullOrEmpty(dsvfilename) ? String.Empty : Path.Combine(sa, dsvfilename);
            _logger = logger;
        }

        public Dsv(string sa, ILogger logger) :this (sa, String.Empty, logger)
        {
        }

        public Dictionary<string,MyColumn> ColumnCollection
        {
            get { return m_columns; }
        }


        public string RecordDelimiter
        {
            get { return this.Valid ? this.dsvtable.ExtendedProperties["RecordDelimiter"] as string : String.Empty; }
        }
        public string DataFormat
        {
            get { return this.Valid ? this.dsvtable.ExtendedProperties["DataFormat"] as string : String.Empty; }
        }
        public string DataCompression
        {
            get { return this.Valid ? this.dsvtable.ExtendedProperties["DataCompression"] as string : String.Empty; }
        }
        public string DataQuery
        {
            get { return this.Valid ? this.dsvtable.ExtendedProperties["DataQuery"] as string : String.Empty; }
        }
        public string TableName
        {
            get { return this.Valid ? this.dsvtable.ExtendedProperties["DbTableName"] as string : String.Empty; }
        }

            
        public bool FindTable(string tname)
        {
            string[] fn = Directory.GetFiles(this.sa,"*.dsv",SearchOption.TopDirectoryOnly);
            foreach (string f in fn)
            {
                XPathDocument xd = new XPathDocument(Path.Combine(this.sa,f));
                XPathNavigator xn = xd.CreateNavigator();
                XmlNamespaceManager ns = new XmlNamespaceManager(xn.NameTable);
                ns.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
                ns.AddNamespace("msprop", "urn:schemas-microsoft-com:xml-msprop");

                //XPathExpression xe = XPathExpression.Compile("//xs:element[@name=\"" + m_tablename + "\"]", ns);
                XPathExpression xe = XPathExpression.Compile("//xs:schema", ns);
                xn = xn.SelectSingleNode(xe);
                if (xn != null)
                {
                    XmlReader xr = xn.ReadSubtree();
                    DataSet ds = new DataSet();
                    ds.ReadXmlSchema(xr);
                    this.dsvtable = ds.Tables[tname];

                    if (this.dsvtable != null)
                    {
                        this.Valid = CreateColumnCollection();
                        if (!this.Valid) { m_columns.Clear(); }
                        break;
                    }
                }
            }
            return this.Valid;
        }

        private bool CreateColumnCollection()
        {
            foreach(DataColumn column in this.dsvtable.Columns)
            {
                MyColumn myCol = new MyColumn();
                myCol.Name = column.ColumnName;
                string exDataType = (column.ExtendedProperties["ExtendedDataType"] == null)? String.Empty : column.ExtendedProperties["ExtendedDataType"].ToString();
                switch (exDataType)
                {
                    case ("String"):
                    case ("NChar"):
                    case ("NVarChar"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_WSTR;
                        myCol.Length = (column.ExtendedProperties["DataSize"] == null) ? column.MaxLength : Convert.ToInt32(column.ExtendedProperties["DataSize"], CultureInfo.InvariantCulture);
                        myCol.CodePage = 1252;
                        break;
                    case ("Char"):
                    case ("VarChar"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_STR;
                        myCol.Length = (column.ExtendedProperties["DataSize"] == null) ? column.MaxLength : Convert.ToInt32(column.ExtendedProperties["DataSize"], CultureInfo.InvariantCulture);
                        myCol.CodePage = 1252;
                        break;
                    case ("SByte"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_I1;
                        break;
                    case ("TinyInt"):
                    case ("Byte"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_UI1;
                        break;
                    case ("SmallInt"):
                    case ("Int16"):
                    case ("Short"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_I2;
                        break;
                    case ("Int"):
                    case ("Integer"):
                    case ("Int32"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_I4;
                        break;
                    case ("BigInt"):
                    case ("Int64"):
                    case ("Long"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_I8;
                        break;
                    case ("UInt16"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_UI2;
                        break;
                    case ("UInt32"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_UI4;
                        break;
                    case ("UInt64"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_UI8;
                        break;
                    case ("VarBinary"):
                    case ("Binary"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_BYTES;
                        myCol.Length = (column.ExtendedProperties["DataSize"] == null) ? column.MaxLength : Convert.ToInt32(column.ExtendedProperties["DataSize"], CultureInfo.InvariantCulture);
                        break;
                    case ("Bit"):
                    case ("Boolean"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_BOOL;
                        break;
                    case ("DateTime"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_DBTIMESTAMP;
                        break;
                    case ("DateTime2"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_DBTIMESTAMP2;
                        break;
                    case ("DateTimeOffset"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_DBTIMESTAMPOFFSET;
                        break;
                    case ("DBTime"):
                    case ("Time"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_DBTIME;
                        break;
                    case ("DBTime2"):
                    case ("Time2"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_DBTIME2;
                        break;
                    case ("DBDate"):
                    case ("Date"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_DBDATE;
                        break;
                    case ("Decimal"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_DECIMAL;
                        break;
                    case ("Numeric"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_NUMERIC;
                        myCol.Precision = (column.ExtendedProperties["Precision"] == null) ? column.MaxLength : Convert.ToInt32(column.ExtendedProperties["Precision"], CultureInfo.InvariantCulture);
                        myCol.Scale = (column.ExtendedProperties["Scale"] == null) ? 0 : Convert.ToInt32(column.ExtendedProperties["Scale"], CultureInfo.InvariantCulture);
                        break;
                    case ("Money"):
                    case ("Currency"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_CY;
                        break;
                    case ("Float"):
                    case ("Double"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_R8;
                        break;
                    case ("Real"):
                    case ("Single"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_R4;
                        break;
                    case ("Guid"):
                        myCol.DataType = Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_GUID;
                        break;

                    default :
                        _logger.Error("Dsv Unsupported datatype {datatype}", exDataType);
                        return false;
                }
                m_columns.Add(myCol.Name, myCol);

            }
            return true;

        }
    
    }
}
