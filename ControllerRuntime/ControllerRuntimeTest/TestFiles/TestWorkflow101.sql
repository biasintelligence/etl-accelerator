---////////////////////////////
--Controller Test Workflow
---///////////////////////////
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 100
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
union all select @BatchID,'Inputdir'	,'<Path*>\ZipFiles'
union all select @BatchID,'SourcePattern'	,'*.txt.gz'
union all select @BatchID,'SourceName'	,'TestSource'
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
select 2,@BatchID,'ST011','processor 1 get workload',32,null,null,null,'011'
union all
select 3,@BatchID,'ST012','processor 1 upzip',29,null,null,null,'012'
union all
select 4,@BatchID,'ST021','processor 2 get workload',32,null,null,null,'021'
union all
select 5,@BatchID,'ST022','processor 2 upzip',29,null,null,null,'022'

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------
--insert dbo.ETLStepAttribute
--(StepID,BatchID,AttributeName,AttributeValue)



end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
