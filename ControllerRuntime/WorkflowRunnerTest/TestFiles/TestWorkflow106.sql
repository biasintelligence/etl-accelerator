---////////////////////////////
--Controller Test Workflow
---///////////////////////////
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 106
set @BatchName = 'Test106' 

print ' Compiling ' + @BatchName;

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
--operational
union all select @BatchID,'Test.File','<path*>\ExcelFiles\testIn.xlsx'
union all select @BatchID,'Test.ConnectionString','Provider=Microsoft.ACE.OLEDB.12.0;Data Source=<Test.File>;Extended Properties="Excel 12.0 XML;HDR=YES";'


-----------------------------------------------------
--create workflow steps 
-----------------------------------------------------
--set identity_insert dbo.ETLStep on

insert dbo.ETLStep
	(StepID,BatchID,StepName,StepDesc,StepProcID
	,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
select 1,@BatchID,'01','Excel Test',24,null,null,null,'01'

--set identity_insert dbo.ETLStep off

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)


	      select 1,@BatchID,'Action','MoveData'
union all select 1,@BatchID,'QueryTimeout','120'
union all select 1,@BatchID,'Source.Component','EXCEL'
union all select 1,@BatchID,'Source.EXCEL.AccessMode','Table or view'			
union all select 1,@BatchID,'Source.EXCEL.OpenRowset','Sheet1$'			
union all select 1,@BatchID,'Source.ConnectionString','<Test.ConnectionString>'			

union all select 1,@BatchID,'Destination.Component','EXCEL'
union all select 1,@BatchID,'Destination.EXCEL.AccessMode','Table or view'			
union all select 1,@BatchID,'Destination.ConnectionString','<Test.ConnectionString>'
union all select 1,@BatchID,'Destination.EXCEL.OpenRowset','Sheet2$'			
--union all select 1,@BatchID,'SavePackage','1'
--union all select 1,@BatchID,'PackageFileName','<Path*>\Test1026_Excel.dtsx'

union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'PRIGROUP','1'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
