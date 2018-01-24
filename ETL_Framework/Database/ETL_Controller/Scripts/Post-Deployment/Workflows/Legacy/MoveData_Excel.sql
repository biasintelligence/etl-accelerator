

set quoted_identIfier on;
---////////////////////////////
--Controller Test Workflow
---///////////////////////////
/*
-----------------------------------------------------------
--This code will return XML representation of this workflow
-----------------------------------------------------------
declare @pHeader xml
declare @pContext xml
exec dbo.prc_createHeader @pHeader out,10,null,null,4,1,15

--exec dbo.prc_createHeader @pHeader out,3,null,null,4,1,15

exec dbo.prc_CreateContext @pContext out,@pHeader
select @pContext
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'MoveData_Excel','debug;forcestart'
*/

set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 10
set @BatchName = 'MoveData_Excel' 

print ' Compiling MoveData_Excel'
begin try
-------------------------------------------------------
--remove Workflow metadata
-------------------------------------------------------
exec dbo.prc_RemoveContext @BatchName

-------------------------------------------------------
--create workflow record 
-------------------------------------------------------
set identity_insert dbo.ETLBatch on
insert dbo.ETLBatch
(BatchID,BatchName,BatchDesc
,OnSuccessID,OnFailureID,IgnoreErr,RestartOnErr
)
select @BatchID,@BatchName,'Test Get data from Excel',null,5,0,1

set identity_insert dbo.ETLBatch off

-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'MAXTHREAD','4'
union all select @BatchID,'TIMEOUT','120'
union all select @BatchID,'LIFETIME','3600'
union all select @BatchID,'PING','20'
union all select @BatchID,'HISTRET','100'
union all select @BatchID,'RETRY','0'
union all select @BatchID,'DELAY','10'

-------------------------------------------------------
--Define workflow level user attributes
-- use systemparameters to store global configuration parameters
-- select * from systemparameters
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)

          select @BatchID,'ENV'					,'dbo.fn_systemparameter(''Environment'',''Current'',''ALL'')'
union all select @BatchID,'Path'				,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'
union all select @BatchID,'LocalServer'			,'@@SERVERNAME'
union all select @BatchID,'LocalDB'				,'db_name()'
union all select @BatchID,'Control.Server'		,'<LocalServer*>'
union all select @BatchID,'Control.Database'	,'<LocalDB*>'
union all select @BatchID,'DEPath'				,'<Path*>\DeltaExtractor\DeltaExtractor32.exe'

union all select @BatchID,'DropTestTables','
If exists (select * from sys.objects where object_id = object_id(N''dbo.WithHeaders'') AND type in (N''U''))
   drop table dbo.WithHeaders;
If exists (select * from sys.objects where object_id = object_id(N''dbo.WithOutHeaders'') AND type in (N''U''))
   drop table dbo.WithOutHeaders;'
union all select @BatchID,'SQL_ER','<DropTestTables>'

-------------------------------------------------------
--create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select  1,@BatchID,'ST01','Create test tables',1,null,null,1,'01' 
union all select  2,@BatchID,'ST02','MoveData to Excel file with headers',7,null,null,1,'02' 
union all select  3,@BatchID,'ST03','MoveData to Excel file without headers',7,null,null,1,'03' 
union all select  4,@BatchID,'ST04','MoveData from Excel file with headers',7,null,null,1,'04' 
union all select  5,@BatchID,'ST05','MoveData from Excel file without headers',7,null,null,1,'05' 
union all select  6,@BatchID,'ST06','Test with headers',1,null,null,1,'06' 
union all select  7,@BatchID,'ST07','Test without headers',1,null,null,1,'07' 
union all select  8,@BatchID,'ST08','Drop test tables',1,null,null,1,'08' 

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------

-- create test table
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','1'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'SQL','
<DropTestTables>
create table dbo.WithHeaders(BatchID int  NULL,BatchName varchar(30) NULL);
create table dbo.WithOutHeaders(F1 int  NULL,F2 varchar(30) NULL);'

 
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 2,@BatchID,'PRIGROUP'							,'2'
union all select 2,@BatchID,'DISABLED'							,'0'
union all select 2,@BatchID,'Action'							,'MoveData'

union all select 2,@BatchID,'Source.Component'					,'OLEDB'
union all select 2,@BatchID,'Source.AccessMode'					,'OpenRowset Using FastLoad'
union all select 2,@BatchID,'Source.Server'						,'<LocalServer*>'
union all select 2,@BatchID,'Source.Database'					,'<LocalDB*>'
union all select 2,@BatchID,'Source.QueryType'					,'SQL'
union all select 2,@BatchID,'Source.Query'						,'Select BatchId, BatchName from ETLBatch'

union all select 2,@BatchID,'Destination.Component'				,'Excel'
union all select 2,@BatchID,'Destination.Excel.Header'			,'1'
union all select 2,@BatchID,'Destination.Excel.FilePath'		,'<Path*>\ExcelBatchTest.xls'			
union all select 2,@BatchID,'Destination.Excel.ExcelVersion'	,'Microsoft Excel 2007'
union all select 2,@BatchID,'Destination.Excel.AccessMode'		,'OpenRowset'
union all select 2,@BatchID,'Destination.Excel.OpenRowset'		,'WithHeaders$'					
union all select 2,@BatchID,'Destination.Excel.CommandTimeout'	,'0'

--union all select 2,@BatchID,'SavePackage'						,'1'
--union all select 2,@BatchID,'PackageFileName'					,'C:\Builds\DSTS_Packages\WithHeaders.dtsx'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 3,@BatchID,'PRIGROUP'							,'3'
union all select 3,@BatchID,'DISABLED'							,'0'
union all select 3,@BatchID,'Action'							,'MoveData'

union all select 3,@BatchID,'Source.Component'					,'OLEDB'
union all select 3,@BatchID,'Source.AccessMode'					,'OpenRowset Using FastLoad'
union all select 3,@BatchID,'Source.Server'						,'<LocalServer*>'
union all select 3,@BatchID,'Source.Database'					,'<LocalDB*>'
union all select 3,@BatchID,'Source.QueryType'					,'SQL'
union all select 3,@BatchID,'Source.Query'						,'Select BatchId as F1, BatchName as F2 from ETLBatch'

union all select 3,@BatchID,'Destination.Component'				,'Excel'
union all select 3,@BatchID,'Destination.Excel.Header'			,'0'
union all select 3,@BatchID,'Destination.Excel.FilePath'		,'<Path*>\ExcelBatchTest.xls'			
union all select 3,@BatchID,'Destination.Excel.ExcelVersion'	,'Microsoft Excel 2007'
union all select 3,@BatchID,'Destination.Excel.AccessMode'		,'OpenRowset'					
union all select 3,@BatchID,'Destination.Excel.OpenRowset'		,'WithOutHeaders$'					
union all select 3,@BatchID,'Destination.Excel.CommandTimeout'	,'0'

--union all select 3,@BatchID,'SavePackage'						,'1'
--union all select 3,@BatchID,'PackageFileName'					,'C:\Builds\DSTS_Packages\WithOutHeaders.dtsx'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 4,@BatchID,'PRIGROUP'							,'4'
union all select 4,@BatchID,'DISABLED'							,'0'
union all select 4,@BatchID,'Action'							,'MoveData'
union all select 4,@BatchID,'Source.Component'					,'Excel'
union all select 4,@BatchID,'Source.Excel.Header'				,'1'
union all select 4,@BatchID,'Source.Excel.FilePath'				,'<Path*>\ExcelBatchTest.xls' 
union all select 4,@BatchID,'Source.Excel.ExcelVersion'			,'Microsoft Excel 2007'
union all select 4,@BatchID,'Source.Excel.AccessMode'			,'OpenRowset'						
union all select 4,@BatchID,'Source.Excel.OpenRowset'			,'WithHeaders$'
union all select 4,@BatchID,'Source.Excel.CommandTimeout'		,'0'
union all select 4,@BatchID,'Destination.Component'				,'OLEDB'
union all select 4,@BatchID,'Destination.AccessMode'			,'OpenRowset Using FastLoad'
union all select 4,@BatchID,'Destination.TableName'				,'dbo.WithHeaders'
union all select 4,@BatchID,'Destination.Server'				,'<LocalServer*>'
union all select 4,@BatchID,'Destination.Database'				,'<LocalDB*>'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 5,@BatchID,'PRIGROUP'							,'5'
union all select 5,@BatchID,'DISABLED'							,'0'
union all select 5,@BatchID,'Action'							,'MoveData'
union all select 5,@BatchID,'Source.Component'					,'Excel'
union all select 5,@BatchID,'Source.Excel.Header'				,'0'
union all select 5,@BatchID,'Source.Excel.FilePath'				,'<Path*>\ExcelBatchTest.xls' 
union all select 5,@BatchID,'Source.Excel.ExcelVersion'			,'Microsoft Excel 2007'
union all select 5,@BatchID,'Source.Excel.AccessMode'			,'OpenRowset'						
union all select 5,@BatchID,'Source.Excel.OpenRowset'			,'WithOutHeaders$'
union all select 5,@BatchID,'Source.Excel.CommandTimeout'		,'0'
union all select 5,@BatchID,'Destination.Component'				,'OLEDB'
union all select 5,@BatchID,'Destination.AccessMode'			,'OpenRowset Using FastLoad'
union all select 5,@BatchID,'Destination.TableName'				,'dbo.WithOutHeaders'
union all select 5,@BatchID,'Destination.Server'				,'<LocalServer*>'
union all select 5,@BatchID,'Destination.Database'				,'<LocalDB*>'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 6,@BatchID,'PRIGROUP','6'
union all select 6,@BatchID,'DISABLED','0'
union all select 6,@BatchID,'SQL','
if (select count(*) from ETLBatch) <> (select count(*) from ETLBatch b join WithHeaders wh on b.BatchID = wh.BatchId)
	RAISERROR (''MoveData with Excel file with headers failed'', 11,11)'
		
		
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 7,@BatchID,'PRIGROUP','6'
union all select 7,@BatchID,'DISABLED','0'
union all select 7,@BatchID,'SQL','
if (select count(*) from ETLBatch) <> (select count(*) from ETLBatch b join WithOutHeaders wh on b.BatchID = wh.F1)
	RAISERROR (''MoveData with Excel file without headers failed'', 11,11)'
	

-- drop test table
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 8,@BatchID,'PRIGROUP','7'
union all select 8,@BatchID,'DISABLED','0'
union all select 8,@BatchID,'SQL','<DropTestTables>'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
