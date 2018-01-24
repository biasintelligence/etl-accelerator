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
exec dbo.prc_createHeader @pHeader out,12,null,null,4,1,15
exec dbo.prc_CreateContext @pContext out,@pHeader
select @pContext
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'Call_BCP','debug;forcestart'
*/
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 12
set @BatchName = 'Call_BCP' 

print ' Compiling Call_BCP'
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

union all select @BatchID,'DropTestTable','
If exists (select * from sys.objects where object_id = object_id(N''dbo.BCPTest'') AND type in (N''U''))
   drop table dbo.BCPTest;'
union all select @BatchID,'SQL_ER','<DropTestTable>'

-------------------------------------------------------
--create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select  1,@BatchID,'ST01','Create test table',1,null,null,1,'01' 
union all select  2,@BatchID,'ST02','BCP out test table',8,null,null,1,'02' 
union all select  3,@BatchID,'ST03','Truncate test table',1,null,null,1,'03' 
union all select  4,@BatchID,'ST04','Verify test table is emtpy',1,null,null,1,'04' 
union all select  5,@BatchID,'ST05','BCP in test table',8,null,null,1,'05' 
union all select  6,@BatchID,'ST06','Verify test table is not empty',1,null,null,1,'06' 
union all select  7,@BatchID,'ST07','Drop test table is not empty',1,null,null,1,'07' 
union all select  8,@BatchID,'ST08','Drop test file',8,null,null,null,'08'

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
<DropTestTable>
CREATE TABLE dbo.BCPTest(Name char(5) NOT NULL,	[Desc] char(5) NOT NULL);
insert BCPTest
select 1,3
union all
select 3,5'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 2,@BatchID,'PRIGROUP','2'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'RETRY','1'
union all select 2,@BatchID,'DELAY','30'
union all select 2,@BatchID,'CONSOLE','bcp'
union all select 2,@BatchID,'Command_BCP','<LocalDB*>.dbo.BCPTest out "<Path*>\BCPTest.txt" -S <LocalServer*> -T -c'
union all select 2,@BatchID,'ARG','<Command_BCP>'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 3,@BatchID,'PRIGROUP','3'
union all select 3,@BatchID,'DISABLED','0'
union all select 3,@BatchID,'SQL','
truncate table dbo.BCPTest;'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 4,@BatchID,'PRIGROUP','4'
union all select 4,@BatchID,'DISABLED','0'
union all select 4,@BatchID,'SQL','
declare @RowCount int
select @RowCount = count(*) from dbo.BCPTest
if isNull(@RowCount,0) > 0
		RAISERROR (''Test table should be empty after truncate'', 11,17,@RowCount)'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 5,@BatchID,'PRIGROUP','5'
union all select 5,@BatchID,'DISABLED','0'
union all select 5,@BatchID,'RETRY','1'
union all select 5,@BatchID,'DELAY','30'
union all select 5,@BatchID,'CONSOLE','bcp'
union all select 5,@BatchID,'Command_BCP','<LocalDB*>.dbo.BCPTest in "<Path*>\BCPTest.txt" -S <LocalServer*> -T -c -b 10000 -h TABLOCK'
union all select 5,@BatchID,'ARG','<Command_BCP>'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 6,@BatchID,'PRIGROUP','6'
union all select 6,@BatchID,'DISABLED','0'
union all select 6,@BatchID,'SQL','
declare @RowCount int
select @RowCount = count(*) from dbo.BCPTest
if isNull(@RowCount,0) = 0
		RAISERROR (''Test should return 2 but it returned %i'', 11,17,@RowCount)'
		
		
-- execute SQL statement to create test tables on the local SQL Server only (ProcessID = 1)  
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 7,@BatchID,'PRIGROUP','7'
union all select 7,@BatchID,'DISABLED','0'
union all select 7,@BatchID,'SQL','<DropTestTable>'


-- Call Powershell cmdlet to drop test file
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 8,@BatchID,'PRIGROUP','7'
union all select 8,@BatchID,'DISABLED','0'
union all select 8,@BatchID,'CONSOLE','powershell.exe'
union all select 8,@BatchID,'ARG','Remove-Item <Path*>\BCPTest.txt'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
