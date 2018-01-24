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
exec dbo.prc_createHeader @pHeader out,6,null,null,4,1,15
exec dbo.prc_createContext @pContext out,@pHeader
select @pContext
*/

/*
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
select getdate()
exec dbo.prc_Execute 'IncrementalStaging','debug;forcestart'
*/

set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 6
set @BatchName = 'IncrementalStaging' 

print ' Compiling IncrementalStaging'

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
select @BatchID,@BatchName,'Incremental_Batch',null,null,1,1
set identity_insert dbo.ETLBatch off


-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'MAXTHREAD','1'
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
union all select @BatchID,'StagingDB'		,'isnull(dbo.fn_systemparameter(''Environment'',''Staging_DB'',''<ENV*>''),''ETL_Staging'')'
union all select @BatchID,'Path'			,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'

union all select @BatchID,'LocalServer'		,'@@SERVERNAME'
union all select @BatchID,'LocalDB'			,'db_name()'
union all select @BatchID,'Control.Server'	,'<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'StagingAreaRoot'	,'<Path*>\DSV'
union all select @BatchID,'DEPath'			,'<Path*>\DeltaExtractor\DeltaExtractor64.exe'

-------------------------------------------------------
--create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on
insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select 1,@BatchID,'ST01','control Counter Set',7,null,null,null,'01'
union all select 2,@BatchID,'ST02','get Src_table',7,5,null,null,'02'

set identity_insert dbo.ETLStep off


-- this step gets the maximum ControlValue from the Src_Tbl table and inserts it into the ETLStepRunCounter, so that the next run of the batch 
-- will know to get data from Src_Tbl which has OperationId > that value.  
-- Note that the inserted row has StepId = 2 because that is step which pulls data from Src_Tbl.
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','1'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'Source.Component','OLEDB'
union all select 1,@BatchID,'Source.Server','<LocalServer*>'		-- would normally not be LocalServer
union all select 1,@BatchID,'Source.Database','<LocalDB*>'
union all select 1,@BatchID,'Source.Query',
'
select <@BatchID> as BatchID,t.StepID,<@RunID> as RunID,''ControlValue'' as CounterName,t.CounterValue,getdate() as createdDTim,getdate() as ModIfiedDTim
from (
select 2 as StepID,cast(max(OperationId) as nvarchar(23)) as CounterValue from Src_Tbl
) t'
union all select 1,@BatchID,'Destination.Component','OLEDB'
union all select 1,@BatchID,'Destination.tableName','dbo.ETLStepRunCounter'
union all select 1,@BatchID,'Destination.Server','<Control.Server>'	
union all select 1,@BatchID,'Destination.Database','<Control.Database>'
union all select 1,@BatchID,'Destination.Staging','0'
union all select 1,@BatchID,'Destination.OLEDB.AccessMode','OpenRowset'
union all select 1,@BatchID,'Action','MoveData'
union all select 1,@BatchID,'RETRY','0'
union all select 1,@BatchID,'DELAY','30'
union all select 1,@BatchID,'RESTART','1'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 2,@BatchID,'PRIGROUP','2'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'Counter_Set','
declare @s nvarchar(23)
set @s = isnull(dbo.fn_CounterGet(<@BatchID>,<@StepID>,<@RunID>,''ControlValue''),''<ControlValue>'')
exec prc_ETLAttributeSet <@BatchID>,<@StepID>,null,''ControlValue'',@s'
union all select 2,@BatchID,'SQL_ER','<Counter_Set>'
union all select 2,@BatchID,'Action','MoveData'
union all select 2,@BatchID,'Source.Component','OLEDB'
union all select 2,@BatchID,'Source.Server','<LocalServer*>'		
union all select 2,@BatchID,'Source.Database','<LocalDB*>'
union all select 2,@BatchID,'SourceQuery','select * from Src_Tbl'
union all select 2,@BatchID,'Source.Query','<SourceQuery> where IsNull(<ControlColumn>,0) > <ControlValue>'

union all select 2,@BatchID,'Destination.Component','OLEDB'
union all select 2,@BatchID,'Destination.tableName','<Dest_DB*>.dbo.Dest_Tbl'
union all select 2,@BatchID,'Destination.Server','<Dest_Server*>'
union all select 2,@BatchID,'Destination.Database','<StagingDB*>'
union all select 2,@BatchID,'Destination.Staging','1'
union all select 2,@BatchID,'ControlColumn','OperationId'
union all select 2,@BatchID,'ControlValue','0'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go

