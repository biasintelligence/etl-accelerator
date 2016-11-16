set quoted_identifier on;
/*
-----------------------------------------------------------
--This code will return XML representation of this workflow
-----------------------------------------------------------
declare @pHeader xml
declare @pContext xml
exec dbo.prc_createHeader @pHeader out,1,null,null,4,1,15
exec dbo.prc_createContext @pContext out,@pHeader
select @pContext
*/
/*
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'Process1','debug;forcestart'
*/

set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 1
set @BatchName = 'Process1' 

print ' Compiling Test Batch'

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
select @BatchID,@BatchName,'Process1',3,null,0,1
set identity_insert dbo.ETLBatch off

-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'TIMEOUT','120'
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
union all select @BatchID,'Event_Server'	,'isnull(dbo.fn_systemparameter(''Environment'',''EventServer'',''<ENV*>''),@@SERVERNAME)'
union all select @BatchID,'Event_Database'	,'isnull(dbo.fn_systemparameter(''Environment'',''EventDB'',''<ENV*>''),''ETL_Event'')'
union all select @BatchID,'Path'			,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'

union all select @BatchID,'LocalServer','@@SERVERNAME'
union all select @BatchID,'LocalDB','db_name()'
union all select @BatchID,'Control.Server','<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'DEPath','<Path*>\DeltaExtractor\DeltaExtractor64.exe'

--Post Event Process1_FINISHED on Batch success (processid = 3)
union all select @BatchID,'date','convert(nchar(8),getdate(),112)'
union all select @BatchID,'SQL_01','
declare @eventargs xml
;with xmlnamespaces(''EventArgs.XSD'' as dwc)
select @eventargs = 
(select ''Process1 Finished'' as ''@Source'',''Week'' as ''@PeriodGrain'',''<date*>'' as ''@Period''
for xml path(''dwc:EventArgs''),type)
exec dbo.prc_ClrEventPost ''<LocalServer*>'',''<Event_Database*>'',''Process1_FINISHED'',null,@eventargs,''debug'''

-------------------------------------------------------
--create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select 1,@BatchID,'ST01','Doing nothing',1,null,null,null,'01'

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------

-- execute SQL statement to create test tables on the local SQL Server only (ProcessID = 1)  
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)

		  select 1,@BatchID,'SQL','SELECT 1+1'
union all select 1,@BatchID,'PING','10'
union all select 1,@BatchID,'DISABLED','0'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
