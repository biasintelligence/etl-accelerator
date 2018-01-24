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
exec dbo.prc_createHeader @pHeader out,8,null,null,4,1,15
exec dbo.prc_createContext @pContext out,@pHeader
select @pContext
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
select getdate()
exec dbo.prc_Execute 'MoveData_TableToTable','debug;forcestart'
*/

set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 8
set @BatchName = 'MoveData_TableToTable' 

print ' Compiling MoveData_TableToTable'

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
select @BatchID,@BatchName,'MoveData_TableToTable',null,null,0,1
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
union all select @BatchID,'Src_Server'		,'isnull(dbo.fn_systemparameter(''Environment'',''Src_Server'',''<ENV*>''),@@SERVERNAME)'
union all select @BatchID,'Src_DB'			,'isnull(dbo.fn_systemparameter(''Environment'',''Src_DB'',''<ENV*>''),db_name())'
union all select @BatchID,'Dest_Server'		,'isnull(dbo.fn_systemparameter(''Environment'',''Dest_Server'',''<ENV*>''),@@SERVERNAME)'
union all select @BatchID,'Dest_DB'			,'isnull(dbo.fn_systemparameter(''Environment'',''Dest_DB'',''<ENV*>''),''ETL_Staging'')'
union all select @BatchID,'Staging_DB'		,'isnull(dbo.fn_systemparameter(''Environment'',''ETL_Staging'',''<ENV*>''),''ETL_Staging'')'
union all select @BatchID,'Path'			,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'

union all select @BatchID,'LocalServer','@@SERVERNAME'
union all select @BatchID,'LocalDB','db_name()'
union all select @BatchID,'StagingAreaRoot','<Path*>\DSV'
union all select @BatchID,'Control.Server','<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'DEPath','<Path*>\DeltaExtractor\DeltaExtractor64.exe'
union all select @BatchID,'FlatFilesDir','<Path*>\FlatFiles'

--set DE/SQL required attributes 
union all select @BatchID,'CONSOLE','sqlcmd'
union all select @BatchID,'Command_SQL','-S <Dest_Server*> -d <Dest_DB*> -E -b -Q"<Query>"'
union all select @BatchID,'ARG','<Command_SQL>'

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
          select  1,@BatchID,'ST01','Create Controller DB test table',1,null,null,null,'01'
union all select  2,@BatchID,'ST02','Create Dest DB tables',8,null,null,1,'02' 
union all select  3,@BatchID,'ST03','1st MoveData to Dest table',7,null,null,null,'03'
union all select  4,@BatchID,'ST04','Get Dest table RowCount',7,null,null,null,'04'
union all select  5,@BatchID,'STO5','Test 1st MoveData to Dest table',1,null,null,1,'05'

union all select  6,@BatchID,'ST06','2nd MoveData to Dest table',7,null,null,null,'06'
union all select  7,@BatchID,'ST07','Get Dest table RowCount',7,null,null,null,'07'
union all select  8,@BatchID,'ST08','Test 2nd MoveData to Dest table',1,null,null,1,'08'

union all select  9,@BatchID,'ST09','3rd MoveData to Dest table (with reload)',7,null,null,null,'09'
union all select 10,@BatchID,'ST010','Get Dest table RowCount',7,null,null,null,'10'
union all select 11,@BatchID,'ST011','Test 3rd MoveData table to table step',1,null,null,1,'11'

union all select 12,@BatchID,'ST012','MoveData to Dest table (with staging)',7,null,null,null,'12'
union all select 13,@BatchID,'ST013','Get Dest table RowCount',7,null,null,null,'13'
union all select 14,@BatchID,'ST014','Test MoveData to Dest table (with staging)',1,null,null,1,'14'

union all select 15,@BatchID,'ST015','Drop Controller DB test table',1,null,null,null,'15'
union all select 16,@BatchID,'ST016','Drop Dest DB tables',8,null,null,null,'16' 

set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','01'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'SQL','
If exists (select * from sys.objects where object_id = object_id(N''dbo.TestRowCounts'') AND type in (N''U''))
	drop table dbo.TestRowCounts;
create table dbo.TestRowCounts(RowCnt int not NULL);'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 2,@BatchID,'PRIGROUP','01'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'Query','
If exists (select * from sys.objects where object_id = object_id(N''dbo.ETLBatch_WO_Staging'') AND type in (N''U''))
  drop table dbo.ETLBatch_WO_Staging;
create table dbo.ETLBatch_WO_Staging(BatchID int NOT NULL,BatchName varchar(30) NOT NULL);
If  exists (select * from sys.objects where object_id = object_id(N''dbo.ETLBatch_With_Staging'') AND type in (N''U''))
  drop table dbo.ETLBatch_With_Staging;
create table dbo.ETLBatch_With_Staging(BatchID int NOT NULL,BatchName varchar(30),AuditId int NULL,ActionId int NOT NULL PRIMARY KEY CLUSTERED (BatchID ASC));
'


-- use DeltaExtractor (ProcessID = 7) move data from a SQL table into a SQL table without staging
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 3,@BatchID,'PRIGROUP','02'
union all select 3,@BatchID,'DISABLED','0'
union all select 3,@BatchID,'Action','MoveData'
union all select 3,@BatchID,'Source.Component','OLEDB'
union all select 3,@BatchID,'Source.Server','<Src_Server*>'
union all select 3,@BatchID,'Source.Database','<Src_DB*>'
union all select 3,@BatchID,'Source.Query','select BatchID, BatchName from dbo.ETLBatch'
union all select 3,@BatchID,'Destination.Component','OLEDB'
union all select 3,@BatchID,'Destination.tableName','dbo.ETLBatch_WO_Staging'
union all select 3,@BatchID,'Destination.Server','<Dest_Server*>'
union all select 3,@BatchID,'Destination.Database','<Dest_DB*>'					-- Destination.Database <> 'ETL_Staging'
union all select 3,@BatchID,'Destination.Staging','0'							-- Destination.Staging = '0'


--use DeltaExtractor (ProcessID = 7) to get rowcounts from multiple tables on the 'Destination.Server' and store them in a table in the ETL_Controller db
-- Destination.UserOptions <> 'reload' so that the data in the destination table from the previous step is not truncated
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 4,@BatchID,'PRIGROUP','03'
union all select 4,@BatchID,'DISABLED','0'
union all select 4,@BatchID,'Action','MoveData'
union all select 4,@BatchID,'Source.Component','OLEDB'
union all select 4,@BatchID,'Source.Server','<Dest_Server*>'
union all select 4,@BatchID,'Source.Database','<Dest_DB*>'
union all select 4,@BatchID,'Source.Query','select count(*) as RowCnt from dbo.ETLBatch_WO_Staging with (nolock)'
union all select 4,@BatchID,'Destination.Component','OLEDB'
union all select 4,@BatchID,'Destination.tableName','dbo.TestRowCounts'
union all select 4,@BatchID,'Destination.Server','<Control.Server>'
union all select 4,@BatchID,'Destination.Database','<Control.Database>'
union all select 4,@BatchID,'Destination.Staging','0'


-- Test step 'DE Table to table w/o staging', with ProcessID = 1
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 5,@BatchID,'PRIGROUP','04'
union all select 5,@BatchID,'SEQGROUP','0'
union all select 5,@BatchID,'DISABLED','0'
union all select 5,@BatchID,'SQL','
	If (select count(*) from ETLBatch) <> 
	(select RowCnt 
	from dbo.TestRowCounts)
		RAISERROR (''DE Table to table w/o staging step did not generate table with correct number of rows'', 11,17)'
		
		
-- use DeltaExtractor (ProcessID = 7) move data from a SQL table into a SQL table without staging
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 6,@BatchID,'PRIGROUP','05'
union all select 6,@BatchID,'DISABLED','0'
union all select 6,@BatchID,'Action','MoveData'
union all select 6,@BatchID,'Source.Component','OLEDB'
union all select 6,@BatchID,'Source.Server','<Src_Server*>'
union all select 6,@BatchID,'Source.Database','<Src_DB*>'
union all select 6,@BatchID,'Source.Query','select BatchID, BatchName from dbo.ETLBatch'
union all select 6,@BatchID,'Destination.Component','OLEDB'
union all select 6,@BatchID,'Destination.tableName','dbo.ETLBatch_WO_Staging'
union all select 6,@BatchID,'Destination.Server','<Dest_Server*>'
union all select 6,@BatchID,'Destination.Database','<Dest_DB*>'					-- Destination.Database <> 'ETL_Staging'
union all select 6,@BatchID,'Destination.Staging','0'							-- Destination.Staging = '0'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 7,@BatchID,'PRIGROUP','06'
union all select 7,@BatchID,'DISABLED','0'
union all select 7,@BatchID,'Action','MoveData'
union all select 7,@BatchID,'Source.Component','OLEDB'
union all select 7,@BatchID,'Source.Server','<Dest_Server*>'
union all select 7,@BatchID,'Source.Database','<Dest_DB*>'
union all select 7,@BatchID,'Source.Query','select count(*) as RowCnt from dbo.ETLBatch_WO_Staging with (nolock)'
union all select 7,@BatchID,'Destination.Component','OLEDB'
union all select 7,@BatchID,'Destination.tableName','dbo.TestRowCounts'
union all select 7,@BatchID,'Destination.Server','<Control.Server>'
union all select 7,@BatchID,'Destination.Database','<Control.Database>'
union all select 7,@BatchID,'Destination.Staging','0'
union all select 7,@BatchID,'Destination.UserOptions','reload'


-- Test step 'DE Table to table w/o staging', with ProcessID = 1
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 8,@BatchID,'PRIGROUP','07'
union all select 8,@BatchID,'SEQGROUP','0'
union all select 8,@BatchID,'DISABLED','0'
union all select 8,@BatchID,'SQL','
	If (select (count(*) * 2) from ETLBatch) <> 
	(select RowCnt 
	from dbo.TestRowCounts)
		RAISERROR (''DE Table to table w/o staging step did not generate table with correct number of rows'', 11,17)'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 9,@BatchID,'PRIGROUP','08'
union all select 9,@BatchID,'DISABLED','0'
union all select 9,@BatchID,'Action','MoveData'
union all select 9,@BatchID,'Source.Component','OLEDB'
union all select 9,@BatchID,'Source.Server','<Src_Server*>'
union all select 9,@BatchID,'Source.Database','<Src_DB*>'
union all select 9,@BatchID,'Source.Query','select BatchID, BatchName from dbo.ETLBatch'
union all select 9,@BatchID,'Destination.Component','OLEDB'
union all select 9,@BatchID,'Destination.tableName','dbo.ETLBatch_WO_Staging'
union all select 9,@BatchID,'Destination.Server','<Dest_Server*>'
union all select 9,@BatchID,'Destination.Database','<Dest_DB*>'				-- Destination.Database <> 'ETL_Staging'
union all select 9,@BatchID,'Destination.Staging','0'							-- Destination.Staging = '0'
union all select 9,@BatchID,'Destination.UserOptions','reload'					-- Destination.UserOptions = 'reload'	


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 10,@BatchID,'PRIGROUP','09'
union all select 10,@BatchID,'DISABLED','0'
union all select 10,@BatchID,'Action','MoveData'
union all select 10,@BatchID,'Source.Component','OLEDB'
union all select 10,@BatchID,'Source.Server','<Dest_Server*>'
union all select 10,@BatchID,'Source.Database','<Dest_DB*>'
union all select 10,@BatchID,'Source.Query','select count(*) as RowCnt from dbo.ETLBatch_WO_Staging with (nolock)'
union all select 10,@BatchID,'Destination.Component','OLEDB'
union all select 10,@BatchID,'Destination.tableName','dbo.TestRowCounts'
union all select 10,@BatchID,'Destination.Server','<Control.Server>'
union all select 10,@BatchID,'Destination.Database','<Control.Database>'
union all select 10,@BatchID,'Destination.Staging','0'
union all select 10,@BatchID,'Destination.UserOptions','reload'


-- Test step 'DE Table to table w/o staging', with ProcessID = 1
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 11,@BatchID,'PRIGROUP','10'
union all select 11,@BatchID,'SEQGROUP','0'
union all select 11,@BatchID,'DISABLED','0'
union all select 11,@BatchID,'SQL','
	If (select count(*)from ETLBatch) <> 
	(select RowCnt 
	from dbo.TestRowCounts)
		RAISERROR (''DE Table to table w/o staging step did not generate table with correct number of rows'', 11,17)'

		
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 12,@BatchID,'PRIGROUP','11'
union all select 12,@BatchID,'DISABLED','0'
union all select 12,@BatchID,'Action','MoveData'
union all select 12,@BatchID,'Source.Component','OLEDB'
union all select 12,@BatchID,'Source.Server','<Src_Server*>'
union all select 12,@BatchID,'Source.Database','<Src_DB*>'
union all select 12,@BatchID,'Source.Query','select BatchID, BatchName from dbo.ETLBatch'
union all select 12,@BatchID,'Destination.Component','OLEDB'
union all select 12,@BatchID,'Destination.tableName','<Dest_DB*>.dbo.ETLBatch_With_Staging'
union all select 12,@BatchID,'Destination.Server','<Dest_Server*>'
union all select 12,@BatchID,'Destination.Database','ETL_Staging'				-- Destination.Database = 'ETL_Staging'
union all select 12,@BatchID,'Destination.Staging','1'							-- Destination.Staging = '1'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 13,@BatchID,'PRIGROUP','12'
union all select 13,@BatchID,'DISABLED','0'
union all select 13,@BatchID,'Action','MoveData'
union all select 13,@BatchID,'Source.Component','OLEDB'
union all select 13,@BatchID,'Source.Server','<Dest_Server*>'
union all select 13,@BatchID,'Source.Database','<Dest_DB*>'
union all select 13,@BatchID,'Source.Query','
	select count(*) as RowCnt from dbo.ETLBatch_With_Staging e with (nolock)
	join Audit a with (nolock) on e.AuditId = a.AuditId
	where e.ActionId = 1
	and a.RunId = <@RunId>
	and a.AuditObject like ''%ETLBatch_With_Staging%''
	and a.RowCnt = (select count(*) as RowCnt from dbo.ETLBatch_With_Staging e with (nolock))'
union all select 13,@BatchID,'Destination.Component','OLEDB'
union all select 13,@BatchID,'Destination.tableName','dbo.TestRowCounts'
union all select 13,@BatchID,'Destination.Server','<Control.Server>'
union all select 13,@BatchID,'Destination.Database','<Control.Database>'
union all select 13,@BatchID,'Destination.Staging','0'
union all select 13,@BatchID,'Destination.UserOptions','reload'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 14,@BatchID,'PRIGROUP','13'
union all select 14,@BatchID,'DISABLED','0'
union all select 14,@BatchID,'SQL','
	If (select count(*) from ETLBatch) <> 
	(select RowCnt 
	from dbo.TestRowCounts)
		RAISERROR (''DE Table to table w/o staging step did not generate table with correct number of rows'', 11,17)'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 15,@BatchID,'PRIGROUP','14'
union all select 15,@BatchID,'DISABLED','0'
union all select 15,@BatchID,'SQL','drop table dbo.TestRowCounts;'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 16,@BatchID,'PRIGROUP','14'
union all select 16,@BatchID,'DISABLED','0'
union all select 16,@BatchID,'Query','
drop table dbo.ETLBatch_WO_Staging;
drop table dbo.ETLBatch_With_Staging;'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
