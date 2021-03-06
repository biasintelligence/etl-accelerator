﻿---////////////////////////////
--Controller Test Workflow
---///////////////////////////
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 102
set @BatchName = 'Test102' 

print ' Compiling Test102'

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
union all select @BatchID,'LocalServer'		,'@@SERVERNAME'
union all select @BatchID,'LocalDB'			,'db_name()'
union all select @BatchID,'Control.Server'	,'<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'Controller.ConnectionString','Server=<Control.Server>;Database=<Control.Database>;Trusted_Connection=True;Connection Timeout=30;'
union all select @BatchID,'ActivityLocation','.\'
--WaitActivity
union all select @BatchID,'WaitTimeout'		,'10'
--operational
union all select @BatchID,'Test.Database','ETL_Staging'
union all select @BatchID,'Test.ConnectionString','Server=<Control.Server>;Database=<Test.Database>;Trusted_Connection=True;Connection Timeout=30;'
union all select @BatchID,'Test.SrcTable','dbo.odbc_test'
union all select @BatchID,'Test.DstTable','dbo.staging_odbc_test'

-----------------------------------------------------
--create workflow steps 
-----------------------------------------------------
--set identity_insert dbo.ETLStep on

insert dbo.ETLStep
	(StepID,BatchID,StepName,StepDesc,StepProcID
	,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
select 1,@BatchID,'ST01','create test tables',20,null,null,null,'01'
union all
select 2,@BatchID,'ST02','ODBC test',24,null,null,null,'02'

--set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)

		  select 1,@BatchID,'Query','
if (object_id(''<Test.SrcTable>'') is not null)
	drop table <Test.SrcTable>;

create table <Test.SrcTable>
(id int not null
,val1 int null
,val2 nvarchar(100) null)
;

insert <Test.SrcTable>
values(1,1,''test'')
,(2,2,''test'')
;

if (object_id(''<Test.DstTable>'') is not null)
	drop table dbo.staging_odbc_test;

create table <Test.DstTable>
(id int not null
,val1 int null
,val2 nvarchar(100) null)
;		  
'
union all select 1,@BatchID,'ConnectionString','<Test.ConnectionString>'
union all select 1,@BatchID,'Timeout','20'

union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'SEQGROUP','1'
union all select 1,@BatchID,'PRIGROUP','1'


union all select 2,@BatchID,'Action','MoveData'
union all select 2,@BatchID,'QueryTimeout','120'
union all select 2,@BatchID,'Source.Component','ODBC'
union all select 2,@BatchID,'Source.ODBC.AccessMode','SQL Command'			
union all select 2,@BatchID,'Source.ConnectionString','Driver={SQL Server};server=<Control.Server>;database=<Test.Database>;trusted_connection=Yes'			
union all select 2,@BatchID,'Source.Query','select * from <Test.SrcTable>'
union all select 2,@BatchID,'Source.ODBC.BatchSize','1000'
union all select 2,@BatchID,'Source.ODBC.LobChunkSize','32768'
union all select 2,@BatchID,'Source.ODBC.ExposeCharColumnsAsUnicode','false'
union all select 2,@BatchID,'Source.ODBC.FetchMethod','Batch'
union all select 2,@BatchID,'Source.ODBC.DefaultCodePage','1252'
union all select 2,@BatchID,'Source.ODBC.BindNumericAs','Char'
union all select 2,@BatchID,'Source.ODBC.BindCharColumnsAs','Unicode'

union all select 2,@BatchID,'Destination.Component','ODBC'
union all select 2,@BatchID,'Destination.InsertMode','Batch'			
union all select 2,@BatchID,'Destination.ConnectionString','Driver={SQL Server};server=<Control.Server>;database=<Test.Database>;trusted_connection=Yes'			
union all select 2,@BatchID,'Destination.TableName','<Test.DstTable>'			
union all select 2,@BatchID,'Destination.ODBC.BatchSize','1000'
union all select 2,@BatchID,'Destination.ODBC.LobChunkSize','32768'
union all select 2,@BatchID,'Destination.ODBC.DefaultCodePage','1252'
union all select 2,@BatchID,'Destination.ODBC.BindNumericAs','Char'
union all select 2,@BatchID,'Destination.ODBC.BindCharColumnsAs','Unicode'
union all select 2,@BatchID,'Destination.ODBC.TransactionSize','0'
union all select 2,@BatchID,'Destination.Staging','0'
--union all select 2,@BatchID,'SavePackage','1'
--union all select 2,@BatchID,'PackageFileName','<Path*>\Test102_ODBC.dtsx'

union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'SEQGROUP','1'
union all select 2,@BatchID,'PRIGROUP','1'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
