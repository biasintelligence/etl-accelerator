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
exec dbo.prc_createHeader @pHeader out,14,null,null,4,1,15
exec dbo.prc_createContext @pContext out,@pHeader
select @pContext
*/

/*
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
select getdate()
exec dbo.prc_Execute 'FileCheck','debug;forcestart'
*/
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 14
set @BatchName = 'FileCheck' 

print ' Compiling FileCheck'

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
select @BatchID,@BatchName,'Test Workflow',null,null,0,1
set identity_insert dbo.ETLBatch off

-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'MAXTHREAD','2'
union all select @BatchID,'TIMEOUT','260'
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
union all select @BatchID,'Path'			,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'

union all select @BatchID,'LocalServer'		,'@@SERVERNAME'
union all select @BatchID,'LocalDB'			,'db_name()'
union all select @BatchID,'Control.Server'	,'<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'DEPath'			,'<Path*>\DeltaExtractor\DeltaExtractor64.exe'


-------------------------------------------------------
--create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
select 1,@BatchID,'ST01','Check if file exists',1,null,null,null,'01'   
                    
set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------

-- check if file exists
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
		  select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'SQL','select 1'

--ETLStepConstraint
set identity_insert dbo.ETLStepConstraint on
insert dbo.ETLStepConstraint
(ConstID,BatchID,StepID,ProcessID,ConstOrder,WaitPeriod)
--wait <?> min for the files
          select  1 ,@BatchID,1,11,'01',0
set identity_insert dbo.ETLStepConstraint off

--ETLStepConstraintAttribute
insert dbo.ETLStepConstraintAttribute
(ConstID,BatchID,StepID,AttributeName,AttributeValue)
          select 1,@BatchID,1,'CheckFile','<Path*>\TestFiles\FileCheckTest.txt'
union all select 1,@BatchID,1,'PING','20'
union all select 1,@BatchID,1,'DISABLED','0'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go