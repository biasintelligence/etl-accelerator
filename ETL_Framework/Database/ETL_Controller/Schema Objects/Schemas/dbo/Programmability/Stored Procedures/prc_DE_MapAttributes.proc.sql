/*
--select * from ETLStepattribute where batchid = -15
declare @uid uniqueidentifier
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @pAttributes xml
--set @uid = newid()
exec dbo.prc_CreateHeader @pHeader out,-10,3,null,4,15
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext,@uid
exec dbo.prc_ReadContextAttributes @pProcessRequest,@pAttributes out
select @pAttributes
exec dbo.prc_DE_MapAttributes @pAttributes out
select @pAttributes
*/
CREATE PROCEDURE dbo.prc_DE_MapAttributes
    @pAttributes xml([ETLController]) output
As
/******************************************************************
**D File:         ETLController.SQL
**
**D Desc:         map ETL attributes to de expected 
**
**D Auth:         andreys
**D Date:         12/11/2007
**
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
2010-05-16           andrey@biasintelligence.com           change the mapping logic to allow for pattern mapping
                                        to support multiple destinations
2017-05-02			 andrey								   add OData source support

******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ProcErr INT
DECLARE @Cnt INT
DECLARE @ProcName sysname
DECLARE @msg nvarchar(max)
DECLARE @pr nvarchar(max)


SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0

begin try

declare @attr table (AttributeName nvarchar(100),AttributeValue nvarchar(max))
declare @map table (AttributeName sysname,inOverride sysname null,outOverride sysname null)


-------------------------------------------------------------------------
-- de required mapping
-------------------------------------------------------------------------
insert @map
--header
          select 'BatchID',null,'Control.BatchID'
union all select 'StepID',null,'Control.StepID'
union all select 'RunID',null,'Control.RunID'
union all select 'Conversation',null,'Control.Conversation'
union all select 'ConversationGrp',null,'Control.ConversationGrp'
union all select 'Options',null,'Control.Options'
union all select 'Control.Database',null,null
union all select 'Control.Server',null,null

--action MoveData,RunPackage
union all select 'Action',null,'Action'
--RunPackage
union all select 'Package.File',null,null

--MoveData
union all select 'Source.Server',null,null
union all select 'Source.Database',null,null
union all select 'Source.ConnectionString',null,null
union all select 'Source.ConnectionQualifier',null,null

--action MoveData
union all select 'Destination%.Server',null,null
union all select 'Destination%.Database',null,null
union all select 'Destination%.TableName',null,null
union all select 'Destination%.ConnectionString',null,null
union all select 'Destination%.ConnectionQualifier',null,null
union all select 'Destination%.DataTypeAutoConvert',null,null

--Partition Control
union all select 'Destination.PartitionFunction',null,null
union all select 'Destination.PartitionFunctionInput',null,null
union all select 'Destination.PartitionFunctionOutput',null,null
union all select 'Destination%.MinPartitionID',null,null
union all select 'Destination%.MaxPartitionID',null,null


--action MoveData
union all select 'Source.Component',null,null
union all select 'StagingAreaRoot',null,null
union all select 'Source.StagingAreaTable','StagingAreaTable','StagingAreaTableName'
union all select 'Source.Query',null,null
union all select 'Source.QueryType',null,null
union all select 'Source.CompessionType',null,null
--OLEDBSource
union all select 'Source.OLEDB.AccessMode',null,null
union all select 'Source.OLEDB.SqlCommand','OLEDB.SqlCommand','Query'
union all select 'Source.OLEDB.CommandTimeout',null,'QueryTimeout'
--FlatFileSource
union all select 'Source.FlatFile.ColumnDelimiter',null,null
union all select 'Source.FlatFile.RecordDelimiter',null,null
union all select 'Source.FlatFile.ConnectionString',null,null
union all select 'Source.FlatFile.TextQualifier',null,null
union all select 'Source.FlatFile.Format',null,null
union all select 'Source.FlatFile.Unicode',null,null
union all select 'Source.FlatFile.ColumnNamesInFirstDataRow',null,null
--SharePointSource
union all select 'Source.SPList.BatchSize',null,null
union all select 'Source.SPList.CamlQuery',null,null
union all select 'Source.SPList.IncludeFolders',null,null
union all select 'Source.SPList.IsRecursive',null,null
union all select 'Source.SPList.SharePointCulture',null,null
union all select 'Source.SPList.SiteListName',null,null
union all select 'Source.SPList.SiteListViewName',null,null
union all select 'Source.SPList.SiteUrl',null,null
union all select 'Source.SPList.DecodeLookupColumns',null,null
union all select 'Source.SPList.IncludeHiddenColumns',null,null
union all select 'Source.SPList.UseConnectionManager',null,null
--ExcelSource
union all select 'Source.Excel.FilePath',null,null
union all select 'Source.Excel.Header',null,null
union all select 'Source.Excel.ExcelVersion',null,null
union all select 'Source.Excel.AccessMode',null,null
union all select 'Source.Excel.OpenRowset','Excel.OpenRowset','TableName'
union all select 'Source.Excel.SqlCommand','Excel.SqlCommand','Query'
union all select 'Source.Excel.CommandTimeout',null,'QueryTimeout'
--AdoNetSource
union all select 'Source.ADONET.AccessMode',null,null
union all select 'Source.ADONET.SqlCommand','ADONET.SqlCommand','Query'
union all select 'Source.ADONET.CommandTimeout',null,'QueryTimeout'
union all select 'Source.ADONET.AllowImplicitStringConversion',null,null
--OdbcSource
union all select 'Source.ODBC.AccessMode',null,null
union all select 'Source.ODBC.SqlCommand','ODBC.SqlCommand','Query'
union all select 'Source.ODBC.CommandTimeout',null,'QueryTimeout'
union all select 'Source.ODBC.StatementTimeout',null,'QueryTimeout'
union all select 'Source.ODBC.BatchSize',null,null
union all select 'Source.ODBC.LobChunkSize',null,null
union all select 'Source.ODBC.ExposeCharColumnsAsUnicode',null,null
union all select 'Source.ODBC.FetchMethod',null,null
union all select 'Source.ODBC.DefaultCodePage',null,null
union all select 'Source.ODBC.BindNumericAs',null,null
union all select 'Source.ODBC.BindCharColumnsAs',null,null
--ODataSource
union all select 'Source.OData.DefaultStringLength',null,null
union all select 'Source.OData.CollectionName',null,null
union all select 'Source.OData.Query',null,null
union all select 'Source.OData.ResourcePath',null,null
union all select 'Source.OData.UseResourcePath',null,null

union all select 'Destination%.StagingAreaTable','StagingAreaTable','StagingAreaTableName'
union all select 'Destination%.Component',null,null
union all select 'Destination%.CompessionType',null,null
--FlatFileDestination
union all select 'Destination%.FlatFile.ColumnDelimiter',null,null
union all select 'Destination%.FlatFile.RecordDelimiter',null,null
union all select 'Destination%.FlatFile.ConnectionString',null,null
union all select 'Destination%.FlatFile.TextQualifier',null,null
union all select 'Destination%.FlatFile.Format',null,null
union all select 'Destination%.FlatFile.Unicode',null,null
union all select 'Destination%.FlatFile.Override',null,null
--OLEDBDestination
union all select 'Destination%.OLEDB.AccessMode',null,null
union all select 'Destination%.OLEDB.OpenRowset','OLEDB.OpenRowset','TableName'
union all select 'Destination%.OLEDB.TableName','OLEDB.TableName','TableName'
union all select 'Destination%.OLEDB.OpenRowsetVariable',null,null
union all select 'Destination%.OLEDB.FastLoadOptions',null,null
union all select 'Destination%.OLEDB.FastLoadKeepIdentity',null,null
union all select 'Destination%.OLEDB.FastLoadKeepNulls',null,null
union all select 'Destination%.OLEDB.FastLoadMaxInsertCommitSize',null,null
union all select 'Destination%.OLEDB.CommandTimeout',null,'QueryTimeout'
--SharepointDestination
union all select 'Destination%.SPList.BatchSize',null,null
union all select 'Destination%.SPList.CamlQuery',null,null
union all select 'Destination%.SPList.IncludeFolders',null,null
union all select 'Destination%.SPList.IsRecursive',null,null
union all select 'Destination%.SPList.SharePointCulture',null,null
union all select 'Destination%.SPList.SiteListName',null,null
union all select 'Destination%.SPList.SiteListViewName',null,null
union all select 'Destination%.SPList.SiteUrl',null,null
union all select 'Destination%.SPList.UseConnectionManager',null,null
--ExcelDestination
union all select 'Destination%.Excel.AccessMode',null,null
union all select 'Destination%.Excel.OpenRowset','Excel.OpenRowset','TableName'
union all select 'Destination%.Excel.CommandTimeout',null,'QueryTimeout'
union all select 'Destination%.Excel.FilePath',null,null
union all select 'Destination%.Excel.Header',null,null
union all select 'Destination%.Excel.ExcelVersion',null,null
--AdoNetDestination
union all select 'Destination%.ADONET.AccessMode',null,null
union all select 'Destination%.ADONET.TableOrViewName','ADONET.TableOrViewName','TableName'
union all select 'Destination%.ADONET.BatchSize',null,null
union all select 'Destination%.ADONET.UseBulkInsertWhenPossible',null,null
union all select 'Destination%.ADONET.CommandTimeout',null,'QueryTimeout'
--OdbcDestination
union all select 'Destination%.ODBC.InsertMethod',null,null
union all select 'Destination%.ODBC.BindCharColumnsAs',null,null
union all select 'Destination%.ODBC.BindNumericAs',null,null
union all select 'Destination%.ODBC.TableName','ODBC.TableName','TableName'
union all select 'Destination%.ODBC.BatchSize',null,null
union all select 'Destination%.ODBC.TransactionSize',null,null
union all select 'Destination%.ODBC.LobChunkSize',null,null
union all select 'Destination%.ODBC.CommandTimeout',null,'QueryTimeout'
union all select 'Destination%.ODBC.StatementTimeout',null,'QueryTimeout'
union all select 'Destination%.ODBC.DefaultCodePage',null,null
--SqlBulkDestination
union all select 'Destination%.SQLBULK.DefaultCodePage',null,null
union all select 'Destination%.SQLBULK.AlwaysUseDefaultCodePage',null,null
union all select 'Destination%.SQLBULK.BulkInsertTableName','SQLBULK.BulkInsertTableName','TableName'
union all select 'Destination%.SQLBULK.TableName','SQLBULK.TableName','TableName'
union all select 'Destination%.SQLBULK.BulkInsertCheckConstraints',null,null
union all select 'Destination%.SQLBULK.BulkInsertFirstRow',null,null
union all select 'Destination%.SQLBULK.BulkInsertFireTriggers',null,null
union all select 'Destination%.SQLBULK.BulkInsertKeepIdentity',null,null
union all select 'Destination%.SQLBULK.BulkInsertKeepNulls',null,null
union all select 'Destination%.SQLBULK.BulkInsertLastRow',null,null
union all select 'Destination%.SQLBULK.BulkInsertMaxErrors',null,null
union all select 'Destination%.SQLBULK.BulkInsertOrder',null,null
union all select 'Destination%.SQLBULK.BulkInsertTablock',null,null
union all select 'Destination%.SQLBULK.CommandTimeout',null,'QueryTimeout'
union all select 'Destination%.SQLBULK.Timeout',null,'QueryTimeout'
union all select 'Destination%.SQLBULK.MaxInsertCommitSize',null,null

--Packadge control
union all select 'SavePackage',null,null
union all select 'PackageFileName',null,null
union all select 'LoadPackage',null,null
--Staging control
union all select 'Destination%.Staging',null,null
union all select 'Destination%.StagingTableName',null,null
union all select 'Destination%.StagingTablePrepare',null,null
union all select 'Destination%.StagingTableUpload',null,null
union all select 'Destination%.UserOptions',null,null
--All
union all select 'QueryTimeout',null,null
union all select 'etl:Timeout',null,'QueryTimeout'
union all select 'UserOptions',null,null
union all select 'ForceStart',null,null
union all select 'Debug',null,null


;with xmlnamespaces('ETLController.XSD' as etl)
insert @attr
select b.b.value('./@Name[1]','nvarchar(100)'),b.b.value('(.)[1]','nvarchar(max)')
  from @pAttributes.nodes('/etl:Attributes') a(a)
  cross apply a.a.nodes('./etl:Attribute') b(b)

;with xmlnamespaces ('DeltaExtractor.XSD' as de, 'ETLController.XSD' as etl)
select @pAttributes = 
 (select case when m.outOverride is null then a.AttributeName
              else replace(a.AttributeName,isnull(m.inOverride,a.AttributeName),m.outOverride)
          end as '@Name',a.AttributeValue as '*'
    from @attr a
   left join @map m on a.AttributeName like m.AttributeName
     for xml path('etl:Attribute'),type, root('etl:Attributes'))

end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pAttributes = null
   raiserror (@msg,11,11)
end catch

RETURN @Err