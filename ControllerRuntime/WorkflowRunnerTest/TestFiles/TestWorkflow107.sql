---////////////////////////////
--Controller Test Workflow
---///////////////////////////
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 107
set @BatchName = 'Test107' 

print ' Compiling Test107'

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
union all select @BatchID,'OLEDB.ConnectionString','Data Source=<Control.Server>;Initial Catalog=<Test.Database>;Integrated Security=SSPI;'--;Provider=SQLNCLI11.1;Auto Translate=False;

union all select @BatchID,'CreateSourceTable','
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
'
union all select @BatchID,'CreateDestinationTable','
if (object_id(''<Test.DstTable>'') is not null)
	drop table <Test.DstTable>;

create table <Test.DstTable>
(id int not null
,val1 int null
,val2 nvarchar(100) null)
;		  
'


-----------------------------------------------------
--create workflow steps 
-----------------------------------------------------
--set identity_insert dbo.ETLStep on

insert dbo.ETLStep
	(StepID,BatchID,StepName,StepDesc,StepProcID
	,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
select 1,@BatchID,'ST01','create src table',20,null,null,null,'01'
union all
select 2,@BatchID,'ST02','create dst01 table',20,null,null,null,'02'
union all
select 3,@BatchID,'ST03','create dst02 table',20,null,null,null,'03'
union all
select 11,@BatchID,'ST11','multi dst test',24,null,null,null,'11'

--set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)

		  select 1,@BatchID,'ConnectionString','<Test.ConnectionString>'
union all select 1,@BatchID,'Test.SrcTable','dbo.oledb_test'
union all select 1,@BatchID,'Query','<CreateSourceTable>'
union all select 1,@BatchID,'Timeout','20'

union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'PRIGROUP','1'

union all select 2,@BatchID,'ConnectionString','<Test.ConnectionString>'
union all select 2,@BatchID,'Test.DstTable','dbo.staging_oledb_test1'
union all select 2,@BatchID,'Query','<CreateDestinationTable>'
union all select 2,@BatchID,'Timeout','20'

union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'PRIGROUP','1'

union all select 3,@BatchID,'ConnectionString','<Test.ConnectionString>'
union all select 3,@BatchID,'Test.DstTable','dbo.staging_oledb_test2'
union all select 3,@BatchID,'Query','<CreateDestinationTable>'
union all select 3,@BatchID,'Timeout','20'

union all select 3,@BatchID,'DISABLED','0'
union all select 3,@BatchID,'PRIGROUP','1'

union all select 11,@BatchID,'Test.SrcTable','dbo.oledb_test1'
union all select 11,@BatchID,'Test.DstTable','dbo.staging_oledb_test1'
union all select 11,@BatchID,'Action','MoveData'
union all select 11,@BatchID,'QueryTimeout','120'
union all select 11,@BatchID,'Source.Component','OLEDB'
union all select 11,@BatchID,'Source.OLEDB.AccessMode','SQL Command'			
union all select 11,@BatchID,'Source.ConnectionString','<OLEDB.ConnectionString>'			
union all select 11,@BatchID,'Source.Query','select * from <Test.SrcTable>'

union all select 11,@BatchID,'Destination_01.Component','OLEDB'
union all select 11,@BatchID,'Destination_01.OLEDB.AccessMode','OpenRowset Using FastLoad'			
union all select 11,@BatchID,'Destination_01.ConnectionString','<OLEDB.ConnectionString>'
union all select 11,@BatchID,'Destination_01.TableName','dbo.staging_oledb_test1'			
union all select 11,@BatchID,'Destination_01.OLEDB.FastLoadOptions','TABLOCK,CHECK_CONSTRAINTS,ROWS_PER_BATCH = 10000'
union all select 11,@BatchID,'Destination_01.Staging','0'

union all select 11,@BatchID,'Destination_02.Component','OLEDB'
union all select 11,@BatchID,'Destination_02.OLEDB.AccessMode','OpenRowset Using FastLoad'			
union all select 11,@BatchID,'Destination_02.ConnectionString','<OLEDB.ConnectionString>'
union all select 11,@BatchID,'Destination_02.TableName','dbo.staging_oledb_test2'			
union all select 11,@BatchID,'Destination_02.OLEDB.FastLoadOptions','TABLOCK,CHECK_CONSTRAINTS,ROWS_PER_BATCH = 10000'
union all select 11,@BatchID,'Destination_02.Staging','0'
--union all select 11,@BatchID,'SavePackage','1'
--union all select 11,@BatchID,'PackageFileName','<Path*>\Test102_OLEDB.dtsx'

union all select 11,@BatchID,'DISABLED','0'
union all select 11,@BatchID,'PRIGROUP','11'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
