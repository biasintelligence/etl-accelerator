--select * from etl_event..EventType et
--join etl_event..Event e
--on et.EventTypeID = e.EventTypeID
--select * from ETLProcess
--select * from ETLStepRunHistory where BatchID = 2
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
exec dbo.prc_CreateHeader @pHeader out,2,null,null,4,1,15
exec dbo.prc_CreateContext @pContext out,@pHeader
select @pContext

-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'Process2','debug;forcestart'
*/


set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 2
set @BatchName = 'Process2' 

print ' Compiling Process2'

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
select @BatchID,@BatchName,'Process2',3,null,0,1
set identity_insert dbo.ETLBatch off


-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'MAXTHREAD','4'
union all select @BatchID,'TIMEOUT','600'
union all select @BatchID,'LIFETIME','3600'
union all select @BatchID,'PING','60'
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
union all select @BatchID,'Max_RunTime'		,'isnull(dbo.fn_systemparameter(''Environment'',''Max_RunTime'',''<ENV*>''),''23:50'')'
union all select @BatchID,'LocalServer'		,'@@SERVERNAME'
union all select @BatchID,'LocalDB'			,'db_name()'
union all select @BatchID,'Control.Server'	,'<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'DEPath'			,'<Path*>\DeltaExtractor\DeltaExtractor64.exe'

--Post Event Process2_FINISHED on Batch success (processid = 3)
union all select @BatchID,'date','convert(nchar(8),getdate(),112)'
union all select @BatchID,'SQL_01','
declare @eventargs xml
;with xmlnamespaces(''EventArgs.XSD'' as dwc)
select @eventargs = 
(select ''Process2 Finished'' as ''@Source'',''Week'' as ''@PeriodGrain'',''<date*>'' as ''@Period''
for xml path(''dwc:EventArgs''),type)
exec dbo.prc_ClrEventPost ''<LocalServer*>'',''<Event_Database*>'',''Process2_FINISHED'',null,@eventargs,''debug'''

-------------------------------------------------------
--create batch level constraints
-------------------------------------------------------

set identity_insert dbo.ETLBatchConstraint on

insert dbo.ETLBatchConstraint
(ConstID,BatchID,ProcessID,ConstOrder,WaitPeriod)
		  select 1,@BatchID,100,'01',0
union all select 2,@BatchID,101,'02',1
		  
set identity_insert dbo.ETLBatchConstraint off

--ImportBatchConstraintAttribute
insert dbo.ETLBatchConstraintAttribute
(ConstID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'MaxRunTm','<Max_RunTime*>'			
union all select 1,@BatchID,'PING','20'
union all select 1,@BatchID,'DISABLED','0'


insert dbo.ETLBatchConstraintAttribute
(ConstID,BatchID,AttributeName,AttributeValue)
		  select 2,@BatchID,'WatermarkEventType','Process2_FINISHED'
union all select 2,@BatchID,'EventType','<WaitEvent*>'		
union all select 2,@BatchID,'EventServer','<Event_Server*>'
union all select 2,@BatchID,'EventDatabase','<Event_Database*>'
union all select 2,@BatchID,'PING','20'
union all select 2,@BatchID,'DISABLED','0'


-----------------------------------------------------
--create workflow steps 
-----------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
	(StepID,BatchID,StepName,StepDesc,StepProcID
	,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
select 1,@BatchID,'ST01','Placeholder step',1,null,null,null,'01'

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)

		  select 1,@BatchID,'SQL','SELECT 1+1'	--  Normally this step would actually do something
union all select 1,@BatchID,'PING','20'
union all select 1,@BatchID,'DISABLED','0'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
