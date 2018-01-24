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
exec dbo.prc_createHeader @pHeader out,3,null,null,4,1,15
exec dbo.prc_CreateContext @pContext out,@pHeader
select @pContext
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'Call_SP','debug;forcestart'
*/

set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 3
set @BatchName = 'Call_SP' 

print ' Compiling Call_SP'
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
select @BatchID,@BatchName,'Test calling SPs',null,5,0,1
set identity_insert dbo.ETLBatch off

-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'MAXTHREAD','4'
union all select @BatchID,'TIMEOUT','180'
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

          select @BatchID,'ENV'				,'dbo.fn_systemparameter(''Environment'',''Current'',''ALL'')'
union all select @BatchID,'Dest_Server'		,'isnull(dbo.fn_systemparameter(''Environment'',''Dest_Server'',''<ENV*>''),@@SERVERNAME)'
union all select @BatchID,'Dest_DB'			,'isnull(dbo.fn_systemparameter(''Environment'',''Dest_DB'',''<ENV*>''),''Dest_DB'')'
union all select @BatchID,'Path'			,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'

union all select @BatchID,'LocalServer'		,'@@SERVERNAME'
union all select @BatchID,'LocalDB'			,'db_name()'
union all select @BatchID,'StagingAreaRoot'	,'<Path*>\DSV'
union all select @BatchID,'Control.Server'	,'<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'DEPath'			,'<Path*>\DeltaExtractor\DeltaExtractor64.exe'

union all select @BatchID,'Command_SQL'		,'-S <Dest_Server*> -d <Dest_DB*> -E -b -Q"<Query>"'
union all select @BatchID,'ARG'				,'<Command_SQL>'
union all select @BatchID,'CONSOLE'			,'sqlcmd'

union all select @BatchID,'SQL_ER','
If exists (select * from sys.objects where object_id = object_id(N''dbo.TestRowCounts'') AND type in (N''U''))
   drop table dbo.TestRowCounts;
If exists (select name from sys.databases where name = N''Dest_DB'')
begin
	ALTER DATABASE Dest_DB
	SET SINGLE_USER
	WITH ROLLBACK IMMEDIATE;
	drop DATABASE Dest_DB;
end'


-------------------------------------------------------
--create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select  1,@BatchID,'ST01','Create Dest DB',1,null,null,1,'01' 
union all select  2,@BatchID,'ST02','Create test table on local server',1,null,null,null,'02'   
union all select  3,@BatchID,'ST03','Create test table on dest server',8,null,null,null,'03'   
union all select  4,@BatchID,'ST04','Create test SP on dest server',8,null,null,null,'04'   
union all select  5,@BatchID,'ST05','Call test SP on dest server',8,null,null,null,'05'
union all select  6,@BatchID,'ST06','Get rowcount from test table on dest server',7,null,null,null,'06'
union all select  7,@BatchID,'ST07','Test that test table rowcount = 1',1,null,null,null,'07'
union all select  8,@BatchID,'ST08','Drop test table on local server',1,null,null,null,'08'   
union all select  9,@BatchID,'ST10','Drop Dest DB',1,null,null,null,'09' 


set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','1'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'SQL','
If not exists (select name from sys.databases where name = N''Dest_DB'')
	create database <Dest_DB*>;'


-- execute SQL statement to create test tables on the local SQL Server only (ProcessID = 1)  
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 2,@BatchID,'PRIGROUP','1'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'SQL','
If exists (select * from sys.objects where object_id = object_id(N''dbo.TestRowCounts'') AND type in (N''U''))
   drop table dbo.TestRowCounts;
create table dbo.TestRowCounts(ObjectName varchar(100) not NULL, RowCnt int not NULL);'


-- create test table
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 3,@BatchID,'PRIGROUP','2'
union all select 3,@BatchID,'DISABLED','0'
union all select 3,@BatchID,'Query','
If exists (select * from sys.objects where object_id = object_id(N''dbo.TestTable'') AND type in (N''U''))
   drop table dbo.TestTable;
create table dbo.TestTable(BatchID int NOT NULL, RunId int NOT NULL, RunTime DateTime);'


-- execute test SP
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 4,@BatchID,'PRIGROUP','3'
union all select 4,@BatchID,'DISABLED','0'
union all select 4,@BatchID,'Query','
If  EXISTS (select * FROM sys.objects where object_id = OBJECT_ID(N''[dbo].[Insert_TestTable]'') AND type in (N''P'', N''PC''))
drop procedure [dbo].[Insert_TestTable];
GO
create procedure [dbo].[Insert_TestTable] (@BatchId INT = 0,@RunId   INT = 0,@RunTime datetime)
AS 
BEGIN
    SET NOCOUNT ON 
	DECLARE @err INT 
	DECLARE @rows INT 
	DECLARE @msg NVARCHAR(1000) 
	DECLARE @proc SYSNAME 
	DECLARE @tran INT 
	SET @proc = OBJECT_NAME(@@PROCID) 
	SET @tran = @@TRANCOUNT  
BEGIN TRY
Insert TestTable
select @BatchID, @RunId, @RunTime
select @rows = @@ROWCOUNT, @err = @@ERROR
END TRY
      BEGIN CATCH 
		SET @proc = OBJECT_NAME(@@PROCID) ;
		SET @Msg = ''ERROR: PROC '' + @proc + '', MSG: '' + ERROR_MESSAGE();
        RAISERROR (@msg,11,17)
      END CATCH 
END'


-- call test table
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 5,@BatchID,'PRIGROUP','4'
union all select 5,@BatchID,'DISABLED','0'
union all select 5,@BatchID,'Date','getdate()'
union all select 5,@BatchID,'Query','exec dbo.Insert_TestTable <@BatchID>,<@RunID>,''<Date*>'''


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 6,@BatchID,'PRIGROUP','5'
union all select 6,@BatchID,'DISABLED','0'
union all select 6,@BatchID,'Source.Component','OLEDB'
union all select 6,@BatchID,'Source.Server','<Dest_Server*>'		
union all select 6,@BatchID,'Source.Database','<Dest_DB*>'
union all select 6,@BatchID,'Source.Query',
'select ''TestTable'' as ObjectName, count(*) as RowCnt from dbo.TestTable where BatchId = <@BatchID> and RunId = <@RunID>'
union all select 6,@BatchID,'Destination.Component','OLEDB'
union all select 6,@BatchID,'Destination.tableName','dbo.TestRowCounts'
union all select 6,@BatchID,'Destination.Server','<Control.Server>'	
union all select 6,@BatchID,'Destination.Database','<Control.Database>'
union all select 6,@BatchID,'Destination.Staging','0'
union all select 6,@BatchID,'Destination.OLEDB.AccessMode','OpenRowset'
union all select 6,@BatchID,'Action','MoveData'
union all select 6,@BatchID,'RETRY','0'
union all select 6,@BatchID,'DELAY','30'
union all select 6,@BatchID,'RESTART','1'
--union all select 5,@BatchID,'SavePackage','1'
--union all select 5,@BatchID,'PackageFileName','C:\Users\aaa\Step5.dtsx'
--union all select 5,@BatchID,'ForceStart','1'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 7,@BatchID,'PRIGROUP','6'
union all select 7,@BatchID,'DISABLED','0'
union all select 7,@BatchID,'SQL','
declare @RowCount int
select @RowCount = count(*) from dbo.TestRowCounts
where ObjectName = ''TestTable''
if (@RowCount <> 1)
		RAISERROR (''Rowcount should = 1, but it = %i'', 11,17,@RowCount)'


-- execute SQL statement to create test tables on the local SQL Server only (ProcessID = 1)  
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 8,@BatchID,'PRIGROUP','6'
union all select 8,@BatchID,'DISABLED','0'
union all select 8,@BatchID,'SQL','
If exists (select * from sys.objects where object_id = object_id(N''dbo.TestRowCounts'') AND type in (N''U''))
   drop table dbo.TestRowCounts;'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 9,@BatchID,'PRIGROUP','7'
union all select 9,@BatchID,'DISABLED','0'
union all select 9,@BatchID,'SQL','
If exists (select name from sys.databases where name = N''Dest_DB'')
begin
	ALTER DATABASE Dest_DB
	SET SINGLE_USER
	WITH ROLLBACK IMMEDIATE;
	drop DATABASE Dest_DB;
end'
	
	
end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
