set quoted_identifier on;
/*
-----------------------------------------------------------
--This code will return XML representation of this workflow
-----------------------------------------------------------
declare @pHeader xml
declare @pContext xml
exec dbo.prc_createHeader @pHeader out,11,null,null,4,1,15
exec dbo.prc_createContext @pContext out,@pHeader
select @pContext
*/
/*
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'SeqGroup','debug;forcestart'
*/

set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 11
set @BatchName = 'SeqGroup' 

print ' Compiling SeqGroup Batch'

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
select @BatchID,@BatchName,'SeqGroup',null,null,0,1
set identity_insert dbo.ETLBatch off

-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'TIMEOUT','120'
union all select @BatchID,'LIFETIME','3600'
union all select @BatchID,'MAXTHREAD','4'
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
union all select @BatchID,'Event_Server'	,'isnull(dbo.fn_systemparameter(''Environment'',''EventServer'',''<ENV*>''),@@SERVERNAME)'
union all select @BatchID,'Event_Database'	,'isnull(dbo.fn_systemparameter(''Environment'',''EventDB'',''<ENV*>''),''ETL_Event'')'
union all select @BatchID,'Path'			,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'

union all select @BatchID,'LocalServer','@@SERVERNAME'
union all select @BatchID,'LocalDB','db_name()'
union all select @BatchID,'Control.Server','<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'DEPath','<Path*>\DeltaExtractor\DeltaExtractor64.exe'

-------------------------------------------------------
--create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select 1,@BatchID,'ST01','Step 1',1,null,null,null,'01'
union all select 2,@BatchID,'ST02','Step 2',1,null,null,null,'02'
union all select 3,@BatchID,'ST03','Step 3',1,null,null,null,'03'
union all select 4,@BatchID,'ST04','Test sequence',1,null,null,null,'04' 

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------

-- execute SQL statement to create test tables on the local SQL Server only (ProcessID = 1)  
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)

		  select 1,@BatchID,'SQL','WAITFOR DELAY ''00:00:03'''
--union all select 1,@BatchID,'PING','10'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'SEQGROUP','1'


union all select 2,@BatchID,'SQL','WAITFOR DELAY ''00:00:03'''
--union all select 2,@BatchID,'PING','10'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'SEQGROUP','1'


union all select 3,@BatchID,'SQL','WAITFOR DELAY ''00:00:03'''
--union all select 3,@BatchID,'PING','10'
union all select 3,@BatchID,'DISABLED','0'
union all select 3,@BatchID,'SEQGROUP','1'


-- Test that step #1 completes before step #2 starts, and that step #2 completes before step #3 
-- because they are in the same SEQGROUP and their StepOrder = '01','02', and '03'.
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          --select 4,@BatchID,'PING','10'
--union all 
select 4,@BatchID,'DISABLED','0'
union all select 4,@BatchID,'SEQGROUP','1'
union all select 4,@BatchID,'SQL','
declare @Step1EndTime datetime,
  @Step2StartTime datetime,
  @Step2EndTime datetime,
  @Step3StartTime datetime

select @Step1EndTime = MAX(LogDT)
from ETLStepRunHistoryLog
where BatchId = <@BatchId>
and  RunID = <@RunId>
and  StepID = 1

select @Step2StartTime = MIN(LogDT), @Step2EndTime = MAX(LogDT)
from ETLStepRunHistoryLog
where BatchId = <@BatchId>
and  RunID = <@RunId>
and  StepID = 2

select @Step3StartTime = MIN(LogDT)
from ETLStepRunHistoryLog
where BatchId = <@BatchId>
and  RunID = <@RunId>
and  StepID = 3

if (isnull(@Step1EndTime,''1900-01-01'') >= isnull(@Step2StartTime,''1900-01-01''))
or (isnull(@Step2EndTime,''1900-01-01'') >= isnull(@Step3StartTime,''1900-01-01''))
   RAISERROR (''SEQGROUP error'', 11,17)'
   
   
end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go

