/*
--select * from ETLStepattribute where batchid = -1
--select * from ETLBatch where batchid = -15
declare @uid uniqueidentifier
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @pParameters xml--(ETLClient_DE)
--set @uid = newid()
exec dbo.prc_CreateHeader @pHeader out,-10,12,null,4,10
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext,@uid
exec dbo.prc_de_CreateParameters @pParameters out, @pProcessRequest
--select @pHeader
select @pContext
--select @pProcessRequest
select @pParameters
*/
CREATE PROCEDURE [dbo].[prc_de_CreateParameters]
    @pParameters xml([ETLClient_DE]) output
   --@pParameters xml output
   ,@pProcessRequest xml([ETLController])
As
/******************************************************************
**D File:         prc_de_CreateParameters.SQL
**
**D Desc:         return delta extractor parameters object
**
**D Auth:         andreys
**D Date:         12/11/2007
**
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
2010-05-16           andrey@biasintelligence.com           add multiple destination support
2017-05-02			 andrey								   add OData source support
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ProcErr INT
DECLARE @Cnt INT
DECLARE @ProcName sysname
DECLARE @msg nvarchar(max)
DECLARE @pr nvarchar(max)

DECLARE @Header xml (ETLController)
DECLARE @Context xml (ETLController)
DECLARE @Attributes xml (ETLController)
DECLARE @Handle uniqueidentifier
DECLARE @HandleGrp uniqueidentifier
DECLARE @BatchID int
DECLARE @StepID int
DECLARE @RunID int
DECLARE @Options int
DECLARE @Scope int

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0

begin try

exec @ProcErr = dbo.[prc_ReadProcessRequest] @pProcessRequest,@Header out,@Context out,@Handle out,@HandleGrp out
exec @ProcErr = dbo.[prc_ReadHeader] @Header,@BatchID out,@StepID out,null,@RunID out,@Options out,@Scope out
exec @ProcErr = dbo.[prc_ReadContextAttributes] @pProcessRequest,@Attributes out
exec @ProcErr = dbo.prc_DE_MapAttributes @Attributes out
--select @Attributes

declare @attr table (AttributeName nvarchar(100),AttributeValue nvarchar(max))
;with xmlnamespaces('ETLController.XSD' as etl)
insert @attr
select b.b.value('./@Name[1]','nvarchar(100)'),b.b.value('(.)[1]','nvarchar(max)')
  from @Attributes.nodes('/etl:Attributes') a(a)
  cross apply a.a.nodes('./etl:Attribute') b(b)


declare @dst table (DestinationName nvarchar(100),DestinationType nvarchar(100))
insert @dst
select replace(AttributeName,'.Component',''),AttributeValue
  from @attr where AttributeName like 'Destination%.Component'


--;with xmlnamespaces ('DeltaExtractor.XSD' as de,'ETLController.XSD' as etl)
;with xmlnamespaces ('DeltaExtractor.XSD' as de)
select @pParameters = 
--header
 (select @BatchID as 'de:ETLHeader/de:BatchID',@StepID as 'de:ETLHeader/de:StepID',@RunID as 'de:ETLHeader/de:RunID'
,isnull((select top 1 AttributeValue from @attr where AttributeName = 'Control.Server'),@@SERVERNAME) as 'de:ETLHeader/de:Controller/de:Server'
,isnull((select top 1 AttributeValue from @attr where AttributeName = 'Control.Database'),DB_NAME()) as 'de:ETLHeader/de:Controller/de:Database'
,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:ETLHeader/de:Controller/de:QueryTimeout'
,@@SERVERNAME as 'de:ETLHeader/de:Node/de:Server'
,DB_NAME() as 'de:ETLHeader/de:Node/de:Database'
,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:ETLHeader/de:Node/de:QueryTimeout'
,@handle as 'de:ETLHeader/de:Conversation',@Options as 'de:ETLHeader/de:Options'
--
,case (select AttributeValue from @attr where AttributeName = 'Action')
 when 'MoveData'
 then
    (select
--define source
--supported OLEDB, FlatFile, Excel, Sharepoint, AdoNet, ODBC
    case when (isnull((select top 1 AttributeValue from @attr where AttributeName = 'Source.Component' and AttributeValue = 'OLEDB'),'') <> '')
      then
--OLEDB source
    (select
   (select top 1 AttributeValue from @attr where AttributeName = 'Source.Server') as 'de:OleDbSource/de:DBConnection/de:Server'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Database') as 'de:OleDbSource/de:DBConnection/de:Database'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ConnectionString') as 'de:OleDbSource/de:DBConnection/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:OleDbSource/de:DBConnection/de:QueryTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.QueryType') as 'de:OleDbSource/de:QueryType'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.OLEDB.AccessMode') as 'de:OleDbSource/de:CustomProperties/de:AccessMode'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Query') as 'de:OleDbSource/de:CustomProperties/de:SqlCommand'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:OleDbSource/de:CustomProperties/de:CommandTimeout'
  for xml path('de:DataSource'),type)
--FlatFile source  
      when (isnull((select top 1 AttributeValue from @attr where AttributeName = 'Source.Component' and AttributeValue = 'FlatFile'),'') <> '')
      then
    (select
   (select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.CodePage') as 'de:FlatFileSource/de:CustomProperties/de:CodePage'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.ConnectionString') as 'de:FlatFileSource/de:CustomProperties/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.DataRowsToSkip') as 'de:FlatFileSource/de:CustomProperties/de:DataRowsToSkip'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.Format') as 'de:FlatFileSource/de:CustomProperties/de:Format'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.ColumnNamesInFirstDataRow') as 'de:FlatFileSource/de:CustomProperties/de:ColumnNamesInFirstDataRow'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.HeaderRowDelimiter') as 'de:FlatFileSource/de:CustomProperties/de:HeaderRowDelimiter'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.HeaderRowsToSkip') as 'de:FlatFileSource/de:CustomProperties/de:HeaderRowsToSkip'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.TextQualifier') as 'de:FlatFileSource/de:CustomProperties/de:TextQualifier'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.Unicode') as 'de:FlatFileSource/de:CustomProperties/de:Unicode'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.ColumnDelimiter') as 'de:FlatFileSource/de:CustomProperties/de:ColumnDelimiter'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.RecordDelimiter') as 'de:FlatFileSource/de:CustomProperties/de:RecordDelimiter'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.FileNameColumnName') as 'de:FlatFileSource/de:CustomProperties/de:FileNameColumnName'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.FlatFile.RetainNulls') as 'de:FlatFileSource/de:CustomProperties/de:RetainNulls'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.CompressionType') as 'de:FlatFileSource/de:CustomProperties/de:DataCompression'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.StagingAreaTableName') as 'de:FlatFileSource/de:CustomProperties/de:StagingAreaTableName'
  for xml path('de:DataSource'),type)
--Sharepoint source  
      when (isnull((select top 1 AttributeValue from @attr where AttributeName = 'Source.Component' and AttributeValue = 'SPList'),'') <> '')
      then
    (select
   (select top 1 AttributeValue from @attr where AttributeName = 'Source.ConnectionString') as 'de:SharePointSource/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.BatchSize') as 'de:SharePointSource/de:CustomProperties/de:BatchSize'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.CamlQuery') as 'de:SharePointSource/de:CustomProperties/de:CamlQuery'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.IncludeFolders') as 'de:SharePointSource/de:CustomProperties/de:IncludeFolders'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.IsRecursive') as 'de:SharePointSource/de:CustomProperties/de:IsRecursive'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.SharePointCulture') as 'de:SharePointSource/de:CustomProperties/de:SharePointCulture'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.SiteListName') as 'de:SharePointSource/de:CustomProperties/de:SiteListName'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.SiteListViewName') as 'de:SharePointSource/de:CustomProperties/de:SiteListViewName'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.SiteUrl') as 'de:SharePointSource/de:CustomProperties/de:SiteUrl'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.DecodeLookupColumns') as 'de:SharePointSource/de:CustomProperties/de:DecodeLookupColumns'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.IncludeHiddenColumns') as 'de:SharePointSource/de:CustomProperties/de:IncludeHiddenColumns'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.SPList.UseConnectionManager') as 'de:SharePointSource/de:CustomProperties/de:UseConnectionManager'
  for xml path('de:DataSource'),type)
--Excel source
      when (isnull((select top 1 AttributeValue from @attr where AttributeName = 'Source.Component' and AttributeValue = 'Excel'),'') <> '')
      then
    (select
   (select top 1 AttributeValue from @attr where AttributeName = 'Source.ConnectionString') as 'de:ExcelSource/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Excel.FilePath') as 'de:ExcelSource/de:FilePath'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Excel.Header') as 'de:ExcelSource/de:Header'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Excel.ExcelVersion') as 'de:ExcelSource/de:ExcelVersion'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Excel.AccessMode') as 'de:ExcelSource/de:CustomProperties/de:AccessMode'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Excel.OpenRowset') as 'de:ExcelSource/de:CustomProperties/de:OpenRowset'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Excel.SqlCommand') as 'de:ExcelSource/de:CustomProperties/de:SqlCommand'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:ExcelSource/de:CustomProperties/de:CommandTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.CompressionType') as 'de:ExcelSource/de:DataCompression'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.StagingAreaTableName') as 'de:ExcelSource/de:StagingAreaTableName'
  for xml path('de:DataSource'),type)
--AdoNet source
    when (isnull((select top 1 AttributeValue from @attr where AttributeName = 'Source.Component' and AttributeValue = 'ADONET'),'') <> '')
      then
    (select
   (select top 1 AttributeValue from @attr where AttributeName = 'Source.Server') as 'de:AdoNetSource/de:DBConnection/de:Server'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Database') as 'de:AdoNetSource/de:DBConnection/de:Database'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ConnectionString') as 'de:AdoNetSource/de:DBConnection/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ConnectionQualifier') as 'de:AdoNetSource/de:DBConnection/de:Qualifier'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:AdoNetSource/de:DBConnection/de:QueryTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ADONET.AccessMode') as 'de:AdoNetSource/de:CustomProperties/de:AccessMode'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Query') as 'de:AdoNetSource/de:CustomProperties/de:SqlCommand'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:AdoNetSource/de:CustomProperties/de:CommandTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ADONET.AllowImplicitStringConversion') as 'de:AdoNetSource/de:CustomProperties/de:AllowImplicitStringConversion'
  for xml path('de:DataSource'),type)
--Odbc source
    when (isnull((select top 1 AttributeValue from @attr where AttributeName = 'Source.Component' and AttributeValue = 'ODBC'),'') <> '')
      then
    (select
   (select top 1 AttributeValue from @attr where AttributeName = 'Source.Server') as 'de:OdbcSource/de:DBConnection/de:Server'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Database') as 'de:OdbcSource/de:DBConnection/de:Database'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ConnectionString') as 'de:OdbcSource/de:DBConnection/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ConnectionQualifier') as 'de:OdbcSource/de:DBConnection/de:Qualifier'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:OdbcSource/de:DBConnection/de:QueryTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ODBC.AccessMode') as 'de:OdbcSource/de:CustomProperties/de:AccessMode'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.Query') as 'de:OdbcSource/de:CustomProperties/de:SqlCommand'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:OdbcSource/de:CustomProperties/de:StatementTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ODBC.BatchSize') as 'de:OdbcSource/de:CustomProperties/de:BatchSize'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ODBC.LobChunkSize') as 'de:OdbcSource/de:CustomProperties/de:LobChunkSize'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ODBC.ExposeCharColumnsAsUnicode') as 'de:OdbcSource/de:CustomProperties/de:ExposeCharColumnsAsUnicode'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ODBC.FetchMethod') as 'de:OdbcSource/de:CustomProperties/de:FetchMethod'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ODBC.DefaultCodePage') as 'de:OdbcSource/de:CustomProperties/de:DefaultCodePage'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ODBC.BindNumericAs') as 'de:OdbcSource/de:CustomProperties/de:BindNumericAs'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.ODBC.BindCharColumnsAs') as 'de:OdbcSource/de:CustomProperties/de:BindCharColumnsAs'
  for xml path('de:DataSource'),type)
--OData source  
      when (isnull((select top 1 AttributeValue from @attr where AttributeName = 'Source.Component' and AttributeValue = 'ODATA'),'') <> '')
      then
    (select
   (select top 1 AttributeValue from @attr where AttributeName = 'Source.ConnectionString') as 'de:ODataSource/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.OData.DefaultStringLength') as 'de:ODataSource/de:CustomProperties/de:DefaultStringLength'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.OData.CollectionName') as 'de:ODataSource/de:CustomProperties/de:CollectionName'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.OData.Query') as 'de:ODataSource/de:CustomProperties/de:Query'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.OData.ResourcePath') as 'de:ODataSource/de:CustomProperties/de:ResourcePath'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Source.OData.UseResourcePath') as 'de:ODataSource/de:CustomProperties/de:UseResourcePath'
  for xml path('de:DataSource'),type)
      end

--define destinations
  ,(select
-- OLEDB destination
    (select
   (select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Server') as 'de:DBConnection/de:Server'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Database') as 'de:DBConnection/de:Database'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ConnectionString') as 'de:DBConnection/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:DBConnection/de:QueryTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Staging') as 'de:StagingBlock/@Staging'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTableName') as 'de:StagingBlock/de:StagingTableName'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTablePrepare') as 'de:StagingBlock/de:StagingTablePrepare'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTableUpload') as 'de:StagingBlock/de:StagingTableUpload'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.UserOptions') as 'de:StagingBlock/de:UserOptions'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MinPartitionID') as 'de:PartitionRange/@Min'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MaxPartitionID') as 'de:PartitionRange/@Max'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.OLEDB.AccessMode') as 'de:CustomProperties/de:AccessMode'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.TableName') as 'de:CustomProperties/de:OpenRowset'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.OLEDB.FastLoadOptions') as 'de:CustomProperties/de:FastLoadOptions'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.OLEDB.FastLoadKeepIdentity') as 'de:CustomProperties/de:FastLoadKeepIdentity'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.OLEDB.FastLoadKeepNulls') as 'de:CustomProperties/de:FastLoadKeepNulls'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.OLEDB.FastLoadMaxInsertCommitSize') as 'de:CustomProperties/de:FastLoadMaxInsertCommitSize'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:CustomProperties/de:CommandTimeout'
  from @dst dst where dst.DestinationType = 'OLEDB'
  for xml path('de:OleDbDestination'),type)
--FlatFile destination 
   ,(select 
   (select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MinPartitionID') as 'de:PartitionRange/@Min'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MaxPartitionID') as 'de:PartitionRange/@Max'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.CodePage') as 'de:CustomProperties/de:CodePage'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.ConnectionString') as 'de:CustomProperties/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.DataRowsToSkip') as 'de:CustomProperties/de:DataRowsToSkip'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.Format') as 'de:CustomProperties/de:Format'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.ColumnNamesInFirstDataRow') as 'de:CustomProperties/de:ColumnNamesInFirstDataRow'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.HeaderRowDelimiter') as 'de:CustomProperties/de:HeaderRowDelimiter'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.TextQualifier') as 'de:CustomProperties/de:TextQualifier'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.Unicode') as 'de:CustomProperties/de:Unicode'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.ColumnDelimiter') as 'de:CustomProperties/de:ColumnDelimiter'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.RecordDelimiter') as 'de:CustomProperties/de:RecordDelimiter'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.Header') as 'de:CustomProperties/de:Header'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.FlatFile.Override') as 'de:CustomProperties/de:Override'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.CompressionType') as 'de:CustomProperties/de:DataCompression'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingAreaTableName') as 'de:CustomProperties/de:StagingAreaTableName'
  from @dst dst where dst.DestinationType = 'FlatFile'
  for xml path('de:FlatFileDestination'),type)
--Sharepoint destination 
   ,(select 
   (select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ConnectionString') as 'de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SPList.BatchSize') as 'de:CustomProperties/de:BatchSize'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SPList.CamlQuery') as 'de:CustomProperties/de:CamlQuery'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SPList.IncludeFolders') as 'de:CustomProperties/de:IncludeFolders'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SPList.IsRecursive') as 'de:CustomProperties/de:IsRecursive'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SPList.SharePointCulture') as 'de:CustomProperties/de:SharePointCulture'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SPList.SiteListName') as 'de:CustomProperties/de:SiteListName'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SPList.SiteListViewName') as 'de:CustomProperties/de:SiteListViewName'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SPList.SiteUrl') as 'de:CustomProperties/de:SiteUrl'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SPList.UseConnectionManager') as 'de:CustomProperties/de:UseConnectionManager'
  from @dst dst where dst.DestinationType = 'SPList'
  for xml path('de:SharePointDestination'),type)
-- Excel destination
   ,(select
   (select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ConnectionString') as 'de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Excel.FilePath') as 'de:FilePath'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Excel.Header') as 'de:Header'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Excel.ExcelVersion') as 'de:ExcelVersion'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MinPartitionID') as 'de:PartitionRange/@Min'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MaxPartitionID') as 'de:PartitionRange/@Max'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Excel.AccessMode') as 'de:CustomProperties/de:AccessMode'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Excel.OpenRowset') as 'de:CustomProperties/de:OpenRowset'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:CustomProperties/de:CommandTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.CompressionType') as 'de:CustomProperties/de:DataCompression'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingAreaTableName') as 'de:CustomProperties/de:StagingAreaTableName'
  from @dst dst where dst.DestinationType = 'Excel'
  for xml path('de:ExcelDestination'),type)
-- ADONET destination
   ,(select
   (select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Server') as 'de:DBConnection/de:Server'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Database') as 'de:DBConnection/de:Database'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ConnectionString') as 'de:DBConnection/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ConnectionQualifier') as 'de:DBConnection/de:Qualifier'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:DBConnection/de:QueryTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Staging') as 'de:StagingBlock/@Staging'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTableName') as 'de:StagingBlock/de:StagingTableName'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTablePrepare') as 'de:StagingBlock/de:StagingTablePrepare'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTableUpload') as 'de:StagingBlock/de:StagingTableUpload'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.UserOptions') as 'de:StagingBlock/de:UserOptions'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MinPartitionID') as 'de:PartitionRange/@Min'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MaxPartitionID') as 'de:PartitionRange/@Max'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ADONET.AccessMode') as 'de:CustomProperties/de:AccessMode'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.TableName') as 'de:CustomProperties/de:TableOrViewName'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ADONET.BatchSize') as 'de:CustomProperties/de:BatchSize'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ADONET.UseBulkInsertWhenPossible') as 'de:CustomProperties/de:UseBulkInsertWhenPossible'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:CustomProperties/de:CommandTimeout'
  from @dst dst where dst.DestinationType = 'ADONET'
  for xml path('de:AdoNetDestination'),type)
-- ODBC destination
   ,(select
   (select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Server') as 'de:DBConnection/de:Server'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Database') as 'de:DBConnection/de:Database'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ConnectionString') as 'de:DBConnection/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ConnectionQualifier') as 'de:DBConnection/de:Qualifier'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:DBConnection/de:QueryTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Staging') as 'de:StagingBlock/@Staging'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTableName') as 'de:StagingBlock/de:StagingTableName'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTablePrepare') as 'de:StagingBlock/de:StagingTablePrepare'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTableUpload') as 'de:StagingBlock/de:StagingTableUpload'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.UserOptions') as 'de:StagingBlock/de:UserOptions'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MinPartitionID') as 'de:PartitionRange/@Min'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MaxPartitionID') as 'de:PartitionRange/@Max'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ODBC.InsertMethod') as 'de:CustomProperties/de:InsertMethod'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ODBC.BindCharColumnsAs') as 'de:CustomProperties/de:BindCharColumnsAs'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ODBC.BindNumericAs') as 'de:CustomProperties/de:BindNumericAs'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.TableName') as 'de:CustomProperties/de:TableName'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ODBC.BatchSize') as 'de:CustomProperties/de:BatchSize'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ODBC.TransactionSize') as 'de:CustomProperties/de:TransactionSize'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ODBC.LobChunkSize') as 'de:CustomProperties/de:LobChunkSize'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:CustomProperties/de:StatementTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ODBC.DefaultCodePage') as 'de:CustomProperties/de:DefaultCodePage'
  from @dst dst where dst.DestinationType = 'ODBC'
  for xml path('de:OdbcDestination'),type)
-- SqlBulk destination
   ,(select
   (select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Server') as 'de:DBConnection/de:Server'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Database') as 'de:DBConnection/de:Database'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.ConnectionString') as 'de:DBConnection/de:ConnectionString'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:DBConnection/de:QueryTimeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.Staging') as 'de:StagingBlock/@Staging'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTableName') as 'de:StagingBlock/de:StagingTableName'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTablePrepare') as 'de:StagingBlock/de:StagingTablePrepare'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.StagingTableUpload') as 'de:StagingBlock/de:StagingTableUpload'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.UserOptions') as 'de:StagingBlock/de:UserOptions'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MinPartitionID') as 'de:PartitionRange/@Min'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.MaxPartitionID') as 'de:PartitionRange/@Max'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.DefaultCodePage') as 'de:CustomProperties/de:DefaultCodePage'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.AlwaysUseDefaultCodePage') as 'de:CustomProperties/de:AlwaysUseDefaultCodePage'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.TableName') as 'de:CustomProperties/de:BulkInsertTableName'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.BulkInsertCheckConstraints') as 'de:CustomProperties/de:BulkInsertCheckConstraints'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.BulkInsertFirstRow') as 'de:CustomProperties/de:BulkInsertFirstRow'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.BulkInsertFireTriggers') as 'de:CustomProperties/de:BulkInsertFireTriggers'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.BulkInsertKeepIdentity') as 'de:CustomProperties/de:BulkInsertKeepIdentity'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.BulkInsertKeepNulls') as 'de:CustomProperties/de:BulkInsertKeepNulls'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.BulkInsertLastRow') as 'de:CustomProperties/de:BulkInsertLastRow'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.BulkInsertMaxErrors') as 'de:CustomProperties/de:BulkInsertMaxErrors'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.BulkInsertOrder') as 'de:CustomProperties/de:BulkInsertOrder'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.BulkInsertTablock') as 'de:CustomProperties/de:BulkInsertTablock'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'QueryTimeout') as 'de:CustomProperties/de:Timeout'
  ,(select top 1 AttributeValue from @attr where AttributeName = dst.DestinationName + '.SQLBULK.MaxInsertCommitSize') as 'de:CustomProperties/de:MaxInsertCommitSize'
  from @dst dst where dst.DestinationType = 'SQLBULK'
  for xml path('de:SqlBulkDestination'),type)
  for xml path('de:DataDestination'),type)
  
  ,(select top 1 AttributeValue from @attr where AttributeName = 'StagingAreaRoot') as 'de:StagingAreaRoot'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Destination.PartitionFunction') as 'de:Partition/@Function'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Destination.PartitionFunctionInput') as 'de:Partition/de:Input'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'Destination.PartitionFunctionOutput') as 'de:Partition/de:Output'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'SavePackage') as 'de:SavePackage/@Save'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'LoadPackage') as 'de:SavePackage/@Load'
  ,(select top 1 AttributeValue from @attr where AttributeName = 'PackageFileName') as 'de:SavePackage'
  for xml path('de:MoveData'),type)
 when 'RunPackage'
 then (select
  (select top 1 AttributeValue from @attr where AttributeName = 'Package.File') as 'de:File'
  for xml path('de:RunPackage'),type)
end
 for xml path(''),type, root('de:Parameters'))


end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pParameters = null
   raiserror (@msg,11,11)
end catch

RETURN @Err