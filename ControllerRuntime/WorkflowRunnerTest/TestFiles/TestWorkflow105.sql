---////////////////////////////
--Controller Test Workflow
---///////////////////////////
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 105
set @BatchName = 'Test105' 

print ' Compiling Test105'

begin try
-------------------------------------------------------
--remove Workflow metadata
-------------------------------------------------------
exec dbo.prc_RemoveContext @BatchName

-------------------------------------------------------
--create workflow record 
-------------------------------------------------------
--set identity_insert dbo.ETLBatch on
insert dbo.ETLBatch
(BatchID,BatchName,BatchDesc
,OnSuccessID,OnFailureID,IgnoreErr,RestartOnErr
)
select @BatchID,@BatchName,@BatchName,null,null,0,1
--set identity_insert dbo.ETLBatch off

-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'etl:Maxthread','4'
union all select @BatchID,'etl:Timeout','600'
union all select @BatchID,'etl:Lifetime','3600'
union all select @BatchID,'etl:Ping','30'
union all select @BatchID,'etl:Histret','100'
union all select @BatchID,'etl:Retry','0'
union all select @BatchID,'etl:Delay','10'

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
union all select @BatchID,'ActivityLocation','.\'

union all select @BatchID,'CounterName','TestCounter'


-----------------------------------------------------
--create workflow steps 
-----------------------------------------------------
--set identity_insert dbo.ETLStep on

insert dbo.ETLStep
	(StepID,BatchID,StepName,StepDesc,StepProcID
	,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
select 1,@BatchID,'ST01','set counters',20,null,null,null,'01'
union all
select 2,@BatchID,'ST02','processId = 0',0,35,null,null,'02'

--set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)

		  select 1,@BatchID,'ConnectionString','<Controller.ConnectionString>'
union all select 1,@BatchID,'Query','exec dbo.prc_etlcounterset <etl:batchId>,<etl:stepId>,<etl:runId>,''<CounterName>'',''100'''
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'PRIGROUP','1'
union all select 1,@BatchID,'SEQGROUP','1'

union all select 2,@BatchID,'MyCounter','<ctr:TestCounter:1>'
union all select 2,@BatchID,'OnSuccessConnectionString','<Controller.ConnectionString>'
union all select 2,@BatchID,'OnSuccessQuery','
declare @val int = isnull((select count(*) from dbo.etlstep where batchId = <MyCounter>),0);
exec dbo.prc_etlcounterset <etl:batchId>,<etl:stepId>,<etl:runId>,''<CounterName>'',@val;'

union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'PRIGROUP','1'
union all select 2,@BatchID,'SEQGROUP','1'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
