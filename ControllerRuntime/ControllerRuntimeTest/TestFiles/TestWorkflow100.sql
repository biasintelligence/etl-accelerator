---////////////////////////////
--Controller Test Workflow
---///////////////////////////
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 100
set @BatchName = 'Test100' 

print ' Compiling Test100'

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
select @BatchID,@BatchName,@BatchName,25,25,0,1
set identity_insert dbo.ETLBatch off

-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'MAXTHREAD','4'
union all select @BatchID,'TIMEOUT','600'
union all select @BatchID,'LIFETIME','3600'
union all select @BatchID,'PING','30'
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
union all select @BatchID,'Event_Server'	,'isnull(dbo.fn_systemparameter(''Environment'',''EventServer'',''<ENV*>''),@@SERVERNAME)'
union all select @BatchID,'Event_Database'	,'isnull(dbo.fn_systemparameter(''Environment'',''EventDB'',''<ENV*>''),''ETL_Event'')'
union all select @BatchID,'Path'			,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'
union all select @BatchID,'WaitEvent'		,'isnull(dbo.fn_systemparameter(''Environment'',''WaitEvent'',''<ENV*>''),''Process1_FINISHED'')'
union all select @BatchID,'LocalServer'		,'@@SERVERNAME'
union all select @BatchID,'LocalDB'			,'db_name()'
union all select @BatchID,'Control.Server'	,'<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'Controller.ConnectionString','Server=<Control.Server>;Database=<Control.Database>;Trusted_Connection=True;Connection Timeout=30;'
union all select @BatchID,'DEPath'			,'<Path*>\DeltaExtractor\DeltaExtractor64.exe'
--union all select @BatchID,'DEPath'			,'C:\Users\andre_000\Documents\BIAS\BISolutionsAccelerator\BISolutionsAccelerator\1.3\Tools\DeltaExtractor\bin\Release\DeltaExtractor64.exe'
union all select @BatchID,'ActivityLocation','.\'
--WaitActivity
union all select @BatchID,'WaitTimeout'		,'10'
--operational
union all select @BatchID,'FlatFilesDir'	,'<Path*>\FlatFiles'
union all select @BatchID,'TestTableName'	,'dbo.TestTable'
union all select @BatchID,'TestConnectionString'	,'<Controller.ConnectionString>'
union all select @BatchID,'TestFileName'	,'TestTable'
union all select @BatchID,'TestFileNameExt'	,'.txt'


-------------------------------------------------------
--create batch level constraints
-------------------------------------------------------

set identity_insert dbo.ETLBatchConstraint on

insert dbo.ETLBatchConstraint
(ConstID,BatchID,ProcessID,ConstOrder,WaitPeriod)
		  select 1,@BatchID,22,'01',30
set identity_insert dbo.ETLBatchConstraint off

insert dbo.ETLBatchConstraintAttribute
(ConstID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'CheckFile','<FlatFilesDir>\test.txt'			
union all select 1,@BatchID,'PING','20'
union all select 1,@BatchID,'DISABLED','1'

-----------------------------------------------------
--create workflow steps 
-----------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
	(StepID,BatchID,StepName,StepDesc,StepProcID
	,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
select 1,@BatchID,'ST01','delete files',23,null,null,null,'01'
union all
select 2,@BatchID,'ST02','create table',20,null,null,null,'02'
union all
select 3,@BatchID,'ST03','parallel extract table to file',24,null,null,null,'03'
union all
select 4,@BatchID,'ST04','parallel extract table to file',24,null,null,null,'04'
union all
select 5,@BatchID,'ST05','loop wait',25,null,null,null,'05'
union all
select 6,@BatchID,'ST06','loop wait',25,null,null,null,'06'
union all
select 7,@BatchID,'ST07','loop wait',25,null,null,null,'07'
union all
select 8,@BatchID,'ST08','loop break',20,null,null,null,'08'

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)

		  select 1,@BatchID,'Console','powershell'
union all select 1,@BatchID,'Arg','remove-item <FlatFilesDir>\<TestFileName>*<TestFileNameExt>'
union all select 1,@BatchID,'Timeout','20'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'SEQGROUP','1'
union all select 1,@BatchID,'PRIGROUP','1'

union all select 2,@BatchID,'Query','
if (object_id(''<TestTableName>'',''U'') is null)
   create table <TestTableName> (id int, code nvarchar(10));
truncate table <TestTableName>;
insert <TestTableName> values
 (1,''test1'')
,(2,''test2'')
,(3,''test3'');
'
union all select 2,@BatchID,'ConnectionString','<TestConnectionString>'
union all select 2,@BatchID,'Timeout','20'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'SEQGROUP','1'
union all select 2,@BatchID,'PRIGROUP','1'

union all select 3,@BatchID,'Action','MoveData'
union all select 3,@BatchID,'Source.Component','ADONET'
union all select 3,@BatchID,'Source.ConnectionString','<TestConnectionString>'			
union all select 3,@BatchID,'Source.Query','select * from <TestTableName>'
union all select 3,@BatchID,'Destination.Component','FlatFile'
union all select 3,@BatchID,'Destination.Staging','0'
union all select 3,@BatchID,'Destination.FlatFile.ConnectionString','<FlatFilesDir>\<TestFileName>1<TestFileNameExt>'
union all select 3,@BatchID,'Destination.FlatFile.ColumnDelimiter',','--'\t'
union all select 3,@BatchID,'Destination.FlatFile.TextQualIfier','"'
union all select 3,@BatchID,'Timeout','120'
--union all select 3,@BatchID,'SavePackage','1'
--union all select 3,@BatchID,'PackageFileName','<Path*>\Test100_TableToFile1.dtsx'
union all select 3,@BatchID,'PRIGROUP','2'

union all select 4,@BatchID,'Action','MoveData'
union all select 4,@BatchID,'Source.Component','ADONET'
union all select 4,@BatchID,'Source.ConnectionString','<TestConnectionString>'			
union all select 4,@BatchID,'Source.Query','select * from <TestTableName>'
union all select 4,@BatchID,'Destination.Component','FlatFile'
union all select 4,@BatchID,'Destination.Staging','0'
union all select 4,@BatchID,'Destination.FlatFile.ConnectionString','<FlatFilesDir>\<TestFileName>2<TestFileNameExt>'
union all select 4,@BatchID,'Destination.FlatFile.ColumnDelimiter',','--'\t'
union all select 4,@BatchID,'Destination.FlatFile.TextQualIfier','"'
union all select 4,@BatchID,'Timeout','120'
--union all select 4,@BatchID,'SavePackage','1'
--union all select 4,@BatchID,'PackageFileName','<Path*>\Test100_TableToFile2.dtsx'
union all select 4,@BatchID,'PRIGROUP','2'

union all select 5,@BatchID,'WaitTimeout','10'
union all select 5,@BatchID,'DISABLED','0'
union all select 5,@BatchID,'PRIGROUP','3'
union all select 5,@BatchID,'LOOPGROUP','1'

union all select 6,@BatchID,'WaitTimeout','15'
union all select 6,@BatchID,'DISABLED','0'
union all select 6,@BatchID,'PRIGROUP','3'
union all select 6,@BatchID,'LOOPGROUP','1'

union all select 7,@BatchID,'WaitTimeout','20'
union all select 7,@BatchID,'DISABLED','0'
union all select 7,@BatchID,'PRIGROUP','3'
union all select 7,@BatchID,'LOOPGROUP','1'

--result should evalate to 0/1
union all select 8,@BatchID,'ConnectionString','<TestConnectionString>'
union all select 8,@BatchID,'Query','
declare @count int;
--check loop count for the LOOPGROUP 1
set @count = isnull(dbo.fn_ETLCounterGet (<@BatchID>,0,<@RunID>,''Loop_<etl:LoopGroup>''),0);
if (@count > 3)
	exec dbo.prc_ETLCounterSet <@BatchID>,0,<@RunID>,''BreakEvent_<etl:LoopGroup>'',''1'';
'

union all select 8,@BatchID,'DISABLED','0'
union all select 8,@BatchID,'PRIGROUP','4'
union all select 8,@BatchID,'LOOPGROUP','1'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
