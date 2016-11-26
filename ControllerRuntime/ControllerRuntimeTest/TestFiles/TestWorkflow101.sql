---////////////////////////////
--Controller Test Workflow
---///////////////////////////
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 101
set @BatchName = 'Test101' 

print ' Compiling Test101'

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
select @BatchID,@BatchName,@BatchName,null,null,0,1
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
union all select @BatchID,'Staging.ConnectionString','Server=<Control.Server>;Database=ETL_Staging;Trusted_Connection=True;Connection Timeout=30;'
union all select @BatchID,'ActivityLocation','.\'
--operational
union all select @BatchID,'StepId','0'
union all select @BatchID,'GetFileId'	,'isnull(dbo.fn_etlCounterGet(<@batchID>,<StepId>,<@runID>,''fileId''),''0'')'
union all select @BatchID,'GetFileName'	,'isnull(dbo.fn_etlCounterGet(<etl:batchId>,<StepId>,<etl:runId>,''fileName_<getFileId*>''),'''')'
union all select @BatchID,'CheckWorkloadStatus','
if (0 = isnull(dbo.fn_ETLCounterGet (<@BatchID>,<StepId>,<@RunID>,''fileId''),0))
begin
	exec dbo.prc_ETLCounterSet <@BatchID>,0,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,0,<@RunID>,''BreakEvent'',<etl:LoopGroup>;
end
'



union all select @BatchID,'Inputdir'	,'<Path*>\ZipFiles'
union all select @BatchID,'SourcePattern'	,'test*.tar.gz'
union all select @BatchID,'SourceName'	,'TestSource_TGZ'
union all select @BatchID,'OutputDir'	,'<Path*>\UnzipFiles'


-------------------------------------------------------
--create batch level constraints
-------------------------------------------------------

-----------------------------------------------------
--create workflow steps 
-----------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
	(StepID,BatchID,StepName,StepDesc,StepProcID
	,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
select 1,@BatchID,'ST01','register files',31,null,null,null,'01'
union all
select 2,@BatchID,'ST011','processor 1 get workload',32,20,34,null,'011'
union all
select 3,@BatchID,'ST012','processor 1 upzip',29,33,34,null,'012'

union all
select 10,@BatchID,'ST021','processor 2 get workload',32,20,34,null,'021'
union all
select 11,@BatchID,'ST022','processor 2 upzip',29,33,34,null,'022'

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--Check for new files
		  select 1,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 1,@BatchId,'RegisterConnectionString','<Staging.ConnectionString>'
union all select 1,@BatchId,'RegisterPath','<Inputdir>\<SourcePattern>'
union all select 1,@BatchId,'ProcessPriority','0'
union all select 1,@BatchId,'Timeout','30'

union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'PRIGROUP','1'

--workload 1 loop
union all select 2,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 2,@BatchId,'RegisterConnectionString','<Staging.ConnectionString>'
union all select 2,@BatchId,'Timeout','30'
union all select 2,@BatchId,'StepId','<@StepId>' --this step
--on success skip workload steps and break loop if no more work found
union all select 2,@BatchID,'Query','
if (0 = isnull(dbo.fn_ETLCounterGet (<@BatchID>,<@StepId>,<@RunID>,''fileId''),0))
begin
	exec dbo.prc_ETLCounterSet <@BatchID>,3,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,0,<@RunID>,''BreakEvent_<etl:LoopGroup>'',''1'';
end
'
--on failure
union all select 2,@BatchId,'FileId','<GetFileId*>' -- get fileId for this workload
union all select 2,@BatchId,'OnFailureStatus','Failed' --set progressStatus for this fileId to failed
--control
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'SEQGROUP','1'
union all select 2,@BatchID,'PRIGROUP','2'
union all select 2,@BatchID,'LOOPGROUP','1'

--upzip and finish workload 1
union all select 3,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 3,@BatchId,'RegisterConnectionString','<Staging.ConnectionString>'
union all select 3,@BatchId,'StepId','2' --where to find the file to process
union all select 3,@BatchId,'InputFile','<GetFileName*>' -- get the file name for this workload
union all select 3,@BatchId,'OutputFolder','<OutputDir>' --destination folder
union all select 3,@BatchId,'Timeout','30'
--on success/failure
union all select 3,@BatchId,'FileId','<GetFileId*>' -- get the fileId being processed
union all select 3,@BatchId,'OnFailureStatus','Failed'
union all select 3,@BatchId,'OnSuccessStatus','Completed'

union all select 3,@BatchID,'DISABLED','0'
union all select 3,@BatchID,'SEQGROUP','1'
union all select 3,@BatchID,'PRIGROUP','2'
union all select 3,@BatchID,'LOOPGROUP','1'

--workload 2 loop
union all select 10,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 10,@BatchId,'RegisterConnectionString','<Staging.ConnectionString>'
union all select 10,@BatchId,'Timeout','30'
union all select 10,@BatchId,'StepId','<@StepId>' --this step
--on success check workload 2 and break loop
--on success skip workload steps and break loop if no more work found
union all select 10,@BatchID,'Query','
if (0 = isnull(dbo.fn_ETLCounterGet (<@BatchID>,<@StepId>,<@RunID>,''fileId''),0))
begin
	exec dbo.prc_ETLCounterSet <@BatchID>,11,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,0,<@RunID>,''BreakEvent_<etl:LoopGroup>'',''1'';
end
'
--on failure
union all select 10,@BatchId,'FileId','<GetFileId*>'
union all select 10,@BatchId,'OnFailureStatus','Failed'

union all select 10,@BatchID,'DISABLED','0'
union all select 10,@BatchID,'SEQGROUP','2'
union all select 10,@BatchID,'PRIGROUP','2'
union all select 10,@BatchID,'LOOPGROUP','2'

--unzip and finish workload 2
union all select 11,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 11,@BatchId,'RegisterConnectionString','<Staging.ConnectionString>'
union all select 11,@BatchId,'StepId','10'
union all select 11,@BatchId,'InputFile','<GetFileName*>'
union all select 11,@BatchId,'OutputFolder','<OutputDir>'
union all select 11,@BatchId,'Timeout','30'
--on success/failure
union all select 11,@BatchId,'FileId','<GetFileId*>'
union all select 11,@BatchId,'OnFailureStatus','Failed'
union all select 11,@BatchId,'OnSuccessStatus','Completed'

union all select 11,@BatchID,'DISABLED','0'
union all select 11,@BatchID,'SEQGROUP','2'
union all select 11,@BatchID,'PRIGROUP','2'
union all select 11,@BatchID,'LOOPGROUP','2'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
