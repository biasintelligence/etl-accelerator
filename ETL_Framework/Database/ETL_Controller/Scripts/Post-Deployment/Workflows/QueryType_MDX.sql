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
exec dbo.prc_createHeader @pHeader out,13,null,null,4,1,15
exec dbo.prc_CreateContext @pContext out,@pHeader
select @pContext
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'QueryType_MDX','debug;forcestart'
*/

set nocount on
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 13
set @BatchName =  'QueryType_MDX'

print ' Compiling ' + @BatchName + ' ...'

begin try

exec dbo.prc_RemoveContext @BatchName

set identity_insert dbo.ETLBatch on

insert dbo.ETLBatch
(BatchID,BatchName,BatchDesc
,OnSuccessID,OnFailureID,IgnoreErr,RestartOnErr
)
select @BatchID,@BatchName,'QueryType_MDX',null,5,0,1

set identity_insert dbo.ETLBatch off


insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
          select @BatchID,'ENV'				,'dbo.fn_systemparameter(''Environment'',''Current'',''ALL'')'
union all select @BatchID,'Path'			,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'
union all select @BatchID,'OLAP_Server'		,'isnull(dbo.fn_systemparameter(''Environment'',''OLAP_Server'',''<ENV*>''),@@SERVERNAME)'
union all select @BatchID,'Test_Cube'		,'isnull(dbo.fn_systemparameter(''Environment'',''Test_Cube'',''<ENV*>''),''Members'')'

union all select @BatchID,'MAXTHREAD','4'
union all select @BatchID,'TIMEOUT','360'
union all select @BatchID,'LIFETIME','3600'
union all select @BatchID,'PING','30'
union all select @BatchID,'HISTRET','100'
union all select @BatchID,'RETRY','2'
union all select @BatchID,'DELAY','10'
union all select @BatchID,'LocalServer','@@SERVERNAME'
union all select @BatchID,'LocalDB','db_name()'
union all select @BatchID,'Control.Server','<LocalServer*>'
union all select @BatchID,'Control.Database','<LocalDB*>'
union all select @BatchID,'DEPath'			,'<Path*>\DeltaExtractor\DeltaExtractor64.exe'

--deltaextractor options
union all select @BatchID,'ForceStart','1'

----ETLStep
set identity_insert dbo.ETLStep on
insert dbo.ETLStep
(StepID,BatchID,StepName,StepDesc,StepProcID
,OnSuccessID,OnFailureID,IgnoreErr,StepOrder
)
          select  1 ,@BatchID,'ST01','Run MDX query',7,null,null,null,'01'
union all select  2 ,@BatchID,'ST02','Test MDX query',1,null,null,null,'02'

set identity_insert dbo.ETLStep off


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','1'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'RETRY','0'
union all select 1,@BatchID,'DELAY','30'
union all select 1,@BatchID,'Action','MoveData'
--set source attributes
union all select 1,@BatchID,'Source.Component','OLEDB'
union all select 1,@BatchID,'Source.Server','<OLAP_Server*>'
union all select 1,@BatchID,'Source.Database','<Test_Cube*>'
union all select 1,@BatchID,'Source.QueryType','MDX'
union all select 1,@BatchID,'Source.Query','
select	{([Measures].[Amount])} on columns,
		{[Dim Member].[Member Name].Members} on rows
from	[Members]'
--set destination attributes
union all select 1,@BatchID,'Destination.Component','OLEDB'
union all select 1,@BatchID,'Destination.TableName','dbo.MemberTotalOrderAmount'
union all select 1,@BatchID,'Destination.Server','<LocalServer*>'
union all select 1,@BatchID,'Destination.Database','<LocalDB*>'
union all select 1,@BatchID,'Destination.Staging','0'
union all select 1,@BatchID,'Destination.UserOptions','reload'
union all select 1,@BatchID,'Destination.OLEDB.AccessMode','OpenRowset Using FastLoad'
union all select 1,@BatchID,'Destination.OLEDB.FastLoadOptions','TABLOCK,ROWS_PER_BATCH = 10000'
--union all select 1,@BatchID,'SavePackage'						,'1'
--union all select 1,@BatchID,'PackageFileName'					,'C:\Builds\DSTS_Packages\MDX.dtsx'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 5,@BatchID,'SQL',
'declare @Count int
select @Count = count(*) from dbo.MemberTotalOrderAmount
if (@Count <> 4)
  RAISERROR (''MDX query moved %i rows but it should have moved 4 rows'', 11,17,@Count);'
union all select 2,@BatchID,'PRIGROUP','2'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'RETRY','0'
union all select 2,@BatchID,'DELAY','30'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
 
end catch
go



