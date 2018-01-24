set quoted_identifier on;
---////////////////////////////
--Controller Test Workflow
---///////////////////////////
/*
-----------------------------------------------------------
--This code will return XML representation of this workflow
-----------------------------------------------------------
declare @pHeader xml
declare @pContext xml
exec dbo.prc_CreateHeader @pHeader out,-10,5,null,4,1,15
exec dbo.prc_CreateContext @pContext out,@pHeader
select @pContext
*/

/*
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
select getdate()
exec dbo.prc_Execute 'TST_DE','debug;forcestart'
*/


set quoted_identifier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID =-10
set @BatchName = 'TST_DE' 

print ' Compiling Test DE'

begin try
-------------------------------------------------------
--remove Workflow metadata
-------------------------------------------------------
exec dbo.prc_RemoveContext @BatchName

-------------------------------------------------------
--Create workflow record 
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
          select @BatchID,'MAXTHREAD','4'
union all select @BatchID,'TIMEOUT','120'
union all select @BatchID,'LIFETIME','7200'
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
union all select @BatchID,'DW'			,'dbo.fn_systemparameter(''Environment'',''DW'',''<ENV*>'')'
union all select @BatchID,'Staging'		,'dbo.fn_systemparameter(''Environment'',''Staging'',''<ENV*>'')'
union all select @BatchID,'Path'		,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'


union all select @BatchID,'LocalServer','@@SERVERNAME'
union all select @BatchID,'StagingAreaRoot','<Path*>\DSV'
union all select @BatchID,'Control.Server','<LocalServer*>'
union all select @BatchID,'Control.Database','ETL_Controller'
union all select @BatchID,'DEPath','<Path*>\DeltaExtractor\deltaextractor.exe'

-------------------------------------------------------
--Create batch level constraints
-------------------------------------------------------

--ETLBatchConstraint
set identity_insert dbo.ETLBatchConstraint on
insert dbo.ETLBatchConstraint
(ConstID,BatchID,ProcessID,ConstOrder,WaitPeriod)
--wait 120 min for DPM_CopyCost to finish
select   1 ,@BatchID,12,'01',120
set identity_insert dbo.ETLBatchConstraint off

--ETLBatchConstraintAttribute
insert dbo.ETLBatchConstraintAttribute
(ConstID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'WatermarkEventType','DPM_LOADDIM_FINISHED'
union all select 1,@BatchID,'EventType','DPM_COPYDIM_FINISHED'
union all select 1,@BatchID,'EventServer','<Control.Server>'
union all select 1,@BatchID,'EventDatabase','ETL_Event'
union all select 1,@BatchID,'PING','30'
union all select 1,@BatchID,'DISABLED','1'

-------------------------------------------------------
--Create workflow steps 
-------------------------------------------------------
set identity_insert dbo.ETLStep on
insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select   1 ,@BatchID,'ST01','table to file',7,null,null,null,'01'
union all select   2 ,@BatchID,'ST02','file to table',7,null,null,null,'02'
union all select   3 ,@BatchID,'ST03','table to table',7,null,null,null,'03'
union all select   4 ,@BatchID,'ST04','file to file',7,null,null,null,'04'

union all select   5 ,@BatchID,'ST05','exec Sql',1,null,null,null,'05'
union all select   6 ,@BatchID,'ST06','exec Cmd',2,null,null,null,'06'
union all select   7 ,@BatchID,'ST07','exec Console',8,null,null,null,'07'

--test loop
--loop control
union all select   8 ,@BatchID,'ST08','Loop step 1  + condition step',1,null,null,null,'081'
union all select   9 ,@BatchID,'ST09','Loop step 2',1,null,null,null,'082'
union all select   10 ,@BatchID,'ST10','ouside the loop',1,null,null,null,'084'

--test ps
union all select   11 ,@BatchID,'ST11','exec Console with ps',8,null,null,null,'09'

--test event

--test partitioned output
union all select   12 ,@BatchID,'ST012','table to partitioned table',7,null,null,null,'12'
--test master/slave
union all select   14 ,@BatchID,'ST014','master/slave 01',1,null,null,null,'14'
union all select   15 ,@BatchID,'ST015','master/slave 02',1,null,null,null,'15'
union all select   16 ,@BatchID,'ST016','run package',1,null,null,null,'16'

set identity_insert dbo.ETLStep off

--select * from ETLProcess
-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','1'
union all select 1,@BatchID,'DISABLED','1'

-------------------------------------------------------
--Define step level user attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'Action','MoveData'
union all select 1,@BatchID,'Source.Component','OLEDB'
union all select 1,@BatchID,'Source.Server','SAJAZZSQL09'
union all select 1,@BatchID,'Source.Database','<YAP_Staging*>'
union all select 1,@BatchID,'Source.TableName','dbo.Audit'
union all select 1,@BatchID,'Source.Query','select top(100) * from dbo.Audit'

--union all select 1,@BatchID,'Destination.Component','XDestination'
--union all select 1,@BatchID,'Destination.StagingAreaTable','dbo_TestAudit'
--union all select 1,@BatchID,'Destination.X.FileName','C:\Users\andrey@biasintelligence.com\XTestAudit.txt'
--union all select 1,@BatchID,'Destination.X.DataFormat','CSV'
--union all select 1,@BatchID,'Destination.X.DataCompression','NONE'
--union all select 1,@BatchID,'Destination.X.ColumnDelimiter','9'

union all select 1,@BatchID,'Destination.Component','FlatFile'
union all select 1,@BatchID,'Destination.StagingAreaTable','dbo_TestAudit'
--union all select 1,@BatchID,'Destination.FlatFile.ConnectionString','\\sajazzsql08\MMN-Share\temp\TestData.txt'
union all select 1,@BatchID,'Destination.FlatFile.ConnectionString','e:\MMN-Share\temp\TestData.txt'
union all select 1,@BatchID,'Destination.FlatFile.ColumnDelimiter',','--'\t'
union all select 1,@BatchID,'Destination.FlatFile.TextQualifier','"'
union all select 1,@BatchID,'SavePackage','1'
union all select 1,@BatchID,'PackageFileName','e:\MMN-Share\temp\TestTableToFile.dtsx'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 2,@BatchID,'DISABLED','1'
--user
union all select 2,@BatchID,'Action','MoveData'
union all select 2,@BatchID,'Destination.Component','OLEDB'
union all select 2,@BatchID,'Destination.TableName','dbo.TestAudit'
union all select 2,@BatchID,'Destination.Server','SAJAZZSQL09'
union all select 2,@BatchID,'Destination.Database','<YAP_Staging*>'
union all select 2,@BatchID,'Destination.Staging','0'

--union all select 2,@BatchID,'Source.Component','XSource'
--union all select 2,@BatchID,'Source.StagingAreaTable','dbo_TestAudit'
--union all select 2,@BatchID,'Source.X.FileName','C:\Users\andrey@biasintelligence.com\XTestAudit.txt'
--union all select 2,@BatchID,'Source.X.DataFormat','CSV'
--union all select 2,@BatchID,'Source.X.DataCompression','NONE'
--union all select 2,@BatchID,'Source.X.ColumnDelimiter','9'

union all select 2,@BatchID,'Source.Component','FlatFile'
union all select 2,@BatchID,'Source.StagingAreaTable','dbo_TestAudit'
union all select 2,@BatchID,'Source.FlatFile.ConnectionString','e:\MMN-Share\temp\TestData.txt'
union all select 2,@BatchID,'Source.FlatFile.ColumnDelimiter',','--'\t'
union all select 2,@BatchID,'Source.FlatFile.TextQualifier','"'
union all select 2,@BatchID,'SavePackage','1'
union all select 2,@BatchID,'ForceStart','1'
union all select 2,@BatchID,'PackageFileName','e:\MMN-Share\temp\TestFileToTable.dtsx'

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 3,@BatchID,'DISABLED','1'
--user
union all select 3,@BatchID,'Action','MoveData'
union all select 3,@BatchID,'Source.Component','OLEDB'
union all select 3,@BatchID,'Source.TableName','dbo.Audit'
union all select 3,@BatchID,'Source.Server','<LocalServer*>'
union all select 3,@BatchID,'Source.Database','<DPM_Staging*>'
union all select 3,@BatchID,'Source.Query','select top(100) * from dbo.Audit'

union all select 3,@BatchID,'Destination.Component','OLEDB'
union all select 3,@BatchID,'Destination.TableName','<DPM_DW*>.dbo.TestAudit'
union all select 3,@BatchID,'Destination.Server','<LocalServer*>'
union all select 3,@BatchID,'Destination.Database','<DPM_Staging*>'
union all select 3,@BatchID,'Destination.StagingTableName','dbo.staging_TestAudit'
union all select 3,@BatchID,'Destination.Staging','1'
union all select 3,@BatchID,'SavePackage','1'
union all select 3,@BatchID,'ForceStart','1'
union all select 3,@BatchID,'PackageFileName','C:\Users\andrey@biasintelligence.com\TestAudiTableToTable.dtsx'

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 4,@BatchID,'DISABLED','1'
--user
union all select 4,@BatchID,'Action','MoveData'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 5,@BatchID,'DISABLED','0'
union all select 5,@BatchID,'PRIGROUP','1'
--user
union all select 5,@BatchID,'date','convert(varchar(8),getdate(),112)'
union all select 5,@BatchID,'SQL','waitfor delay ''00:01'''

--union all select 5,@BatchID,'SQL','
--declare @eventargs xml
--;with xmlnamespaces(''EventArgs.XSD'' as dwc)
--select @eventargs = 
--(select ''TEST.DPM_Event'' as ''@Source'',''Day'' as ''@PeriodGrain'',''<date*>'' as ''@Period''
--for xml path(''dwc:EventArgs''),type)
--exec dbo.ClrEventPost ''<Control.Server>'',''DPM_Event'',''DPM_LOADDIM_FINISHED'',null,@eventargs,''debug''
--'

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 6,@BatchID,'DISABLED','1'
--user

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 7,@BatchID,'DISABLED','1'
union all select 7,@BatchID,'PRIGROUP','1'
--user
--union all select 7,@BatchID,'CONSOLE','osql.exe'
--union all select 7,@BatchID,'ARG','-S "andrey@biasintelligence.com1" -d YAP_DW -E -Q"waitfor delay ''00:02''"'
union all select 7,@BatchID,'CONSOLE','cmd.exe'
union all select 7,@BatchID,'ARG','/C dir <file>'
union all select 7,@BatchID,'file','/C dir <file>'

--ETLStepConstraint
set identity_insert dbo.ETLStepConstraint on
insert dbo.ETLStepConstraint
(ConstID,BatchID,StepID,ProcessID,ConstOrder,WaitPeriod)
--wait <?> min for the files
          select   1 ,@BatchID,7,11,'01',2
set identity_insert dbo.ETLStepConstraint off

--ETLBatchConstraintAttribute
insert dbo.ETLStepConstraintAttribute
(ConstID,BatchID,StepID,AttributeName,AttributeValue)
          select 1,@BatchID,7,'CheckFile','<file>'
union all select 1,@BatchID,7,'PING','30'
union all select 1,@BatchID,7,'DISABLED','0'

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 8,@BatchID,'DISABLED','1'
union all select 8,@BatchID,'PRIGROUP','1'
union all select 8,@BatchID,'LOOPGROUP','loop1'
--user
union all select 8,@BatchID,'Cnt','0'
union all select 8,@BatchID,'SQL','
declare @cnt int
set @cnt = <Cnt> + 1
if (@cnt > 3)
begin
   exec prc_cdrCounterSet <@BatchID>,<@StepID>,<@RunID>,''BreakEvent'',''loop1''
   exec prc_cdrAttributeSet <@BatchID>,<@StepID>,null,''Cnt'',''0''
end
else
   exec prc_cdrAttributeSet <@BatchID>,<@StepID>,null,''Cnt'',@cnt
'

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 9,@BatchID,'DISABLED','1'
union all select 9,@BatchID,'PRIGROUP','1'
union all select 9,@BatchID,'LOOPGROUP','loop1'
--user
union all select 9,@BatchID,'SQL','print ''inside loop test'''

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 10,@BatchID,'DISABLED','1'
union all select 10,@BatchID,'PRIGROUP','2'
--user
union all select 10,@BatchID,'SQL','print ''outside loop test'''

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 11,@BatchID,'DISABLED','1'
union all select 11,@BatchID,'PRIGROUP','1'
--user
union all select 11,@BatchID,'Server','andrey@biasintelligence.com1'
union all select 11,@BatchID,'CONSOLE','sqlps.exe'
union all select 11,@BatchID,'ARG','&"
 $Check = get-WMIObject win32_pingstatus -Filter ''Address=''''<Server>'''''' | Select-Object StatusCode,IPV4Address;
 if ($Check.StatusCode -eq 0){
    write-host "::Ping Succeeded $Check.IPV4Address.IPAddressToString";
    $query = ''exec dbo.prc_etlCounterSet <@BatchID>,<@StepID>,<@RunID>,''''Ping'''',''''Success'''';'';
    } 
else {
    write-host "::Ping Failed $Check.StatusCode";
    $query = ''exec dbo.prc_etlCounterSet <@BatchID>,<@StepID>,<@RunID>,''''Ping'''',''''Failure'''';'';
    } 
invoke-sqlcmd -serverinstance <Control.Server> -database <Control.Database> -query $query -ErrorLevel 1 -AbortOnError;
 "'
--$query = ''exec dbo.prc_etlCounterSet <@BatchID>,<@StepID>,<@RunID>,''''Ping'''','''''' + $Check.IPV4Address.IPAddressToString + '''''''';

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 12,@BatchID,'DISABLED','1'
--user
union all select 12,@BatchID,'Action','MoveData'
union all select 12,@BatchID,'Source.Component','OLEDB'
union all select 12,@BatchID,'Source.Server','<LocalServer*>'
union all select 12,@BatchID,'Source.Database','YAP_DW'
union all select 12,@BatchID,'Source.Query','select  * from dbo.PartitionedInput'

union all select 12,@BatchID,'Destination.PartitionFunction','UPS'
union all select 12,@BatchID,'Destination.PartitionFunctionInput','PUID'
union all select 12,@BatchID,'Destination.PartitionFunctionOutput','PartitionId'

union all select 12,@BatchID,'Destination_01.Component','OLEDB'
union all select 12,@BatchID,'Destination_01.TableName','dbo.PartitionedOutput_P1'
union all select 12,@BatchID,'Destination_01.MinPartitionID','1'
union all select 12,@BatchID,'Destination_01.MaxPartitionID','12'
union all select 12,@BatchID,'Destination_01.Server','<LocalServer*>'
union all select 12,@BatchID,'Destination_01.Database','YAP_DW'
union all select 12,@BatchID,'Destination_01.UserOptions','reload'

union all select 12,@BatchID,'Destination_02.Component','OLEDB'
union all select 12,@BatchID,'Destination_02.TableName','dbo.PartitionedOutput_P2'
union all select 12,@BatchID,'Destination_02.MinPartitionID','13'
union all select 12,@BatchID,'Destination_02.MaxPartitionID','24'
union all select 12,@BatchID,'Destination_02.Server','<LocalServer*>'
union all select 12,@BatchID,'Destination_02.Database','YAP_DW'
union all select 12,@BatchID,'Destination_02.UserOptions','reload'

union all select 12,@BatchID,'Destination_03.Component','OLEDB'
union all select 12,@BatchID,'Destination_03.TableName','dbo.PartitionedOutput_P3'
union all select 12,@BatchID,'Destination_03.MinPartitionID','25'
union all select 12,@BatchID,'Destination_03.MaxPartitionID','36'
union all select 12,@BatchID,'Destination_03.Server','<LocalServer*>'
union all select 12,@BatchID,'Destination_03.Database','YAP_DW'
union all select 12,@BatchID,'Destination_03.UserOptions','reload'

union all select 12,@BatchID,'Destination_04.Component','OLEDB'
union all select 12,@BatchID,'Destination_04.TableName','dbo.PartitionedOutput_P4'
union all select 12,@BatchID,'Destination_04.MinPartitionID','37'
union all select 12,@BatchID,'Destination_04.MaxPartitionID','48'
union all select 12,@BatchID,'Destination_04.Server','<LocalServer*>'
union all select 12,@BatchID,'Destination_04.Database','YAP_DW'
union all select 12,@BatchID,'Destination_04.UserOptions','reload'

union all select 12,@BatchID,'SavePackage','1'
union all select 12,@BatchID,'ForceStart','1'
union all select 12,@BatchID,'PackageFileName','C:\Users\andrey@biasintelligence.com\Test\TableToTablePartitioned.dtsx'

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 14,@BatchID,'DISABLED','1'
union all select 14,@BatchID,'PRIGROUP','1'
union all select 14,@BatchID,'SVCNAME','CDRCONTROLLER_PROCESS_NODE01'
--user
union all select 14,@BatchID,'SQL','print ''step 14'''

insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
--system
          select 15,@BatchID,'DISABLED','1'
union all select 15,@BatchID,'PRIGROUP','1'
--user
union all select 15,@BatchID,'SQL','print ''step 15'''
union all select 15,@BatchID,'SVCNAME','ANY'

-------------------------------------------------------
--Define step level system attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 16,@BatchID,'PRIGROUP','1'
union all select 16,@BatchID,'DISABLED','0'

-------------------------------------------------------
--Define step level user attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 16,@BatchID,'Action','RunPackage'
union all select 16,@BatchID,'Package.File','C:\Users\andrey@biasintelligence.com\TestAudiTableToTable.dtsx'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
end catch
go
