---////////////////////////////
--Mongo file processor Workflow
---///////////////////////////
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 1001
set @BatchName = 'Mongo' + cast(@BatchId as nvarchar(30)); 

print ' Compiling ' + @BatchName;

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
--those attributes can be referenced in wf body like <etl:MaxThread> etc.
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
union all select @BatchID,'GetFileId'	,'isnull(dbo.fn_etlCounterGet(<@batchID>,<@StepId>,<@runID>,''fileId''),''0'')'
union all select @BatchID,'Workload_1_workingDir'	,'' --placeholder for workload 1 working dir

union all select @BatchID,'InputDir'	,'<Path*>\ZipFiles'
union all select @BatchID,'SourcePrefix'	,'mongobackup'
union all select @BatchID,'SourceExt'	,'.tar.gz'
union all select @BatchID,'SourcePattern'	,'<SourcePrefix>_*<SourceExt>'
union all select @BatchID,'SourceName'	,'<SourcePrefix>_TGZ'
union all select @BatchID,'UnzipDir'	,'<Path*>\TempFiles'
union all select @BatchID,'OutputDir'	,'<Path*>\OutputMongoFiles'


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
select 1,@BatchID,'ST01','check and register new files',31,null,null,null,'010'
union all
select 2,@BatchID,'ST011','processor 1 get workload',32,35,34,null,'011'
union all
select 3,@BatchID,'ST012','processor 1 decompress',29,null,34,null,'012'
union all
select 4,@BatchID,'ST013','processor 1 load staging edxapp fs.chunks ',30,null,null,null,'013'
union all
select 5,@BatchID,'ST014','processor 1 load staging edxapp fs.files ',30,null,null,null,'014'
union all
select 6,@BatchID,'ST015','processor 1 load staging edxapp modulestore.active_versions ',30,null,null,null,'015'
union all
select 7,@BatchID,'ST016','processor 1 load staging edxapp modulestore.definitions ',30,null,null,null,'016'
union all
select 8,@BatchID,'ST017','processor 1 load staging edxapp modulestore.structures ',30,null,null,null,'017'
union all
select 9,@BatchID,'ST018','processor 1 finalize ',33,null,null,null,'018'
union all
select 100,@BatchID,'ST100','workflow exit',20,null,null,null,'100'


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
union all select 1,@BatchID,'PRIGROUP','01'

--Workload 1 processor
union all select 2,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 2,@BatchId,'RegisterConnectionString','<Staging.ConnectionString>'
union all select 2,@BatchId,'Timeout','30'
union all select 2,@BatchId,'UnzipStepId','3' --workload unzip step
union all select 2,@BatchId,'FinalizeStepId','9' --workload finalize step
--on success skip workload steps and break loop if no more work found
--or configure workload steps for new work
union all select 2,@BatchID,'OnSuccessQuery','
declare @fid nvarchar(30) = <GetFileId>;
if (''0'' = @fid)
begin
	--skip all workload steps
	exec dbo.prc_ETLCounterSet <@BatchID>,3,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,4,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,5,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,6,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,7,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,8,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,9,<@RunID>,''ExitEvent'',''2'';
	exec dbo.prc_ETLCounterSet <@BatchID>,0,<@RunID>,''BreakEvent_<etl:LoopGroup>'',''1'';
end
else
begin
	declare @fn nvarchar(100) = isnull(dbo.fn_ETLCounterGet (<@BatchID>,<@StepId>,<@RunID>,''filename_'' + @fid),'''')
	declare @dir nvarchar(100) = replace(right(@fn,len(@fn) - charindex(''<SourcePrefix>'',@fn) + 1),''<SourceExt>'','''')
	exec dbo.prc_ETLAttributeSet <@BatchID>,<UnzipStepId>,null,''FileId'',@fid;
	exec dbo.prc_ETLAttributeSet <@BatchID>,<UnzipStepId>,null,''InputFile'',@fn;
	exec dbo.prc_ETLAttributeSet <@BatchID>,<FinalizeStepId>,null,''FileId'',@fid;
	exec dbo.prc_ETLAttributeSet <@BatchID>,null,null,''workload_1_WorkingDir'',@dir;
end
'
--on failure
union all select 2,@BatchId,'fileId','<GetFileId*>' -- get fileId for this workload
union all select 2,@BatchId,'OnFailureStatus','Failed' --set progressStatus for this fileId to failed
--control
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'SEQGROUP','1'
union all select 2,@BatchID,'PRIGROUP','02'
union all select 2,@BatchID,'LOOPGROUP','1'

--upzip for workload 1
union all select 3,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 3,@BatchId,'RegisterConnectionString','<Staging.ConnectionString>'
union all select 3,@BatchId,'InputFile','' -- file name for this workload
union all select 3,@BatchId,'OutputFolder','<UnzipDir>' --destination folder

--union all select 3,@BatchId,'Timeout','300'
--on success/failure
union all select 3,@BatchId,'FileId','' -- workload fileId
union all select 3,@BatchId,'OnFailureStatus','Failed'
union all select 3,@BatchId,'OnSuccessStatus','Completed'

union all select 3,@BatchID,'DISABLED','0'
--union all select 3,@BatchID,'SEQGROUP','1'
union all select 3,@BatchID,'PRIGROUP','021'
union all select 3,@BatchID,'LOOPGROUP','1'

--load edxapp fs.chunks
union all select 4,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 4,@BatchId,'InputFile','<UnzipDir>\<Workload_1_workingDir>\edxapp\fs.chunks.bson' -- bson converter input
union all select 4,@BatchId,'OutputFolder','<OutputDir>\<Workload_1_workingDir>' --destination folder

union all select 4,@BatchID,'DISABLED','0'
union all select 4,@BatchID,'PRIGROUP','03'
union all select 4,@BatchID,'LOOPGROUP','1'

--load edxapp fs.files
union all select 5,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 5,@BatchId,'InputFile','<UnzipDir>\<Workload_1_workingDir>\edxapp\fs.files.bson' -- bson converter input
union all select 5,@BatchId,'OutputFolder','<OutputDir>\<Workload_1_workingDir>' --destination folder

union all select 5,@BatchID,'DISABLED','0'
union all select 5,@BatchID,'PRIGROUP','03'
union all select 5,@BatchID,'LOOPGROUP','1'

--load edxapp modulestore.active_versions
union all select 6,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 6,@BatchId,'InputFile','<UnzipDir>\<Workload_1_workingDir>\edxapp\modulestore.active_versions.bson' -- bson converter input
union all select 6,@BatchId,'OutputFolder','<OutputDir>\<Workload_1_workingDir>' --destination folder

union all select 6,@BatchID,'DISABLED','0'
union all select 6,@BatchID,'PRIGROUP','03'
union all select 6,@BatchID,'LOOPGROUP','1'

--load edxapp modulestore.definitions
union all select 7,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 7,@BatchId,'InputFile','<UnzipDir>\<Workload_1_workingDir>\edxapp\modulestore.definitions.bson' -- bson converter input
union all select 7,@BatchId,'OutputFolder','<OutputDir>\<Workload_1_workingDir>' --destination folder

union all select 7,@BatchID,'DISABLED','0'
union all select 7,@BatchID,'PRIGROUP','03'
union all select 7,@BatchID,'LOOPGROUP','1'

--load edxapp modulestore.structures
union all select 8,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 8,@BatchId,'InputFile','<UnzipDir>\<Workload_1_workingDir>\edxapp\modulestore.structures.bson' -- bson converter input
union all select 8,@BatchId,'OutputFolder','<OutputDir>\<Workload_1_workingDir>' --destination folder

union all select 8,@BatchID,'DISABLED','0'
union all select 8,@BatchID,'PRIGROUP','03'
union all select 8,@BatchID,'LOOPGROUP','1'

--finalize load
union all select 9,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 9,@BatchId,'RegisterConnectionString','<Staging.ConnectionString>'
union all select 9,@BatchId,'FileId','' --workload fileId
union all select 9,@BatchId,'FileStatus','Completed'

union all select 9,@BatchId,'Timeout','30'
union all select 9,@BatchID,'DISABLED','0'
union all select 9,@BatchID,'PRIGROUP','04'
union all select 9,@BatchID,'LOOPGROUP','1'


--workflow exit
union all select 100,@BatchId,'ConnectionString','<Controller.ConnectionString>'
union all select 100,@BatchId,'Query','
exec prc_ApplicationLog @pMessage = ''Workflow is completed'',@pErr = 0, @pBatchId = <@BatchId>,@pStepId = <@StepId>,@pRunId=<@RunId>;
'
union all select 100,@BatchId,'Timeout','30'
union all select 100,@BatchID,'DISABLED','0'
union all select 100,@BatchID,'PRIGROUP','99'



end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
