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
exec dbo.prc_createHeader @pHeader out,7,null,null,4,1,15
exec dbo.prc_createContext @pContext out,@pHeader
select @pContext
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'MoveData_TableToFile','debug;forcestart'
*/

set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 7
set @BatchName = 'MoveData_TableToFile' 

print ' Compiling MoveData_TableToFile'

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
select @BatchID,@BatchName,'MoveData_TableToFile',null,5,1,1
set identity_insert dbo.ETLBatch off

-------------------------------------------------------
--Define workflow level system attributes
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'MAXTHREAD','4'
union all select @BatchID,'TIMEOUT','360'
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
union all select @BatchID,'StagingAreaRoot'	,'<Path*>\TestFiles'
union all select @BatchID,'Control.Server'	,'<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'DEPath'			,'<Path*>\DeltaExtractor\DeltaExtractor64.exe'
union all select @BatchID,'FlatFilesDir'	,'<Path*>\FlatFiles'

union all select @BatchID,'DropTestTable','
If exists (select * from sys.objects where object_id = object_id(N''dbo.ETLBatch_From_File'') AND type in (N''U''))
   drop table dbo.ETLBatch_From_File;'
union all select @BatchID,'SQL_ER','<DropTestTable>'

-------------------------------------------------------
--create batch level constraints
-------------------------------------------------------

-------------------------------------------------------
--create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on

insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select 1,@BatchID,'ST01','Create test table',1,null,null,null,'01'
union all select 2,@BatchID,'ST02','Create test folder',8,null,null,1,'02'    
union all select 3,@BatchID,'ST03','DE table to file',7,null,null,null,'03'
union all select 4,@BatchID,'ST04','DE file to table ',7,null,null,null,'04'
union all select 5,@BatchID,'ST05','Test 2 tables have same number of rows',1,null,null,null,'05'
union all select 6,@BatchID,'ST06','Drop test table',1,null,null,null,'06'
union all select 7,@BatchID,'ST07','Delete test folder',8,null,null,null,'07' 


set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------

-- execute SQL statement to create test tables on the local SQL Server only (ProcessID = 1)  
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','1'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'SQL','
If exists (select * from sys.objects where object_id = object_id(N''dbo.ETLBatch_From_File'') AND type in (N''U''))
   drop table dbo.ETLBatch_From_File;
create table dbo.ETLBatch_From_File(BatchID int NOT NULL,BatchName varchar(30) NOT NULL,BatchDesc varchar(500) NULL);'


-- create folder with console app = cmd (ProcessID = 8) 
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 2,@BatchID,'PRIGROUP','1'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'CONSOLE','cmd'
union all select 2,@BatchID,'ARG','/c If NOT EXIST <FlatFilesDir> md <FlatFilesDir>'


-- use DeltaExtractor (ProcessID = 7) to move some data in a SQL table to a file
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 3,@BatchID,'PRIGROUP','2'
union all select 3,@BatchID,'DISABLED','0'
union all select 3,@BatchID,'Action','MoveData'
union all select 3,@BatchID,'Source.Component','OLEDB'
union all select 3,@BatchID,'Source.Server','<Control.Server>'			
union all select 3,@BatchID,'Source.Database','<Control.Database>'		
--union all select 11,@BatchID,'Source.tableName','dbo.ETLBatch'			
union all select 3,@BatchID,'Source.Query','select BatchId, BatchName, BatchDesc from dbo.ETLBatch'
union all select 3,@BatchID,'Destination.Component','FlatFile'
union all select 3,@BatchID,'Destination.Staging','0'
union all select 3,@BatchID,'Destination.StagingAreatable','dbo_ETLBatch'
union all select 3,@BatchID,'Destination.FlatFile.ConnectionString','<FlatFilesDir>\ETLBatch.txt'
union all select 3,@BatchID,'Destination.FlatFile.ColumnDelimiter',','--'\t'
union all select 3,@BatchID,'Destination.FlatFile.TextQualIfier','"'
--union all select 3,@BatchID,'SavePackage','1'
--union all select 3,@BatchID,'PackageFileName','C:\builds\Step3.dtsx'


-- use DeltaExtractor (ProcessID = 7) to move data from a file into a SQL table
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 4,@BatchID,'PRIGROUP','3'
union all select 4,@BatchID,'DISABLED','0'
union all select 4,@BatchID,'Action','MoveData'
union all select 4,@BatchID,'Source.Component','FlatFile'
union all select 4,@BatchID,'Source.StagingAreatable','dbo_ETLBatch'
union all select 4,@BatchID,'Source.FlatFile.ConnectionString','<FlatFilesDir>\ETLBatch.txt'
union all select 4,@BatchID,'Source.FlatFile.ColumnDelimiter',','--'\t'
union all select 4,@BatchID,'Source.FlatFile.TextQualIfier','"'
union all select 4,@BatchID,'Destination.Component','OLEDB'
union all select 4,@BatchID,'Destination.tableName','dbo.ETLBatch_From_File'
union all select 4,@BatchID,'Destination.Server','<Control.Server>'
union all select 4,@BatchID,'Destination.Database','<Control.Database>'
union all select 4,@BatchID,'Destination.Staging','0'
union all select 4,@BatchID,'Destination.UserOptions','reload'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 5,@BatchID,'PRIGROUP','4'
union all select 5,@BatchID,'DISABLED','0'
union all select 5,@BatchID,'SQL','
declare @ETLBatchCount int,@ETLBatch_From_FileCount int
select @ETLBatchCount = count(*) from ETLBatch
select @ETLBatch_From_FileCount = count(*) from ETLBatch_From_File
if (@ETLBatchCount <> @ETLBatch_From_FileCount)
		RAISERROR (''ETLBatch rowcount = %i, <> @ETLBatch_From_FileCount rowcount = %i'', 11,17,@ETLBatchCount,@ETLBatch_From_FileCount)'


-- drop test table
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 6,@BatchID,'PRIGROUP','5'
union all select 6,@BatchID,'DISABLED','0'
union all select 6,@BatchID,'SQL','<DropTestTable>'


-- drop test folder   
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 7,@BatchID,'PRIGROUP','5'
union all select 7,@BatchID,'DISABLED','0'
union all select 7,@BatchID,'CONSOLE','cmd'
union all select 7,@BatchID,'ARG','/c If EXIST <FlatFilesDir> rd /S /Q <FlatFilesDir>'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
