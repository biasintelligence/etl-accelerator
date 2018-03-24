CREATE XML SCHEMA COLLECTION [dbo].[ETLClient_DE]
    AS N'<?xml version="1.0"?>
<xsd:schema xmlns="DeltaExtractor.XSD" xmlns:xsd="http://www.w3.org/2001/XMLSchema" targetNamespace="DeltaExtractor.XSD"  elementFormDefault="qualified">
	<!-- Guid Pattern definition -->
	<xsd:simpleType name="GUID">
		<xsd:restriction base="xsd:string">
			<xsd:pattern value="[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}" />
		</xsd:restriction>
	</xsd:simpleType>
  
	<!-- List of possible actions -->
	<xsd:simpleType name="Actions">
		<xsd:restriction base="xsd:string">
      <xsd:enumeration value="MoveData" />
    </xsd:restriction>
	</xsd:simpleType>


  <!-- Sharepoint Destination Action -->
  <xsd:simpleType name="SPAction">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="Modification" />
      <xsd:enumeration value="Deletion" />
    </xsd:restriction>
  </xsd:simpleType>

  <!-- AccessMode -->
  <xsd:simpleType name="AccessMode">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="OpenRowset" />
      <xsd:enumeration value="OpenRowset From Variable" />
      <xsd:enumeration value="SQL Command" />
      <xsd:enumeration value="SQL Command From Variable" />
      <xsd:enumeration value="Table or view" />
      <xsd:enumeration value="Table Name" />
      <xsd:enumeration value="OpenRowset Using FastLoad" />
      <xsd:enumeration value="OpenRowset Using FastLoad From Variable" />
    </xsd:restriction>
  </xsd:simpleType>

  <!-- BindNumericMode -->
  <xsd:simpleType name="NumericMode">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="Char" />
      <xsd:enumeration value="Numeric" />
    </xsd:restriction>
  </xsd:simpleType>

  <!-- BindCharMode -->
  <xsd:simpleType name="CharMode">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="ANSI" />
      <xsd:enumeration value="Unicode" />
    </xsd:restriction>
  </xsd:simpleType>

  <!-- FetchMode -->
  <xsd:simpleType name="FetchMode">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="Batch" />
      <xsd:enumeration value="Row by row" />
    </xsd:restriction>
  </xsd:simpleType>

  <xsd:simpleType name="CompressionMethod">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="GZIP" />
      <xsd:enumeration value="XPRESS" />
      <xsd:enumeration value="ZIP" />
      <xsd:enumeration value="NONE" />
    </xsd:restriction>
  </xsd:simpleType>
  
	<xsd:simpleType name="ExcelVersion">
		<xsd:restriction base="xsd:string">
      <xsd:enumeration value="Microsoft Excel 97-2003" />
      <xsd:enumeration value="Microsoft Excel 2007-2010" />
      <xsd:enumeration value="Microsoft Excel 2013" />
      <xsd:enumeration value="Microsoft Excel 2016" />
    </xsd:restriction>
	</xsd:simpleType>

  <xsd:simpleType name="FlatFileFormat">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="Delimited" />
      <xsd:enumeration value="FixedWidth" />
      <xsd:enumeration value="RaggedRight" />
    </xsd:restriction>
  </xsd:simpleType>

  <xsd:simpleType name="QueryType">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="SQL" />
      <xsd:enumeration value="MDX" />
      <xsd:enumeration value="XMLA" />
    </xsd:restriction>
  </xsd:simpleType>

  <xsd:simpleType name="PartitionFunction">
    <xsd:restriction base="xsd:string">
      <xsd:enumeration value="UPS" />
      <xsd:enumeration value="SRC" />
      <xsd:enumeration value="NONE" />
    </xsd:restriction>
  </xsd:simpleType>

  <!-- Partition block properties -->
  <xsd:complexType name="PartitionBlock">
    <xsd:sequence>
      <xsd:element name="Input" type="xsd:string" maxOccurs="1" minOccurs="0" />
      <xsd:element name="Output" type="xsd:string" maxOccurs="1" minOccurs="0" />
    </xsd:sequence>
    <xsd:attribute name="Function" type="PartitionFunction" use="required"/>
  </xsd:complexType>


  <!-- Partition range properties -->
  <xsd:complexType name="PartitionRange">
    <xsd:attribute name="Min" type="xsd:int" use="optional"/>
    <xsd:attribute name="Max" type="xsd:int" use="optional"/>
  </xsd:complexType>


  <!-- ODataSource -->
  <xsd:complexType name="ODataSource">
    <xsd:sequence>
      <xsd:element name="ConnectionString" type="xsd:string" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="DefaultStringLength" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="CollectionName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="Query" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="ResourcePath" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="UseResourcePath" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>

  <!-- SharePointSource -->
  <xsd:complexType name="SharePointSource">
    <xsd:sequence>
      <xsd:element name="ConnectionString" type="xsd:string" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="BatchSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="CamlQuery" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="IncludeFolders" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="IsRecursive" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SharePointCulture" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SiteListName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SiteListViewName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SiteUrl" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="DecodeLookupColumns" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="IncludeHiddenColumns" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="UseConnectionManager" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>

  <!-- SharePointDestination -->
  <xsd:complexType name="SharePointDestination">
    <xsd:sequence>
      <xsd:element name="ConnectionString" type="xsd:string" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="BatchSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BatchType" type="SPAction" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SharePointCulture" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SiteListName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SiteListViewName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SiteUrl" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="UseConnectionManager" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>

  <!-- FlatFileSource -->
  <xsd:complexType name="FlatFileSource">
    <xsd:sequence>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="CodePage" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="ConnectionString" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="DataRowsToSkip" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="Format" type="FlatFileFormat" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="ColumnNamesInFirstDataRow" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="HeaderRowDelimiter" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="HeaderRowsToSkip" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="TextQualifier" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="Unicode" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="ColumnDelimiter" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="RecordDelimiter" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="FileNameColumnName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="RetainNulls" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="DataCompression" type="CompressionMethod" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="StagingAreaTableName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>

  <!-- FlatFileDestination -->
  <xsd:complexType name="FlatFileDestination">
    <xsd:sequence>
      <xsd:element name="PartitionRange" type="PartitionRange" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="CodePage" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="ConnectionString" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="DataRowsToSkip" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="Format" type="FlatFileFormat" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="ColumnNamesInFirstDataRow" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="HeaderRowDelimiter" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="HeaderRowsToSkip" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="TextQualifier" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="Unicode" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="ColumnDelimiter" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="RecordDelimiter" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="Header" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="Override" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="DataCompression" type="CompressionMethod" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="StagingAreaTableName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>


  <!-- ExcelSource -->
  <xsd:complexType name="ExcelSource">
    <xsd:sequence>
      <xsd:element name="ConnectionString" type="xsd:string" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="FilePath" type="xsd:string" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="Header" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="ExcelVersion" type="ExcelVersion" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="AccessMode" type="AccessMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="OpenRowset" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SqlCommand" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="CommandTimeout" type="xsd:int" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
      <xsd:element name="DataCompression" type="CompressionMethod" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="StagingAreaTableName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
    </xsd:sequence>
  </xsd:complexType>

  <!-- ExcelDestination -->
  <xsd:complexType name="ExcelDestination">
    <xsd:sequence>
      <xsd:element name="ConnectionString" type="xsd:string" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="FilePath" type="xsd:string" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="Header" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="ExcelVersion" type="ExcelVersion" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="PartitionRange" type="PartitionRange" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="AccessMode" type="AccessMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="OpenRowset" type="xsd:string" minOccurs="1" maxOccurs="1"/>
            <xsd:element name="CommandTimeout" type="xsd:int" minOccurs="1" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
      <xsd:element name="DataCompression" type="CompressionMethod" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="StagingAreaTableName" type="xsd:string" minOccurs="0" maxOccurs="1"/>
    </xsd:sequence>
  </xsd:complexType>


  <!-- Database Connection -->
	<xsd:complexType name="DBConnection">
    <xsd:sequence>
      <xsd:choice>
        <xsd:sequence>
          <xsd:element name="Server" type="xsd:string" maxOccurs="1" minOccurs="0" />
          <xsd:element name="Database" type="xsd:string" maxOccurs="1" minOccurs="0" />
        </xsd:sequence>
        <xsd:sequence>
          <xsd:element name="ConnectionString" type="xsd:string" maxOccurs="1" minOccurs="0" />
          <xsd:element name="Qualifier" type="xsd:string" maxOccurs="1" minOccurs="0" />
        </xsd:sequence>
      </xsd:choice>
      <xsd:element name="QueryTimeout" type="xsd:integer" maxOccurs="1" minOccurs="0" />
    </xsd:sequence>
	</xsd:complexType>

	<!-- OLEDB Source -->
	<xsd:complexType name="OleDbSource">
		<xsd:sequence>
			<xsd:element name="DBConnection" type="DBConnection" minOccurs="1" maxOccurs="1"/>
      <xsd:element name="QueryType" type="QueryType" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
				<xsd:complexType>
					<xsd:sequence>
						<xsd:element name="AccessMode" type="AccessMode" minOccurs="0" maxOccurs="1"/>
						<xsd:element name="SqlCommand" type="xsd:string" minOccurs="1" maxOccurs="1"/>
						<xsd:element name="CommandTimeout" type="xsd:int" minOccurs="0" maxOccurs="1"/>
					</xsd:sequence>
				</xsd:complexType>
			</xsd:element>
		</xsd:sequence>
	</xsd:complexType>

  <!-- ADONET Source -->
  <xsd:complexType name="AdoNetSource">
    <xsd:sequence>
      <xsd:element name="DBConnection" type="DBConnection" minOccurs="1" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="AccessMode" type="AccessMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SqlCommand" type="xsd:string" minOccurs="1" maxOccurs="1"/>
            <xsd:element name="CommandTimeout" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="AllowImplicitStringConversion" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>

  <!-- ODBC Source -->
  <xsd:complexType name="OdbcSource">
    <xsd:sequence>
      <xsd:element name="DBConnection" type="DBConnection" minOccurs="1" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="AccessMode" type="AccessMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="SqlCommand" type="xsd:string" minOccurs="1" maxOccurs="1"/>
            <xsd:element name="StatementTimeout" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BatchSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="LobChunkSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="ExposeCharColumnsAsUnicode" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="FetchMethod" type="FetchMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="DefaultCodePage" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BindNumericAs" type="NumericMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BindCharColumnsAs" type="CharMode" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>


  <!-- Staging block properties -->
  <xsd:complexType name="StagingBlock">
    <xsd:sequence>
      <xsd:element name="StagingTableName" type="xsd:string" maxOccurs="1" minOccurs="0" />
      <xsd:element name="StagingTablePrepare" type="xsd:string" maxOccurs="1" minOccurs="0" />
      <xsd:element name="StagingTableUpload" type="xsd:string" maxOccurs="1" minOccurs="0" />
      <xsd:element name="UserOptions" type="xsd:string" minOccurs="0" maxOccurs="1"/>
    </xsd:sequence>
    <xsd:attribute name="Staging" type="xsd:boolean" use="required"/>
  </xsd:complexType>


  <!-- OLEDB Destination -->
	<xsd:complexType name="OleDbDestination">
		<xsd:sequence>
			<xsd:element name="DBConnection" type="DBConnection" minOccurs="1" maxOccurs="1"/>
			<xsd:element name="StagingBlock" type="StagingBlock" maxOccurs="1" minOccurs="0" />
      <xsd:element name="PartitionRange" type="PartitionRange" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
				<xsd:complexType>
					<xsd:sequence>
						<xsd:element name="AccessMode" type="AccessMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="OpenRowset" type="xsd:string" minOccurs="1" maxOccurs="1"/>
            <xsd:element name="FastLoadOptions" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="FastLoadKeepIdentity" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="FastLoadKeepNulls" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="FastLoadMaxInsertCommitSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="CommandTimeout" type="xsd:int" minOccurs="0" maxOccurs="1"/>
					</xsd:sequence>
				</xsd:complexType>
			</xsd:element>
		</xsd:sequence>
	</xsd:complexType>

  <!-- ADONET Destination -->
  <xsd:complexType name="AdoNetDestination">
    <xsd:sequence>
      <xsd:element name="DBConnection" type="DBConnection" minOccurs="1" maxOccurs="1"/>
      <xsd:element name="StagingBlock" type="StagingBlock" maxOccurs="1" minOccurs="0" />
      <xsd:element name="PartitionRange" type="PartitionRange" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="TableOrViewName" type="xsd:string" minOccurs="1" maxOccurs="1"/>
            <xsd:element name="BatchSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="UseBulkInsertWhenPossible" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="CommandTimeout" type="xsd:int" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>

  <!-- ODBC Destination -->
  <xsd:complexType name="OdbcDestination">
    <xsd:sequence>
      <xsd:element name="DBConnection" type="DBConnection" minOccurs="1" maxOccurs="1"/>
      <xsd:element name="StagingBlock" type="StagingBlock" maxOccurs="1" minOccurs="0" />
      <xsd:element name="PartitionRange" type="PartitionRange" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="InsertMethod" type="FetchMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BindCharColumnsAs" type="CharMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BindNumericAs" type="NumericMode" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="TableName" type="xsd:string" minOccurs="1" maxOccurs="1"/>
            <xsd:element name="BatchSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="TransactionSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="LobChunkSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="StatementTimeout" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="DefaultCodePage" type="xsd:int" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>

  <!-- SQLBULK Destination -->
  <xsd:complexType name="SqlBulkDestination">
    <xsd:sequence>
      <xsd:element name="DBConnection" type="DBConnection" minOccurs="1" maxOccurs="1"/>
      <xsd:element name="StagingBlock" type="StagingBlock" maxOccurs="1" minOccurs="0" />
      <xsd:element name="PartitionRange" type="PartitionRange" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="CustomProperties">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="DefaultCodePage" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="AlwaysUseDefaultCodePage" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BulkInsertTableName" type="xsd:string" minOccurs="1" maxOccurs="1"/>
            <xsd:element name="BulkInsertCheckConstraints" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BulkInsertFirstRow" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BulkInsertFireTriggers" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BulkInsertKeepIdentity" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BulkInsertKeepNulls" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BulkInsertLastRow" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BulkInsertMaxErrors" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BulkInsertOrder" type="xsd:string" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="BulkInsertTablock" type="xsd:boolean" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="Timeout" type="xsd:int" minOccurs="0" maxOccurs="1"/>
            <xsd:element name="MaxInsertCommitSize" type="xsd:int" minOccurs="0" maxOccurs="1"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>


  <!-- Complex type for destinations -->
	<xsd:complexType name="DataDestination">
    <xsd:sequence>
      <xsd:element name="OleDbDestination" type="OleDbDestination" minOccurs="0" maxOccurs="unbounded"/>
      <xsd:element name="FlatFileDestination" type="FlatFileDestination" minOccurs="0" maxOccurs="unbounded"/>
      <xsd:element name="ExcelDestination" type="ExcelDestination" minOccurs="0" maxOccurs="unbounded"/>
      <xsd:element name="SharePointDestination" type="SharePointDestination" minOccurs="0" maxOccurs="unbounded"/>
      <xsd:element name="AdoNetDestination" type="AdoNetDestination" minOccurs="0" maxOccurs="unbounded"/>
      <xsd:element name="OdbcDestination" type="OdbcDestination" minOccurs="0" maxOccurs="unbounded"/>
      <xsd:element name="SqlBulkDestination" type="SqlBulkDestination" minOccurs="0" maxOccurs="unbounded"/>
    </xsd:sequence>
	</xsd:complexType>


	<!-- Complex type for sources -->
	<xsd:complexType name="DataSource">
	 <xsd:choice>
	  <xsd:sequence>
	    <xsd:element name="OleDbSource" type="OleDbSource" minOccurs="0" maxOccurs="1"/>
	  </xsd:sequence>
	  <xsd:sequence>
        <xsd:element name="FlatFileSource" type="FlatFileSource" minOccurs="0" maxOccurs="1" />
      </xsd:sequence>
      <xsd:sequence>
        <xsd:element name="ExcelSource" type="ExcelSource" minOccurs="0" maxOccurs="1" />
      </xsd:sequence>
      <xsd:sequence>
        <xsd:element name="SharePointSource" type="SharePointSource" minOccurs="0" maxOccurs="1" />
      </xsd:sequence>
      <xsd:sequence>
        <xsd:element name="AdoNetSource" type="AdoNetSource" minOccurs="0" maxOccurs="1"/>
      </xsd:sequence>
      <xsd:sequence>
        <xsd:element name="OdbcSource" type="OdbcSource" minOccurs="0" maxOccurs="1"/>
      </xsd:sequence>
      <xsd:sequence>
        <xsd:element name="ODataSource" type="ODataSource" minOccurs="0" maxOccurs="1"/>
      </xsd:sequence>
    </xsd:choice>
  </xsd:complexType>


	<xsd:complexType name="MoveData">
		<xsd:sequence maxOccurs="1" minOccurs="1">
			<xsd:element name="DataSource" type="DataSource" maxOccurs="1" minOccurs="1" />
			<xsd:element name="DataDestination" type="DataDestination" minOccurs="1" maxOccurs="1" />
			<xsd:element name="StagingAreaRoot" type="xsd:string" minOccurs="0" maxOccurs="1"/>
      <xsd:element name="Partition" type="PartitionBlock" maxOccurs="1" minOccurs="0" />
      <xsd:element name="SavePackage" maxOccurs="1" minOccurs="0">
        <xsd:complexType>
          <xsd:simpleContent>
            <xsd:extension base="xsd:string">
              <xsd:attribute name="Save" type="xsd:boolean"  use="required"/>
              <xsd:attribute name="Load" type="xsd:boolean"  use="optional"/>
            </xsd:extension>
          </xsd:simpleContent>
        </xsd:complexType>
      </xsd:element>
    </xsd:sequence>
	</xsd:complexType>

  <xsd:complexType name="RunPackage">
    <xsd:sequence maxOccurs="1" minOccurs="1">
      <xsd:element name="File" type="xsd:string" maxOccurs="1" minOccurs="1" />
    </xsd:sequence>
  </xsd:complexType>

  <!-- ETL_Controller Header -->
  <xsd:complexType name="ETLHeader">
    <xsd:sequence maxOccurs="1" minOccurs="1">
      <xsd:element name="BatchID" type="xsd:int" maxOccurs="1" minOccurs="1" />
      <xsd:element name="StepID" type="xsd:int" maxOccurs="1" minOccurs="1" />
      <xsd:element name="RunID" type="xsd:int" maxOccurs="1" minOccurs="1" />
      <xsd:element name="Controller" type="DBConnection" maxOccurs="1" minOccurs="1" />
      <xsd:element name="Node" type="DBConnection" maxOccurs="1" minOccurs="0" />
      <xsd:element name="Conversation" type="GUID" maxOccurs="1" minOccurs="0" />
      <xsd:element name="ConversationGrp" type="GUID" maxOccurs="1" minOccurs="0" />
      <xsd:element name="Options" type="xsd:int" maxOccurs="1" minOccurs="0" />
    </xsd:sequence>
  </xsd:complexType>

  <!-- BIAS Workflow header -->
  <xsd:complexType name="BIASHeader">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence />
        <xsd:attribute name="RunID" type="xsd:int" use="required" />
        <xsd:attribute name="UserOptions" type="xsd:string" />
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>


  <!-- Parameters to be passed -->
	<xsd:complexType name="Parameters">
		<xsd:sequence minOccurs="1" maxOccurs="1">
      <xsd:choice>
        <xsd:sequence>
          <xsd:element name="ETLHeader" type="ETLHeader" maxOccurs="1" minOccurs="0" />
        </xsd:sequence>
        <xsd:sequence>
          <xsd:element name="BIASHeader" type="BIASHeader" maxOccurs="1" minOccurs="0" />
        </xsd:sequence>
      </xsd:choice>
      <xsd:choice>
				<xsd:sequence>
					<xsd:element name="MoveData" type="MoveData"  maxOccurs="1" minOccurs="0"/>
				</xsd:sequence>
        <xsd:sequence>
          <xsd:element name="RunPackage" type="RunPackage"  maxOccurs="1" minOccurs="0"/>
        </xsd:sequence>
      </xsd:choice>
    </xsd:sequence>
	</xsd:complexType>
	<xsd:element name="Parameters" type="Parameters" />
</xsd:schema>';

