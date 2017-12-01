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
using System.Runtime.Serialization;
using System.Globalization;
using System.Linq;
using System.Data.Common;
//using Excel = Microsoft.Office.Interop.Excel;

using Serilog;
using ControllerRuntime;

namespace BIAS.Framework.DeltaExtractor
{

    public enum DbProvider : ushort
    {
        Unknown = 0,
        NoSupport = 1,
        SqlServer = 2,
        Oracle = 3,
        DB2 = 4,
        MySql = 5,
        Hive = 6,
        HBase = 7,
    }

    
    public enum DestinationType : ushort
    {
        Unknown = 0,
        OleDb = 1,
        FlatFile = 2,
        Excel = 3,
        SPList = 4,
        AdoNet = 5,
        Odbc = 6,
        SqlBulk = 7
    }


    [XmlRoot(Namespace = "DeltaExtractor.XSD", ElementName = "PartitionRange")]
    public class PartitionRange
    {
        [XmlAttribute("Min")]
        public int Min { get; set; }

        [XmlAttribute("Max")]
        public int Max { get; set; }

    }


    [XmlRoot(Namespace = "DeltaExtractor.XSD", ElementName = "StagingBlock")]
    public class StagingBlock
    {
        [XmlAttribute("Staging")]
        public bool Staging { get; set; }

        public string StagingTableName { get; set; }
        public string StagingTablePrepare { get; set; }
        public string StagingTableUpload { get; set; }

        private bool reload = false;
        private string uop;
        public string UserOptions
        {
            get { return this.uop; }
            set
            {
                this.uop = value;
                this.reload = this.uop.ToLower().Contains("reload");
            }
        }

        public bool Reload { get { return this.reload; } }

    }

    public interface IDeDestination
    {        
        PartitionRange PartitionRange { get; set; }
        StagingBlock StagingBlock { get; set; }
        bool IsValid { get; set; }
        bool Test(ILogger logger);
        DestinationType Type { get; }
        string Description { get; }
        string ConnectionString { get; }
        object DbSupportObject { get; set; }
    }


    abstract public class DeDestination : IDeDestination
    {
        DbProvider dbprovider = DbProvider.SqlServer;
        
        virtual public PartitionRange PartitionRange { get; set; }
        virtual public StagingBlock StagingBlock { get; set; }
        virtual public bool IsValid { get; set; }
        virtual public bool Test(ILogger logger)
        {
            logger.Debug("Not implemented");
            this.IsValid = true;
            return true;        
        }
        virtual public DestinationType Type { get { return DestinationType.Unknown; } }
        virtual public string Description
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "Loading to {0} destination", Type.ToString());
            }
        }
        virtual public string ConnectionString { get { return String.Empty; } }
        virtual public object DbSupportObject { get; set; }
        virtual public DbProvider DbProvider
        {
            get
            {
                return this.dbprovider;
            }
            set
            {
                this.dbprovider = value;
            }
        }

    }
    
    
    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class DataDestination
    {

        private List<object> dest = new List<object> ();

        public List<object> Destinations
        {
            get { return dest; }
        }

        //[XmlArray("OleDbDestination")]
        //[XmlArrayItem("OleDbDestination")]
        [XmlElement("OleDbDestination")] 
        //public OleDbDestination[] OleDbDestination {get; set;}
        public OleDbDestination[] OleDbDestination
        {
            get
            {
                IEnumerable<object> res = dest.Where(d => ((IDeDestination)d).Type == DestinationType.OleDb);
                return res.Cast<OleDbDestination>().ToArray();
            }           
            set
            {
                if (value != null)          
                {
                    dest.AddRange(value);
                }
            }
        }

        [XmlElement("FlatFileDestination")]
        //public FlatFileDestination[] FlatFileDestination {get; set;}
        public FlatFileDestination[] FlatFileDestination
        {
            get
            {
                IEnumerable<object> res = dest.Where(d => ((IDeDestination)d).Type == DestinationType.FlatFile);
                return res.Cast<FlatFileDestination>().ToArray();
            }
            set
            {
                if (value != null)
                {
                    dest.AddRange(value);
                }
            }
        }

        [XmlElement("ExcelDestination")]
        //public ExcelDestination[] ExcelDestination { get; set; }
        public ExcelDestination[] ExcelDestination
        {
            get
            {
                IEnumerable<object> res = dest.Where(d => ((IDeDestination)d).Type == DestinationType.Excel);
                return res.Cast<ExcelDestination>().ToArray();
            }
            set
            {
                if (value != null)
                {
                    dest.AddRange(value);
                }
            }
        }

        [XmlElement("SharePointDestination")]
        //public SharePointDestination[] SharePointDestination { get; set; }
        public SharePointDestination[] SharePointDestination
        {
            get
            {
                IEnumerable<object> res = dest.Where(d => ((IDeDestination)d).Type == DestinationType.SPList);
                return res.Cast<SharePointDestination>().ToArray();
            }
            set
            {
                if (value != null)
                {
                    dest.AddRange(value);
                }
            }
        }

        [XmlElement("AdoNetDestination")]
        //public AdoNetDestination[] AdoNetDestination { get; set; }
        public AdoNetDestination[] AdoNetDestination
        {
            get
            {
                IEnumerable<object> res = dest.Where(d => ((IDeDestination)d).Type == DestinationType.AdoNet);
                return res.Cast<AdoNetDestination>().ToArray();
            }
            set
            {
                if (value != null)
                {
                    dest.AddRange(value);
                }
            }
        }

        [XmlElement("OdbcDestination")]
        //public OdbcDestination[] OdbcDestination { get; set; }
        public OdbcDestination[] OdbcDestination
        {
            get
            {
                IEnumerable<object> res = dest.Where(d => ((IDeDestination)d).Type == DestinationType.Odbc);
                return res.Cast<OdbcDestination>().ToArray();
            }
            set
            {
                if (value != null)
                {
                    dest.AddRange(value);
                }
            }
        }

        [XmlElement("SqlBulkDestination")]
        //public SqlBulkDestination[] SqlBulkDestination { get; set; }
        public SqlBulkDestination[] SqlBulkDestination
        {
            get
            {
                IEnumerable<object> res = dest.Where(d => ((IDeDestination)d).Type == DestinationType.SqlBulk);
                return res.Cast<SqlBulkDestination>().ToArray();
            }
            set
            {
                if (value != null)
                {
                    dest.AddRange(value);
                }
            }
        }

        public int Test(ILogger logger)
        {
            int cntValid = dest.Count(d => ((IDeDestination)d).Test(logger) == true);
            return cntValid;
        }

    }
  
    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class OleDBDestinationProperties
    {
        //private Dictionary<string, object> property = new Dictionary<string, object>();
        private DECustomPropertyCollection property = new DECustomPropertyCollection();
        private static readonly Dictionary<string, int> AccessModeList = new Dictionary<string, int>()
            {
                {"OpenRowset",0},
                {"OpenRowset From Variable",1},
                {"SQL Command",2},
                {"OpenRowset Using FastLoad",3},
                {"OpenRowset Using FastLoad From Variable",4},
            };

        public OleDBDestinationProperties()
        {
            this.AccessMode = "OpenRowset Using FastLoad";
        }
        
        public string AccessMode
        {
            get { return this.property["AccessMode"].ToString(); }
            set
            {
                int ival = AccessModeList.ContainsKey(value) ? AccessModeList[value] : 3;
                this.property.Add("AccessMode", ival);
            }
        }
        public string OpenRowset
        {
            get { return (string)this.property["OpenRowset"]; }
            set { setCommand(value); }
        }
        public string OpenRowsetVariable
        {
            get { return (string)this.property["OpenRowsetVariable"]; }
        }
        public string FastLoadOptions
        {
            get { return (string)this.property["FastLoadOptions"]; }
            set { this.property.Add("FastLoadOptions", value); }
        }
        public bool FastLoadKeepIdentity
        {
            get { return (bool)this.property["FastLoadKeepIdentity"]; }
            set { this.property.Add("FastLoadKeepIdentity", value); }
        }
        public bool FastLoadKeepNulls
        {
            get { return (bool)this.property["FastLoadKeepNulls"]; }
            set { this.property.Add("FastLoadKeepNulls", value); }
        }
        public int FastLoadMaxInsertCommitSize
        {
            get { return (int)this.property["FastLoadMaxInsertCommitSize"]; }
            set { this.property.Add("FastLoadMaxInsertCommitSize", value); }
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
        //public Dictionary<string, object> CustomPropertyCollection
        //{
        //    get { return this.property; }
        //}

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
                    this.property.Add("OpenRowset", value);
                    break;
                default:
                    this.property.Add("OpenRowset", value);
                    break;
            }
        }

    }

    
    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class OleDbDestination : DeDestination
    {

        private static string connstr = "Data Source={0};Initial Catalog={1};Integrated Security=SSPI;Persist Security Info=False;Auto Translate=False;";

        public DBConnection DBConnection { get; set; } 
        public string TableName
        {
            get { return String.IsNullOrEmpty(CustomProperties.OpenRowset) ? "Unknown" : CustomProperties.OpenRowset; }
        }

        public bool IsView
        {
            get
            {
                if (DbSupportObject == null)
                    return false;

                return ((IDeStagingSupport)DbSupportObject).IsView;
            }
        }
        public OleDBDestinationProperties CustomProperties {get; set;}

        #region IDeDestination
        public override bool Test(ILogger logger)
        {
            if (DbProvider == DbProvider.Unknown)
            {
                IsValid = true;
                return IsValid;
            }
            
            if (DbSupportObject == null)
            {
                if (DbProvider == DbProvider.NoSupport)
                {
                    DeNoSupportHelper deh = new DeNoSupportHelper();
                    DbSupportObject = deh;
                }
                else if (DbProvider == DbProvider.SqlServer)
                {
                    DeSqlServerHelper deh = new DeSqlServerHelper();
                    deh.TableName = TableName;
                    deh.ConnectionString = ConnectionString;
                    deh.DBConnection = DBConnection;
                    deh.PartitionRange = PartitionRange;
                    deh.StagingBlock = StagingBlock;
                    DbSupportObject = deh;
                }
                else
                {
                    throw new DeltaExtractorBuildException(String.Format("{0}. Staging support is not available for this destination",DbProvider.ToString()));
                }
            }

            IsValid = ((IDeStagingSupport)DbSupportObject).Test(logger);

            return IsValid;
        }

        public override string ConnectionString
        {
            get
            {
                return (String.IsNullOrEmpty(DBConnection.ConnectionString) ?
                    String.Format(connstr, DBConnection.Server, DBConnection.Database)
                    : DBConnection.ConnectionString);
            }
        }
        public override DestinationType Type
        {
            get
            {
                return DestinationType.OleDb;
            }
        }
        #endregion

    }




    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class FlatFileDestinationProperties
    {
        //private string this.Header;
        //private string this.Override;
        //private Dictionary<string, object> this.property = new Dictionary<string, object>();
        //do not use: XDestination compatibility
        //private string this.DataCompression;
        //private string this.DataFormat;
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
            set { this.fileproperty.ColumnDelimiter = value; }
        }
        public string RecordDelimiter
        {
            get { return this.fileproperty.RecordDelimiter; }
            set { this.fileproperty.RecordDelimiter = value; }
        }
        public string StagingAreaTableName {get; set;}
        public string Header
        {
            get { return (string)this.property["Header"]; }
            set { this.property.Add("Header", value); }
        }
        public bool Override
        {
            get { return (bool)this.property["Override"]; }
            set { this.property.Add("Override", value); }
        }
        public string DataCompression
        {
            get { return (string)this.property["DataCompression"]; }
            set { this.property.Add("DataCompression", value); }
        }
        public string DataFormat
        {
            get { return (string)this.property["DataFormat"]; }
            set { this.property.Add("DataFormat", value); }
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
    public class FlatFileDestination : DeDestination
    {

        public string CompressionType {get; set;}
        public FlatFileDestinationProperties CustomProperties {get; set;}
        public string FullName
        {
        get {return this.CustomProperties.FileName;}
        }
        public string FileName
        {
            get { return Path.GetFileName(this.CustomProperties.FileName); }
        }
        public string FilePath
        {
            get { return Path.GetFullPath(this.CustomProperties.FileName); }
        }
        #region IDeDestination
        public override DestinationType Type
        {
            get
            {
                return DestinationType.FlatFile;
            }
        }
        public override string ConnectionString
        {
            get
            {
                return (CustomProperties.ConnectionString);
            }
        }
        #endregion


    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class ExcelDestinationProperties
    {
        private static readonly Dictionary<string, int> AccessModeList = new Dictionary<string, int>
            {
                {"OpenRowset",0},
                {"OpenRowset From Variable",1},
                {"SQL Command",2},
                {"SQL Command From Variable",3},
            };

        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public ExcelDestinationProperties()
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

        public string OpenRowset
        {
            get { return (string)this.property["OpenRowset"]; }
            set
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
                        this.property.Add("OpenRowset", value);
                        break;
                }
            }
        }

        public string OpenRowsetVariable
        {
            get { return (string)this.property["OpenRowsetVariable"]; }
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
    public class ExcelDestination : DeDestination
    {

        private static Dictionary<string, List<string>> ExcelVersionList = new Dictionary<string, List<string>>
        {
            {"Microsoft Excel 97-2003", new List<string> {"Microsoft.Jet.OLEDB.4.0","Excel 8.0"}},
            {"Microsoft Excel 2007",new List<string> {"Microsoft.ACE.OLEDB.12.0","Excel 12.0"}},
            {"Microsoft Excel 2010",new List<string> {"Microsoft.ACE.OLEDB.12.0","Excel 12.0"}},
        };

        private static string connstr = "Provider={0};Data Source={1};Extended Properties=\"{2};HDR={3}\";";

        [XmlElement("ConnectionString")]
        public string ExcelConnectionString { get; set; }

        public ExcelDestinationProperties CustomProperties { get; set; }
        public string CompressionType { get; set; }
        public string FilePath { get; set; }
        public bool Header { get; set; }
        public string ExcelVersion { get; set; }
        public string StagingAreaTableName { get; set; }

        #region IDeDestination
        public override DestinationType Type
        {
            get
            {
                return DestinationType.Excel;
            }
        }
        public override string ConnectionString
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
    public class SharePointDestinationProperties
    {

        private static readonly Dictionary<string, int> BatchTypeList = new Dictionary<string, int>()
            {
                {"Modification",0},
                {"Deletion",1},
            };

        
        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public int BatchSize
        {
            get { return (int)this.property["BatchSize"]; }
            set { this.property.Add("BatchSize", value); }
        }
        public string BatchType
        {
            get { return this.property["BatchType"].ToString(); }
            set
            {
                int ival = BatchTypeList.ContainsKey(value) ? BatchTypeList[value] : 0;
                this.property.Add("BatchType", ival);
            }
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
    public class SharePointDestination : DeDestination
    {
        public SharePointDestinationProperties CustomProperties { get; set; }

        #region IDeDestination
        public override DestinationType Type
        {
            get
            {
                return DestinationType.SPList;
            }
        }
        public override string ConnectionString
        {
            get
            {
                return (CustomProperties.SiteUrl + "//" + CustomProperties.SiteListName);
            }
        }

        #endregion
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class AdoNetDestinationProperties
    {
        //private Dictionary<string, object> property = new Dictionary<string, object>();
        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public string TableOrViewName
        {
            get { return (string)this.property["TableOrViewName"]; }
            set { this.property.Add("TableOrViewName", value); }
        }
        public int BatchSize
        {
            get { return (int)this.property["BatchSize"]; }
            set { this.property.Add("BatchSize", value); }
        }
        public bool UseBulkInsertWhenPossible
        {
            get { return (bool)this.property["UseBulkInsertWhenPossible"]; }
            set { this.property.Add("UseBulkInsertWhenPossible", value); }
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
    public class AdoNetDestination : DeDestination
    {

        private static string connstr = "Data Source={0};Initial Catalog={1};Integrated Security=SSPI;Persist Security Info=False;";

        public AdoNetDestinationProperties CustomProperties { get; set; }
        public DBConnection DBConnection { get; set; }

        public bool IsView
        {
            get
            {
                if (DbSupportObject == null)
                    return false;

                return ((IDeStagingSupport)DbSupportObject).IsView;
            }
        }

        #region IDeDestination
        public override bool Test(ILogger logger)
        {
            if (DbProvider == DbProvider.Unknown)
            {
                IsValid = true;
                return IsValid;
            }

            if (DbSupportObject == null)
            {
                if (DbProvider == DbProvider.NoSupport)
                {
                    DeNoSupportHelper deh = new DeNoSupportHelper();
                    DbSupportObject = deh;
                }
                else if (DbProvider == DbProvider.SqlServer)
                {
                    DeSqlServerHelper deh = new DeSqlServerHelper();
                    deh.TableName = CustomProperties.TableOrViewName;
                    deh.ConnectionString = ConnectionString;
                    deh.DBConnection = DBConnection;
                    deh.PartitionRange = PartitionRange;
                    deh.StagingBlock = StagingBlock;
                    DbSupportObject = deh;
                }
                else
                {
                    throw new DeltaExtractorBuildException(String.Format("{0}. Staging support is not available for this destination", DbProvider.ToString()));
                }
            }

            IsValid = ((IDeStagingSupport)DbSupportObject).Test(logger);

            return IsValid;
        }

        public override DestinationType Type
        {
            get
            {
                return DestinationType.AdoNet;
            }
        }
        public override string ConnectionString
        {
            get
            {
                return (String.IsNullOrEmpty(DBConnection.ConnectionString) ?
                    String.Format(connstr, DBConnection.Server, DBConnection.Database)
                    : DBConnection.ConnectionString);
            }
        }

        #endregion
    }


    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class OdbcDestinationProperties
    {


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

        private static readonly Dictionary<string, int> InsertModeList = new Dictionary<string, int>
            {
                {"Row by row",0},
                {"Batch",1}
            };
      
        
        //private Dictionary<string, object> property = new Dictionary<string, object>();
        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public string InsertMethod
        {
            get { return this.property["InsertMethod"].ToString(); }
            set
            {
                int ival = InsertModeList.ContainsKey(value) ? InsertModeList[value] : 0;
                this.property.Add("InsertMethod", ival);
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
        public string BindNumericAs
        {
            get { return this.property["BindNumericAs"].ToString(); }
            set
            {
                int ival = BindNumericModeList.ContainsKey(value) ? BindNumericModeList[value] : 0;
                this.property.Add("BindNumericAs", ival);
            }
        }
        public string TableName
        {
            get { return (string)this.property["TableName"]; }
            set { this.property.Add("TableName", value); }
        }
        public int BatchSize
        {
            get { return (int)this.property["BatchSize"]; }
            set { this.property.Add("BatchSize", value); }
        }
        public int TransactionSize
        {
            get { return (int)this.property["TransactionSize"]; }
            set { this.property.Add("TransactionSize", value); }
        }
        public int LobChunkSize
        {
            get { return (int)this.property["LobChunkSize"]; }
            set { this.property.Add("LobChunkSize", value); }
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
        public DECustomPropertyCollection CustomPropertyCollection
        {
            get { return this.property; }
        }
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class OdbcDestination : DeDestination
    {

        private static string connstr = "server={0};Driver={SQL Server Native Client 10.0};trusted_connection=Yes;app=DE;database={1};";
        
        public OdbcDestinationProperties CustomProperties { get; set; }
        public DBConnection DBConnection { get; set; }

        public bool IsView
        {
            get
            {
                if (DbSupportObject == null)
                    return false;

                return ((IDeStagingSupport)DbSupportObject).IsView;
            }
        }
        #region IDeDestination
        public override bool Test(ILogger logger)
        {
            if (DbProvider == DbProvider.Unknown)
            {
                IsValid = true;
                return IsValid;
            }

            if (DbSupportObject == null)
            {
                if (DbProvider == DbProvider.NoSupport)
                {
                    DeNoSupportHelper deh = new DeNoSupportHelper();
                    DbSupportObject = deh;
                }
                else if (DbProvider == DbProvider.SqlServer)
                {
                    DeSqlServerHelper deh = new DeSqlServerHelper();
                    deh.TableName = CustomProperties.TableName;
                    deh.ConnectionString = ConnectionString;
                    deh.DBConnection = DBConnection;
                    deh.PartitionRange = PartitionRange;
                    deh.StagingBlock = StagingBlock;
                    DbSupportObject = deh;
                }
                else
                {
                    throw new DeltaExtractorBuildException(String.Format("{0}. Staging support is not available for this destination", DbProvider.ToString()));
                }
            }

            IsValid = ((IDeStagingSupport)DbSupportObject).Test(logger);

            return IsValid;
        }

        public override DestinationType Type
        {
            get
            {
                return DestinationType.Odbc;
            }
        }
        public override string ConnectionString
        {
            get
            {
                return (String.IsNullOrEmpty(DBConnection.ConnectionString) ?
                    String.Format(connstr, DBConnection.Server, DBConnection.Database)
                    : DBConnection.ConnectionString);
            }
        }

        #endregion
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class SqlBulkDestinationProperties
    {
        //private Dictionary<string, object> property = new Dictionary<string, object>();
        private DECustomPropertyCollection property = new DECustomPropertyCollection();

        public int DefaultCodePage
        {
            get { return (int)this.property["DefaultCodePage"]; }
            set { this.property.Add("DefaultCodePage", value); }
        }
        public bool AlwaysUseDefaultCodePage
        {
            get { return (bool)this.property["AlwaysUseDefaultCodePage"]; }
            set { this.property.Add("AlwaysUseDefaultCodePage", value); }
        }
        public string BulkInsertTableName
        {
            get { return (string)this.property["BulkInsertTableName"]; }
            set { this.property.Add("BulkInsertTableName", value); }
        }
        public bool BulkInsertCheckConstraints
        {
            get { return (bool)this.property["BulkInsertCheckConstraints"]; }
            set { this.property.Add("BulkInsertCheckConstraints", value); }
        }
        public int BulkInsertFirstRow
        {
            get { return (int)this.property["BulkInsertFirstRow"]; }
            set { this.property.Add("BulkInsertFirstRow", value); }
        }
        public bool BulkInsertFireTriggers
        {
            get { return (bool)this.property["BulkInsertFireTriggers"]; }
            set { this.property.Add("BulkInsertFireTriggers", value); }
        }
        public bool BulkInsertKeepIdentity
        {
            get { return (bool)this.property["BulkInsertKeepIdentity"]; }
            set { this.property.Add("BulkInsertKeepIdentity", value); }
        }
        public bool BulkInsertKeepNulls
        {
            get { return (bool)this.property["BulkInsertKeepNulls"]; }
            set { this.property.Add("BulkInsertKeepNulls", value); }
        }
        public int BulkInsertLastRow
        {
            get { return (int)this.property["BulkInsertLastRow"]; }
            set { this.property.Add("BulkInsertLastRow", value); }
        }
        public int BulkInsertMaxErrors
        {
            get { return (int)this.property["BulkInsertMaxErrors"]; }
            set { this.property.Add("BulkInsertMaxErrors", value); }
        }
        public string BulkInsertOrder
        {
            get { return (string)this.property["BulkInsertOrder"]; }
            set { this.property.Add("BulkInsertOrder", value); }
        }
        public bool BulkInsertTablock
        {
            get { return (bool)this.property["BulkInsertTablock"]; }
            set { this.property.Add("BulkInsertTablock", value); }
        }
        public int Timeout
        {
            get { return (int)this.property["Timeout"]; }
            set { this.property.Add("Timeout", value); }
        }
        public int MaxInsertCommitSize
        {
            get { return (int)this.property["MaxInsertCommitSize"]; }
            set { this.property.Add("MaxInsertCommitSize", value); }
        }
        public DECustomPropertyCollection CustomPropertyCollection
        {
            get { return this.property; }
        }
    }

    [XmlRoot(Namespace = "DeltaExtractor.XSD")]
    public class SqlBulkDestination : DeDestination
    {
        private static string connstr = "Provider=SQLOLEDB.1;Data Source={0};Initial Catalog={1};Integrated Security=SSPI;Persist Security Info=False;Auto Translate=False;";

        public SqlBulkDestinationProperties CustomProperties { get; set; }
        public DBConnection DBConnection { get; set; }

        public bool IsView
        {
            get
            {
                if (DbSupportObject == null)
                    return false;

                return ((IDeStagingSupport)DbSupportObject).IsView;
            }
        }
        #region IDeDestination
        public override bool Test(ILogger logger)
        {
            if (DbProvider == DbProvider.Unknown)
            {
                IsValid = true;
                return IsValid;
            }

            if (DbSupportObject == null)
            {
                if (DbProvider == DbProvider.NoSupport)
                {
                    DeNoSupportHelper deh = new DeNoSupportHelper();
                    DbSupportObject = deh;
                }
                else if (DbProvider == DbProvider.SqlServer)
                {
                    DeSqlServerHelper deh = new DeSqlServerHelper();
                    deh.TableName = CustomProperties.BulkInsertTableName;
                    deh.ConnectionString = ConnectionString;
                    deh.DBConnection = DBConnection;
                    deh.PartitionRange = PartitionRange;
                    deh.StagingBlock = StagingBlock;
                    DbSupportObject = deh;
                }
                else
                {
                    throw new DeltaExtractorBuildException(String.Format("{0}. Staging support is not available for this destination", DbProvider.ToString()));
                }
            }

            IsValid = ((IDeStagingSupport)DbSupportObject).Test(logger);

            return IsValid;
        }

        public override DestinationType Type
        {
            get
            {
                return DestinationType.SqlBulk;
            }
        }
        public override string ConnectionString
        {
            get
            {
                return (String.IsNullOrEmpty(DBConnection.ConnectionString) ?
                    String.Format(connstr, DBConnection.Server, DBConnection.Database)
                    : DBConnection.ConnectionString);
            }
        }

        #endregion
    }

}