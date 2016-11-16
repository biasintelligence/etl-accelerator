--delete	e
--from		ETL_Event.dbo.[Event] e
--join		ETL_Event.dbo.EventType et
--on		e.EventTypeID = et.EventTypeID
--and		et.EventTypeName in ('Process1_FINISHED','Process2_FINISHED');
--delete	ETL_Event.dbo.EventType 
--where	EventTypeName in ('Process1_FINISHED','Process2_FINISHED');
--insert ETL_Event.dbo.EventType(EventTypeID,EventTypeName,EventArgsSchema,SourceName,LogRetention,CreateDT,ModifyDT)
--select NEWID(),'Process1_FINISHED','EventArgs.XSD','',100,GETDATE(),GETDATE()
--union all
--select NEWID(),'Process2_FINISHED','EventArgs.XSD','',100,GETDATE(),GETDATE()
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
exec dbo.prc_CreateHeader @pHeader out,9,null,null,4,1,15
exec dbo.prc_CreateContext @pContext out,@pHeader
select @pContext

-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'Step_WaitConstraint_Met','debug;forcestart'
*/


set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 9
set @BatchName = 'Step_WaitConstraint_Met' 

print ' Compiling Step_WaitConstraint_Met'

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
select @BatchID,@BatchName,'Step_WaitConstraint_Met',3,null,0,1
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
union all select @BatchID,'Max_RunTime'		,'isnull(dbo.fn_systemparameter(''Environment'',''Max_RunTime'',''<ENV*>''),''20:00'')'
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

------------------------------------------------------
--create workflow steps 
-----------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
	(StepID,BatchID,StepName,StepDesc,StepProcID
	,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
select 1,@BatchID,'ST01','Wait for Process1 Event',1,null,null,null,'01'
union all
select 2,@BatchID,'ST02','Insert Process1 Event',1,null,null,null,'02'

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','2'
union all select 1,@BatchID,'SQL','SELECT 1+1'	
union all select 1,@BatchID,'PING','30'
union all select 1,@BatchID,'DISABLED','0'

--StepWaitConstraint
set identity_insert dbo.ETLStepConstraint on
insert dbo.ETLStepConstraint
(ConstID,BatchID,StepID,ProcessID,ConstOrder,WaitPeriod)
          select  1,@BatchID,1,12,'01',1
          
set identity_insert dbo.ETLStepConstraint off

--StepWaitConstraintAttribute
insert dbo.ETLStepConstraintAttribute
(ConstID,BatchID,StepID,AttributeName,AttributeValue)
		  select 1,@BatchID,1,'WatermarkEventType','Process2_FINISHED'
union all select 1,@BatchID,1,'EventType','Process1_FINISHED'				
union all select 1,@BatchID,1,'EventServer','<LocalServer*>'			
union all select 1,@BatchID,1,'EventDatabase','<Event_Database*>'
union all select 1,@BatchID,1,'PING','30'
union all select 1,@BatchID,1,'DISABLED','0'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 2,@BatchID,'PRIGROUP','2'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'SQL','
waitfor delay ''00:00:10''
declare @eventargs xml
;with xmlnamespaces(''EventArgs.XSD'' as dwc)
select @eventargs = 
(select ''Process1 Finished'' as ''@Source'',''Week'' as ''@PeriodGrain'',''<date*>'' as ''@Period''
for xml path(''dwc:EventArgs''),type)
exec dbo.prc_ClrEventPost ''<LocalServer*>'',''<Event_Database*>'',''Process1_FINISHED'',null,@eventargs,''debug'''


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
