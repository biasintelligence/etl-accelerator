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
exec dbo.prc_createHeader @pHeader out,5,null,null,4,1,15
exec dbo.prc_createContext @pContext out,@pHeader
select @pContext
*/

/*
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
select getdate()
exec dbo.prc_Execute 'Call_Powershell','debug'
*/
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 5
set @BatchName = 'Call_Powershell' 

print ' Compiling Call_Powershell'

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
select @BatchID,@BatchName,'Test Workflow',null,5,0,1
set identity_insert dbo.ETLBatch off

-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'MAXTHREAD','4'
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

          select @BatchID,'ENV'			,'dbo.fn_systemparameter(''Environment'',''Current'',''ALL'')'
union all select @BatchID,'Path'		,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'

union all select @BatchID,'LocalServer'		,'@@SERVERNAME'
union all select @BatchID,'LocalDB'		,'db_name()'
union all select @BatchID,'Control.Server'	,'<LocalServer*>'
union all select @BatchID,'Control.Database'	,'<LocalDB*>'
union all select @BatchID,'DEPath'		,'<Path*>\DeltaExtractor\DeltaExtractor64.exe'
union all select @BatchID,'FlatFilesDir'	,'<Path*>\FlatFiles'

union all select @BatchID,'SQL_ER','If exists (select * from sys.objects where object_id = object_id(N''dbo.sqlps_Test'') AND type in (N''U''))
   drop table dbo.sqlps_Test;'

-------------------------------------------------------
--create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select 1,@BatchID,'ST01','Call Powershell to create folder',8,null,null,null,'01'
union all select 2,@BatchID,'ST02','Test if file was created by step 3',8,null,null,null,'02'   
union all select 3,@BatchID,'ST03','Call Powershell to create file',8,null,null,null,'03'
union all select 4,@BatchID,'ST04','Call Powershell for SQL Server',8,null,null,null,'04'
union all select 5,@BatchID,'ST05','Test call Powershell for SQL Server step',1,null,null,null,'05' 
union all select 6,@BatchID,'ST06','Drop folder',8,null,null,null,'06'
union all select 7,@BatchID,'ST07','Delete row inserted in step 4',8,null,null,null,'07'
                    

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------

--- Call Powershell cmdlet to create a file
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','1'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'CONSOLE','powershell.exe'
union all select 1,@BatchID,'ARG','New-Item <FlatFilesDir> -type directory -force'


-- check if file created by step 3 exists
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 2,@BatchID,'PRIGROUP','2'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'CONSOLE','cmd'
union all select 2,@BatchID,'file','<FlatFilesDir>\PS_created.txt'
union all select 2,@BatchID,'ARG','/c dir <file>'

--ETLStepConstraint
set identity_insert dbo.ETLStepConstraint on
insert dbo.ETLStepConstraint
(ConstID,BatchID,StepID,ProcessID,ConstOrder,WaitPeriod)
--wait <?> min for the files
          select  1 ,@BatchID,2,11,'01',2
set identity_insert dbo.ETLStepConstraint off

--ETLStepConstraintAttribute
insert dbo.ETLStepConstraintAttribute
(ConstID,BatchID,StepID,AttributeName,AttributeValue)
          select 1,@BatchID,2,'CheckFile','<FlatFilesDir>\PS_created.txt'
union all select 1,@BatchID,2,'PING','20'
union all select 1,@BatchID,2,'DISABLED','0'


-- Call Powershell .ps1 script file to create a file
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 3,@BatchID,'PRIGROUP','2'
union all select 3,@BatchID,'DISABLED','0'
union all select 3,@BatchID,'CONSOLE','powershell.exe'
union all select 3,@BatchID,'file','PS_created.txt'
union all select 3,@BatchID,'ARG','New-Item <FlatFilesDir>\<file> -type file -force'
--union all select 3,@BatchID,'ARG','<Path*>\TestFiles\createFile.ps1 -FilePath <FlatFilesDir>\<file>'


-- Call Powershell for SQL Server (SQLPS.EXE) to insert row into ETLStepRunCounter table
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 4,@BatchID,'PRIGROUP','2'
union all select 4,@BatchID,'DISABLED','0'
union all select 4,@BatchID,'CONSOLE','sqlps.exe'
union all select 4,@BatchID,'ARG','&"
$query = ''exec dbo.prc_etlCounterSet <@BatchID>,<@StepID>,<@RunID>,''''SQLPS'''',''''Success'''';'';
invoke-sqlcmd -serverinstance <Control.Server> -database <Control.Database> -query $query -ErrorLevel 1 -AbortOnError;
 "'


-- Check if row got inserted into ETLStepRunCounter table with Status = 'Success' by step 4
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 5,@BatchID,'PRIGROUP','3'
union all select 5,@BatchID,'DISABLED','0'
union all select 5,@BatchID,'SQL','
declare @result nvarchar(100) 
set @result = [dbo].[fn_CounterGet] (<@BatchID>,4,<@RunID>,''SQLPS'')
if isNull(@result,'''') <> ''Success''
   RAISERROR (''Call Powershell for SQL Server step failed:'', 11,17)'


-- Call Powershell cmdlet to drop folder
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 6,@BatchID,'PRIGROUP','4'
union all select 6,@BatchID,'DISABLED','0'
union all select 6,@BatchID,'CONSOLE','powershell.exe'
union all select 6,@BatchID,'ARG','Remove-Item <FlatFilesDir> -recurse'

-- Call Powershell for SQL Server (SQLPS.EXE) to delete the row int ETLStepRunCounter table
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 7,@BatchID,'PRIGROUP','4'
union all select 7,@BatchID,'DISABLED','0'
union all select 7,@BatchID,'CONSOLE','sqlps.exe'
union all select 7,@BatchID,'ARG','&"
$query = ''delete ETLStepRunCounter where BatchId = <@BatchId> and RunId = <@RunId> and CounterName = ''''sqlps'''';'';
invoke-sqlcmd -serverinstance <Control.Server> -database <Control.Database> -query $query -ErrorLevel 1 -AbortOnError;"' 
-- query parameter can also be embedded without using a $query object like this:
-- invoke-sqlcmd -Query ''delete ETLStepRunCounter where BatchId = <@BatchId> and CounterName = ''''SQLPS'''' and RunId = <@RunId>'' -Server V-CCOLIE1-11 -Database etl_controller"'
 
 
end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go