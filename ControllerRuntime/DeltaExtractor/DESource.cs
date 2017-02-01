using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Data;
using System.Globalization;

using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{

    public enum SourceType : ushort
    {
        Unknown = 0,
        OleDb = 1,
        FlatFile = 2,
        Excel = 3,
        SPList = 4,
        AdoNet = 5,
        Odbc = 6,
    }

    public interface IDeSource
    {
        bool IsValid { get; set; }
        bool Test(IWorkflowLogger logger);
        SourceType Type { get; }
        string ConnectionString { get; }
        string Description { get; }
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class DataSource : IDeSource
    {
        private object source;

        #region DeSource
        public FlatFileSource FlatFileSource
        {
            get { return (((IDeSource)source).Type == SourceType.FlatFile) ? (FlatFileSource)source : null; }
            set { source = value; }
        }


        public OleDbSource OleDbSource
        {
            get { return (((IDeSource)source).Type == SourceType.OleDb) ? (OleDbSource)source : null; }
            set { source = value; }
        }


        public ExcelSource ExcelSource
        {
            get { return (((IDeSource)source).Type == SourceType.Excel) ? (ExcelSource)source : null; }
            set { source = value; }
        }

        public SharePointSource SharePointSource
        {
            get { return (((IDeSource)source).Type == SourceType.SPList) ? (SharePointSource)source : null; }
            set { source = value; }
        }

        public AdoNetSource AdoNetSource
        {
            get { return (((IDeSource)source).Type == SourceType.AdoNet) ? (AdoNetSource)source : null; }
            set { source = value; }
        }

        public OdbcSource OdbcSource
        {
            get { return (((IDeSource)source).Type == SourceType.Odbc) ? (OdbcSource)source : null; }
            set { source = value; }
        }
        #endregion

        #region IDeSources
        public bool IsValid
        {
            get { return ((IDeSource)source).IsValid; }
            set { ((IDeSource)source).IsValid = value;}
        }
        public bool Test(IWorkflowLogger logger)
        {
            return ((IDeSource)source).Test(logger);
        }
        public SourceType Type
        {
            get { return ((IDeSource)source).Type; }
        }

        public string Description
        {
            get
            {
                return ((IDeSource)source).Description;
            }
        }

        public string ConnectionString
        {
            get
            {
                return ((IDeSource)source).ConnectionString;
            }
        }
        #endregion
    }


    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class OleDBSourceProperties
    {
        private static readonly Dictionary<string,int> AccessModeList = new Dictionary<string,int>
            {
                {"OpenRowset",0},
                {"OpenRowset From Variable",1},
                {"SQL Command",2},
                {"SQL Command From Variable",3},
            };

        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public OleDBSourceProperties()
        {
            this.AccessMode = "SQL Command";
        }

        public string AccessMode
        {
            get { return this.property["AccessMode"].ToString(); }
            set 
            {
                int ival = AccessModeList.ContainsKey(value) ? AccessModeList[value] : 2;
                this.property.Add("AccessMode", ival);
            }
        }
        public string OpenRowset
        {
            get { return (string)this.property["OpenRowset"]; }
        }
        public string OpenRowsetVariable
        {
            get { return (string)this.property["OpenRowsetVariable"]; }
        }
        public string SqlCommandVariable
        {
            get { return (string)this.property["SqlCommandVariable"]; }
        }
        public string SqlCommand
        {
            get { return (string)this.property["SqlCommand"]; }
            set
            {
                switch (AccessMode)
                {
                    case "0":
                        this.property.Add("OpenRowset", value);
                        break;
                    case "1":
                        this.property.Add("OpenRowsetVariable", value);
                        break;
                    case "2":
                        this.property.Add("SqlCommand", value);
                        break;
                    case "3":
                        this.property.Add("SqlCommandVariable", value);
                        break;
                    default:
                        this.property.Add("SqlCommand", value);
                        break;
                }
            }
        }
        public int CommandTimeout
        {
            get { return (int)this.property["CommandTimeout"]; }
            set { this.property.Add("CommandTimeout", value); }
        }
        public DECustomPropertyCollection CustomPropertyCollection
        {
            get { return this.property; }
        }
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class OleDbSource:IDeSource
    {

        private static Dictionary<string, List<string>> ProviderList = new Dictionary<string, List<string>>
        {
            {"SQL", new List<string> {"SQLNCLI10","Integrated Security=SSPI;Persist Security Info=False;Auto Translate=False;","TSQL SQL Server"}},
            {"MDX",new List<string> {"MSOLAP","Integrated Security=SSPI;Extended Properties=\"Format=Tabular\";","MDX SSAS"}},
            {"XMLA",new List<string> {"MSOLAP","Integrated Security=SSPI;Extended Properties=\"Format=Tabular\";","XMLA SSAS"}},
        };

        private static string connstr = "Provider={0};Data Source={1};Initial Catalog={2};{3}";



        public OleDbSource()
        {
            this.QueryType = "SQL";
        }

        public DBConnection DBConnection { get; set; }
        public OleDBSourceProperties CustomProperties { get; set; }
        public string QueryType { get; set; }

        #region IDeSource
        public bool IsValid { get; set; }
        public bool Test(IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            return true;
        }
        public SourceType Type
        {
            get { return SourceType.OleDb; }
        }

        public string Description
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "Extracting from {0} source" , Type.ToString());
            }
        }

        public string ConnectionString
        {
            get
            {
                return (String.IsNullOrEmpty(DBConnection.ConnectionString) ?
                    String.Format(connstr, ProviderList[QueryType][0], DBConnection.Server, DBConnection.Database, ProviderList[QueryType][1])
                    : DBConnection.ConnectionString);
            }
        }
        #endregion
    }

  
    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class FlatFileSourceProperties
    {
        private SSISFlatFileConnectionProperties fileproperty = new SSISFlatFileConnectionProperties();
        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public int CodePage
        {
            get { return this.fileproperty.CodePage; }
            set { this.fileproperty.CodePage = value; }
        }
        public bool ColumnNamesInFirstDataRow
        {
            get { return this.fileproperty.ColumnNamesInFirstDataRow; }
            set { this.fileproperty.ColumnNamesInFirstDataRow = value; }
        }
        public string ConnectionString
        {
            get { return this.fileproperty.ConnectionString; }
            set { this.fileproperty.ConnectionString = value; }
        }
        public int DataRowsToSkip
        {
            get { return this.fileproperty.DataRowsToSkip; }
            set { this.fileproperty.DataRowsToSkip = value; }
        }
        public string Format
        {
            get { return this.fileproperty.Format; }
            set { this.fileproperty.Format = value; }
        }
        public string HeaderRowDelimiter
        {
            get { return this.fileproperty.HeaderRowDelimiter; }
            set { this.fileproperty.HeaderRowDelimiter = value; }
        }
        public int HeaderRowsToSkip
        {
            get { return this.fileproperty.HeaderRowsToSkip; }
            set { this.fileproperty.HeaderRowsToSkip = value; }
        }
        public string TextQualifier
        {
            get { return this.fileproperty.TextQualifier; }
            set { this.fileproperty.TextQualifier = value; }
        }
        public bool Unicode
        {
            get { return this.fileproperty.Unicode; }
            set { this.fileproperty.Unicode = value; }
        }
        public string FileName
        {
            get { return this.fileproperty.ConnectionString; }
        }
        public string ColumnDelimiter
        {
            get { return this.fileproperty.ColumnDelimiter; }
            set { this.fileproperty.ColumnDelimiter = value ; }
        }
        public string RecordDelimiter
        {
            get { return this.fileproperty.RecordDelimiter; }
            set { this.fileproperty.RecordDelimiter = value; }
        }
        public string StagingAreaTableName {get; set;}

        public string FileNameColumnName
        {
            get { return (string)this.property["FileNameColumnName"]; }
            set { this.property.Add("FileNameColumnName", value); }
        }
        public bool RetainNulls
        {
            get { return (bool)this.property["RetainNulls"]; }
            set { this.property.Add("RetainNulls", value); }
        }
        
        public DECustomPropertyCollection CustomPropertyCollection
        {
            get { return this.property; }
        }
        public SSISFlatFileConnectionProperties FlatFileConnectionProperties
        {
            get { return this.fileproperty; }
        }

    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class FlatFileSource : IDeSource
    {

        public FlatFileSourceProperties CustomProperties { get; set; }
        public string CompressionType { get; set; }

        #region IDeSource
        public bool IsValid { get; set; }
        public bool Test(IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            return true;
        }
        public SourceType Type
        {
            get { return SourceType.FlatFile; }
        }

        public string Description
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "Extracting from {0} source", Type.ToString());
            }
        }
        public string ConnectionString
        {
            get
            {
                return CustomProperties.ConnectionString;
            }
        }

        #endregion


    }


    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class ExcelSourceProperties
    {
        private static readonly Dictionary<string, int> AccessModeList = new Dictionary<string, int>
            {
                {"OpenRowset",0},
                {"OpenRowset From Variable",1},
                {"SQL Command",2},
                {"SQL Command From Variable",3},
            };

        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public ExcelSourceProperties()
        {
            this.AccessMode = "OpenRowset";
        }

        public string AccessMode
        {
            get { return this.property["AccessMode"].ToString(); }
            set
            {
                int ival = AccessModeList.ContainsKey(value) ? AccessModeList[value] : 0;
                this.property.Add("AccessMode", ival);
            }
        }
        public string SqlCommandVariable
        {
            get { return (string)this.property["SqlCommandVariable"]; }
        }
        public string SqlCommand
        {
            get { return (string)this.property["SqlCommand"]; }
            set { setCommand(value);}
        }

        public string OpenRowset
        {
            get { return (string)this.property["OpenRowset"]; }
            set { setCommand(value); }
        }

        public string OpenRowsetVariable
        {
            get { return (AccessMode == "1") ? (string)this.property["OpenRowset"] : String.Empty; }
        }

        public int CommandTimeout
        {
            get { return (int)this.property["CommandTimeout"]; }
            set { this.property.Add("CommandTimeout", value); }
        }
        public DECustomPropertyCollection CustomPropertyCollection
        {
            get { return this.property; }
        }
        private void setCommand(string value)
        {
            switch (this.AccessMode)
            {
                case "0":
                    this.property.Add("OpenRowset", value);
                    break;
                case "1":
                    this.property.Add("OpenRowsetVariable", value);
                    break;
                case "2":
                    this.property.Add("SqlCommand", value);
                    break;
                case "3":
                    this.property.Add("SqlCommandVariable", value);
                    break;
                default:
                    this.property.Add("SqlCommand", value);
                    break;
            }
        }
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class ExcelSource : IDeSource
    {

        private static Dictionary<string, List<string>> ExcelVersionList = new Dictionary<string, List<string>>
        {
            {"Microsoft Excel 97-2003", new List<string> {"Microsoft.Jet.OLEDB.4.0","Excel 8.0"}},
            {"Microsoft Excel 2007",new List<string> {"Microsoft.ACE.OLEDB.12.0","Excel 12.0"}},
            {"Microsoft Excel 2010",new List<string> {"Microsoft.ACE.OLEDB.12.0","Excel 12.0"}},
        };

        private static string connstr = "Provider={0};Data Source={1};Extended Properties=\"{2};HDR={3}\";";

        [XmlElement("ConnectionString")]
        public string ExcelConnectionString  { get; set; }

        public ExcelSourceProperties CustomProperties { get; set; }
        public string CompressionType { get; set; }
        public string FilePath { get; set; }
        public bool Header { get; set; }
        public string ExcelVersion { get; set; }
        public string StagingAreaTableName { get; set; }

        #region IDeSources
        public bool IsValid { get; set; }
        public bool Test(IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            return true;
        }
        public SourceType Type
        {
            get { return SourceType.Excel; }
        }

        public string Description
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "Extracting from {0} source", Type.ToString());
            }
        }
        public string ConnectionString
        {
            get
            {
                return (String.IsNullOrEmpty(ExcelConnectionString) ?
                    String.Format(connstr, ExcelVersionList[ExcelVersion][0], FilePath, ExcelVersionList[ExcelVersion][1], Header ? "YES" : "NO")
                    : ExcelConnectionString);
            }
        }
        #endregion

    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class SharePointSourceProperties
    {
        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public int BatchSize
        {
            get { return (int)this.property["BatchSize"]; }
            set { this.property.Add("BatchSize", value); }
        }
        public string CamlQuery
        {
            get { return (string)this.property["CamlQuery"]; }
            set { this.property.Add("CamlQuery", value); }
        }
        public bool IncludeFolders
        {
            get { return (bool)this.property["IncludeFolders"]; }
            set { this.property.Add("IncludeFolders", value); }
        }
        public bool IsRecursive
        {
            get { return (bool)this.property["IsRecursive"]; }
            set { this.property.Add("IsRecursive", value); }
        }
        public string SharePointCulture
        {
            get { return (string)this.property["SharePointCulture"]; }
            set { this.property.Add("SharePointCulture", value); }
        }
        public string SiteListName
        {
            get { return (string)this.property["SiteListName"]; }
            set { this.property.Add("SiteListName", value); }
        }
        public string SiteListViewName
        {
            get { return (string)this.property["SiteListViewName"]; }
            set { this.property.Add("SiteListViewName", value); }
        }
        public string SiteUrl
        {
            get { return (string)this.property["SiteUrl"]; }
            set { this.property.Add("SiteUrl", value); }
        }

        public DECustomPropertyCollection CustomPropertyCollection
        {
            get { return this.property; }
        }

    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class SharePointSource : IDeSource
    {

        public SharePointSourceProperties CustomProperties { get; set; }

        #region IDeSource
        public bool IsValid { get; set; }
        public bool Test(IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            return true;
        }
        public SourceType Type
        {
            get { return SourceType.SPList; }
        }

        public string Description
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "Extracting from {0} source", Type.ToString());
            }
        }
        public string ConnectionString
        {
            get
            {
                return CustomProperties.SiteUrl + "//" + CustomProperties.SiteListName;
            }
        }

        #endregion

    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class AdoNetSourceProperties
    {
        private static readonly Dictionary<string, int> AccessModeList = new Dictionary<string, int>
            {
                {"Table or view",0},
                {"SQL Command",2},
            };

        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public AdoNetSourceProperties()
        {
            this.AccessMode = "SQL Command";
        }

        public string AccessMode
        {
            get { return this.property["AccessMode"].ToString(); }
            set
            {
                int ival = AccessModeList.ContainsKey(value) ? AccessModeList[value] : 2;
                this.property.Add("AccessMode", ival);
            }
        }
        public string TableOrViewName
        {
            get { return (string)this.property["TableOrVieName"]; }
        }
        public string SqlCommand
        {
            get { return (string)this.property["SqlCommand"]; }
            set
            {
                switch (AccessMode)
                {
                    case "0":
                        this.property.Add("TableOrViewName", value);
                        break;
                    case "2":
                        this.property.Add("SqlCommand", value);
                        break;
                    default:
                        this.property.Add("SqlCommand", value);
                        break;
                }
            }
        }
        public int CommandTimeout
        {
            get { return (int)this.property["CommandTimeout"]; }
            set { this.property.Add("CommandTimeout", value); }
        }
        public bool AllowImplicitStringConversion
        {
            get { return (bool)this.property["AllowImplicitStringConversion"]; }
            set { this.property.Add("AllowImplicitStringConversion", value); }
        }
        public DECustomPropertyCollection CustomPropertyCollection
        {
            get { return this.property; }
        }
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class AdoNetSource : IDeSource
    {

        public DBConnection DBConnection { get; set; }
        public AdoNetSourceProperties CustomProperties { get; set; }
        public string QueryType { get; set; }

        #region IDeSource
        public bool IsValid { get; set; }
        public bool Test(IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            return true;
        }
        public SourceType Type
        {
            get { return SourceType.AdoNet; }
        }

        public string Description
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "Extracting from {0} source", Type.ToString());
            }
        }
        public string ConnectionString
        {
            get
            {
                return (DBConnection.ConnectionString);
            }
        }
        #endregion
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class OdbcSourceProperties
    {
        private static readonly Dictionary<string, int> AccessModeList = new Dictionary<string, int>
            {
                {"Table Name",0},
                {"SQL Command",1},
            };

        private static readonly Dictionary<string, int> BindNumericModeList = new Dictionary<string, int>
            {
                {"Char",0},
                {"Numeric",1},
            };

        private static readonly Dictionary<string, int> BindCharModeList = new Dictionary<string, int>
            {
                {"Unicode",0},
                {"ANSI",1}
            };

        private static readonly Dictionary<string, int> FetchModeList = new Dictionary<string, int>
            {
                {"Row by row",0},
                {"Batch",1}
            };


        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public OdbcSourceProperties()
        {
            this.AccessMode = "SQL Command";
        }

        public string AccessMode
        {
            get { return this.property["AccessMode"].ToString(); }
            set
            {
                int ival = AccessModeList.ContainsKey(value) ? AccessModeList[value] : 2;
                this.property.Add("AccessMode", ival);
            }
        }
        public string TableName
        {
            get { return (string)this.property["TableName"]; }
        }
        public string SqlCommand
        {
            get { return (string)this.property["SqlCommand"]; }
            set
            {
                switch (AccessMode)
                {
                    case "0":
                        this.property.Add("TableName", value);
                        break;
                    case "1":
                        this.property.Add("SqlCommand", value);
                        break;
                    default:
                        this.property.Add("SqlCommand", value);
                        break;
                }
            }
        }
        public int BatchSize
        {
            get { return (int)this.property["BatchSize"]; }
            set { this.property.Add("BatchSize", value); }
        }
        public int LobChunkSize
        {
            get { return (int)this.property["LobChunkSize"]; }
            set { this.property.Add("LobChunkSize", value); }
        }
        public bool ExposeCharColumnsAsUnicode
        {
            get { return (bool)this.property["ExposeCharColumnsAsUnicode"]; }
            set { this.property.Add("ExposeCharColumnsAsUnicode", value); }
        }
        public string FetchMethod
        {
            get { return this.property["FetchMethod"].ToString(); }
            set
            {
                int ival = FetchModeList.ContainsKey(value) ? FetchModeList[value] : 0;
                this.property.Add("FetchMethod", ival);
            }
        }
        public int StatementTimeout
        {
            get { return (int)this.property["StatementTimeout"]; }
            set { this.property.Add("StatementTimeout", value); }
        }
        public int DefaultCodePage
        {
            get { return (int)this.property["DefaultCodePage"]; }
            set { this.property.Add("DefaultCodePage", value); }
        }
        public string BindNumericAs
        {
            get { return this.property["BindNumericAs"].ToString(); }
            set
            {
                int ival = BindNumericModeList.ContainsKey(value) ? BindNumericModeList[value] : 0;
                this.property.Add("BindNumericAs", ival);
            }
        }
        public string BindCharColumnsAs
        {
            get { return this.property["BindCharColumnsAs"].ToString(); }
            set
            {
                int ival = BindCharModeList.ContainsKey(value) ? BindCharModeList[value] : 1;
                this.property.Add("BindCharColumnsAs", ival);
            }
        }
        public DECustomPropertyCollection CustomPropertyCollection
        {
            get { return this.property; }
        }
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class OdbcSource : IDeSource
    {

        public DBConnection DBConnection { get; set; }
        public OdbcSourceProperties CustomProperties { get; set; }

        #region IDeSource
        public bool IsValid { get; set; }
        public bool Test(IWorkflowLogger logger)
        {
            logger.WriteDebug("Not implemented");
            return true;
        }
        public SourceType Type
        {
            get { return SourceType.Odbc; }
        }

        public string Description
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "Extracting from {0} source", Type.ToString());
            }
        }
        public string ConnectionString
        {
            get
            {
                return (DBConnection.ConnectionString);
            }
        }
        #endregion
    }

}