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
exec dbo.prc_createHeader @pHeader out,4,null,null,4,1,15
exec dbo.prc_CreateContext @pContext out,@pHeader
select @pContext
-----------------------------------------------------------
--Executing this workflow
-----------------------------------------------------------
exec dbo.prc_Execute 'Loop','debug;forcestart'
*/


set nocount on
Declare @Batchid int,@BatchName nvarchar(100) 
set @BatchID = 4
set @BatchName =  'Loop'

print ' Compiling ' + @BatchName + ' ...'

begin try

exec dbo.prc_RemoveContext @BatchName

set identity_insert dbo.ETLBatch on
insert dbo.ETLBatch
(BatchID,BatchName,BatchDesc
,OnSuccessID,OnFailureID,IgnoreErr,RestartOnErr
)
select @BatchID,@BatchName,'Loop',null,null,0,1

set identity_insert dbo.ETLBatch off


insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)

          select @BatchID,'ENV'				,'dbo.fn_systemparameter(''Environment'',''Current'',''ALL'')'
union all select @BatchID,'Path'			,'dbo.fn_systemparameter(''Environment'',''BuildLocation'',''<ENV*>'')'
union all select @BatchID,'LoopStart'		,'isnull(dbo.fn_systemparameter(''Environment'',''LoopStart'',''<ENV*>''),5)'
union all select @BatchID,'LoopEnd'			,'isnull(dbo.fn_systemparameter(''Environment'',''LoopEnd'',''<ENV*>''),9)'
union all select @BatchID,'Dest_Server'		,'isnull(dbo.fn_systemparameter(''Environment'',''Dest_Server'',''<ENV*>''),@@SERVERNAME)'
union all select @BatchID,'Dest_DB'			,'isnull(dbo.fn_systemparameter(''Environment'',''Dest_DB'',''<ENV*>''),''Dest_DB'')'


union all select @BatchID,'MAXTHREAD'		,'4'
union all select @BatchID,'TIMEOUT'			,'180'
union all select @BatchID,'LIFETIME'		,'3600'
union all select @BatchID,'PING'			,'30'
union all select @BatchID,'HISTRET'			,'100'
union all select @BatchID,'RETRY'			,'1'
union all select @BatchID,'DELAY'			,'10'
union all select @BatchID,'CurrentLoop'		,'0'
union all select @BatchID,'reset'			,'0'
union all select @BatchID,'LocalServer'		,'@@SERVERNAME'
union all select @BatchID,'LocalDB'			,'db_name()'
union all select @BatchID,'Control.Server'	,'<LocalServer*>'
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
          select 1,@BatchID,'ST01','Create Dest DB',1,null,null,1,'01' 
union all select 2,@BatchID,'ST02','create test table',8,null,null,null,'02'
union all select 3,@BatchID,'ST03','create test SP',8,null,null,null,'03'        
union all select 4,@BatchID,'ST04','reset runtime attributes',1,null,null,null,'04'
union all select 5,@BatchID,'ST05','call SP to insert data into test table',8,null,null,null,'05'
union all select 6,@BatchID,'ST06','set next period and check exit criteria',1,null,null,null,'06'
union all select 7,@BatchID,'ST07','test Looping',8,null,null,null,'07'
union all select 8,@BatchID,'ST08','Drop Dest DB',1,null,null,null,'08' 
  

set identity_insert dbo.ETLStep off


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 1,@BatchID,'PRIGROUP','1'
union all select 1,@BatchID,'SEQGROUP','1'
union all select 1,@BatchID,'DISABLED','0'
union all select 1,@BatchID,'SQL','
If not exists (select name from sys.databases where name = N''Dest_DB'')
	create database <Dest_DB*>;'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 2,@BatchID,'PRIGROUP','1'
union all select 2,@BatchID,'SEQGROUP','1'
union all select 2,@BatchID,'DISABLED','0'
union all select 2,@BatchID,'Query','
If exists (select * from sys.objects where object_id = object_id(N''dbo.LoopTest'') AND type in (N''U''))
   drop table dbo.LoopTest;
create table dbo.LoopTest(CurrentLoop int NOT NULL, RunId int NOT NULL, BatchId int NOT NULL, StepId int NOT NULL);'
union all select 2,@BatchID,'CONSOLE','sqlcmd'
union all select 2,@BatchID,'Command_SQL','-S <Dest_Server*> -d <Dest_DB*> -E -b -Q"<Query>"'
union all select 2,@BatchID,'ARG','<Command_SQL>'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 3,@BatchID,'PRIGROUP','1'
union all select 3,@BatchID,'SEQGROUP','1'
union all select 3,@BatchID,'DISABLED','0'
union all select 3,@BatchID,'Query','
If  exists (select * FROM sys.objects where object_id = OBJECT_ID(N''[dbo].[InsertLoopTest]'') AND type in (N''P'', N''PC''))
drop procedure [dbo].[InsertLoopTest];
go
create proc InsertLoopTest (@CurrentLoop int, @RunId int, @BatchId int, @StepId int)
as insert dbo.LoopTest select @CurrentLoop, @RunId, @BatchId, @StepId'
union all select 3,@BatchID,'CONSOLE','sqlcmd'
union all select 3,@BatchID,'Command_SQL','-S <Dest_Server*> -d <Dest_DB*> -E -b -Q"<Query>"'
union all select 3,@BatchID,'ARG','<Command_SQL>'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 4,@BatchID,'SQL',
'declare @cn nvarchar(100),@ds int;
select @ds = <LoopStart*>

if (<reset> = 1) 
      exec prc_ETLAttributeSet <@BatchID>,null,null,''reset'',''0'';

if (<CurrentLoop> > 0) and (<CurrentLoop> < <LoopEnd*>)
   set @ds = <CurrentLoop>;
  
exec prc_ETLAttributeSet <@BatchID>,null,null,''CurrentLoop'',@ds;       

set @cn = ''Loop '' + cast(@ds as nvarchar(10));
exec prc_ETLCounterSet <@BatchID>,null,<@RunID>,@cn,''started'';'
union all select 4,@BatchID,'PRIGROUP','1'
union all select 4,@BatchID,'SEQGROUP','1'
union all select 4,@BatchID,'DISABLED','0'
union all select 4,@BatchID,'RETRY','0'
union all select 4,@BatchID,'DELAY','30'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 5,@BatchID,'PRIGROUP','1'
union all select 5,@BatchID,'SEQGROUP','1'
union all select 5,@BatchID,'DISABLED','0'
union all select 5,@BatchID,'Query','exec InsertLoopTest @CurrentLoop=<CurrentLoop>, @RunID=<@RunID>, @BatchID=<@BatchID>, @StepID=<@StepID>'
union all select 5,@BatchID,'CONSOLE','sqlcmd'
union all select 5,@BatchID,'Command_SQL','-S <Dest_Server*> -d <Dest_DB*> -E -b -Q"<Query>"'
union all select 5,@BatchID,'ARG','<Command_SQL>'
union all select 5,@BatchID,'RETRY','0'
union all select 5,@BatchID,'DELAY','30'
union all select 5,@BatchID,'LOOPGROUP','1'


-- loop configurations for paramters
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 6,@BatchID,'SQL',
'declare @fds int,@cn nvarchar(100);
exec dbo.prc_ETLCounterSet <@BatchID>,null,<@RunID>,''Loop <CurrentLoop>'',''finished'';

select @fds = <CurrentLoop> + 1;

exec dbo.prc_ETLAttributeSet <@BatchID>,null,null,''CurrentLoop'',@fds;       
if (@fds > <LoopEnd*>)
begin
   exec dbo.prc_ETLAttributeSet <@BatchID>,null,null,''CurrentLoop'',''0'';       
   exec dbo.prc_ETLCounterSet <@BatchID>,<@StepID>,<@RunID>,''BreakEvent'',''1'';
end
else
begin
   set @cn = ''Loop '' + cast(@fds as nvarchar(10));
   exec dbo.prc_ETLCounterSet <@BatchID>,null,<@RunID>,@cn,''started'';
end'         
union all select 6,@BatchID,'PRIGROUP','2'
union all select 6,@BatchID,'DISABLED','0'
union all select 6,@BatchID,'RETRY','0'
union all select 6,@BatchID,'DELAY','30'
union all select 6,@BatchID,'LOOPGROUP','1'


insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 7,@BatchID,'PRIGROUP','3'
union all select 7,@BatchID,'DISABLED','0'
union all select 7,@BatchID,'Query','If (select count(*) from LoopTest) <> 5 
	RAISERROR (''LoopTest does not have correct number of rows'', 11,17)'
union all select 7,@BatchID,'CONSOLE','sqlcmd'
union all select 7,@BatchID,'Command_SQL','-S <Dest_Server*> -d <Dest_DB*> -E -b -Q"<Query>"'
union all select 7,@BatchID,'ARG','<Command_SQL>'
		
		
insert dbo.ETLStepAttribute
(StepID,BatchID,AttributeName,AttributeValue)
          select 8,@BatchID,'PRIGROUP','4'
union all select 8,@BatchID,'DISABLED','0'
union all select 8,@BatchID,'SQL','
If exists (select name from sys.databases where name = N''Dest_DB'')
	drop database [Dest_DB];'


end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = error_message()
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg)
 
end catch
go

