/*

declare @Header xml
declare @pContext xml
declare @pProcessRequest xml
declare @pAttributes xml
exec dbo.prc_CreateHeader @Header out,-20,null,null,4,15
exec dbo.prc_CreateContext @pContext out,@Header
--exec dbo.prc_CreateProcessRequest @pProcessRequest out,@Header,@pContext
--select @pProcessRequest
--exec dbo.prc_ReadContextAttributes @pProcessRequest,@pAttributes out
--select @pAttributes
select @pContext
exec prc_PersistContext @pContext,'debug,replace'
exec dbo.prc_CreateContext @pContext out,@Header
select @pContext
rollback tran
*/
CREATE PROCEDURE dbo.prc_PersistContext
    @pContext xml([ETLController])
   ,@pHandle uniqueidentifier = null 
   ,@pOptions nvarchar(100) = null
As
/******************************************************************
**D File:         prc_PersistContext.SQL
**
**D Desc:         create persist context into ETLBatch tables
**
** @Options       debug,replace
** @pHandle       conversation handle to communicate messages back to main thread

**D Auth:         andreys
**D Date:         10/27/2007
**
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
01/12/2008           Praveen			Added Retry and Delay 83349
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ProcErr INT
DECLARE @Cnt INT
DECLARE @ProcName sysname
DECLARE @msg nvarchar(max)
DECLARE @trancount int

DECLARE @BatchID int
DECLARE @StepID int
DECLARE @ConstID int
DECLARE @RunID int
DECLARE @Options int
DECLARE @debug tinyint
DECLARE @replace tinyint
DECLARE @Handle uniqueidentifier
DECLARE @BatchName nvarchar(30)

declare @Name nvarchar(100)
declare @Value1 nvarchar(max)
declare @Value2 nvarchar(max)
declare @nValue nvarchar(max)

DECLARE @Header xml(ETLController)
DECLARE @Context xml(ETLController)
DECLARE @ProcessInfo xml(ETLController)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0
SET @trancount = @@trancount


begin try
set @debug = case when charindex('debug',@pOptions) > 0 then 1 else 0 end
set @replace = case when charindex('replace',@pOptions) > 0 then 1 else 0 end

;with xmlnamespaces('ETLController.XSD' as etl)
select @BatchID = @pContext.value('(/etl:Context/@BatchID)[1]','int')
      ,@BatchName = @pContext.value('(/etl:Context/@BatchName)[1]','nvarchar(30)')

exec [prc_CreateHeader] @Header out,@BatchID,null,null,0,@debug,15
if (@debug = 1)
begin
   SET @msg =  'BEGIN Procedure ' + @ProcName + ' for BatchName=' + isnull(@BatchName,'NULL')
            + ' (' + isnull(cast(@BatchID as nvarchar(10)),'NULL') + ')'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end


if exists (select 1 from dbo.[ETLBatch] where BatchName = @BatchName and BatchID <> @BatchID)
BEGIN
   SET @Err = 50101
   SET @msg = '   ERROR pContext: BatchName=' + @BatchName + ' already exists with different BatchID'
   RAISERROR(@msg,11,11) 
END

--b shred
;with xmlnamespaces('ETLController.XSD' as etl)
select
       cb.b.value('(./@BatchID)[1]','int') as BatchID
      ,cb.b.value('(./@BatchName)[1]','nvarchar(30)') as BatchName
      ,cb.b.value('(./@BatchDesc)[1]','nvarchar(500)') as BatchDesc
      ,cb.b.value('(./etl:OnSuccess/@ProcessID)[1]','int') as OnSuccessID
      ,cb.b.value('(./etl:OnFailure/@ProcessID)[1]','int') as OnFailureID
      ,cb.b.value('(./@IgnoreErr)[1]','tinyint') as IgnoreErr
      ,cb.b.value('(./@Restart)[1]','tinyint') as RestartOnErr
      ,cb.b.value('(./@MaxThread)[1]','tinyint') as MaxThread
      ,cb.b.value('(./@Timeout)[1]','int') as [Timeout]
      ,cb.b.value('(./@Lifetime)[1]','int') as Lifetime
      ,cb.b.value('(./@Ping)[1]','tinyint') as Ping
      ,cb.b.value('(./@HistRet)[1]','int') as HistRet
      ,cb.b.value('(./@Retry)[1]','int') as Retry
      ,cb.b.value('(./@Delay)[1]','int') as [Delay]
  into #b
  from @pContext.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Shredding B:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--ba shred
;with xmlnamespaces('ETLController.XSD' as etl)
select
       cb.b.value('(./@BatchID)[1]','int') as BatchID
      ,cba.ba.value('(./@Name)[1]','nvarchar(100)') as AttributeName
      ,cba.ba.value('(.)[1]','nvarchar(4000)') as AttributeValue
  into #ba
  from @pContext.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)
  cross apply cb.b.nodes('./etl:Attributes/etl:Attribute') cba(ba)
  union select BatchID,'MAXTHREAD',cast(MaxThread as nvarchar(1000)) from #b where MaxThread is not null
  union select BatchID,'TIMEOUT',cast([Timeout] as nvarchar(1000)) from #b where [Timeout] is not null
  union select BatchID,'LIFETIME',cast(Lifetime as nvarchar(1000)) from #b where Lifetime is not null
  union select BatchID,'PING',cast(Ping as nvarchar(1000)) from #b where Ping is not null
  union select BatchID,'HISTRET',cast(HistRet as nvarchar(1000)) from #b where HistRet is not null
  union select BatchID,'RETRY',cast(Retry as nvarchar(1000)) from #b where Retry is not null
  union select BatchID,'DELAY',cast([Delay] as nvarchar(1000)) from #b where [Delay] is not null

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Shredding BA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end


;with xmlnamespaces('ETLController.XSD' as etl)
select
       cb.b.value('(./@BatchID)[1]','int') as BatchID
      ,cbc.bc.value('(./@ConstID)[1]','int') as ConstID
      ,cbc.bc.value('(./etl:Process/@ProcessID)[1]','int') as ProcessID
      ,cbc.bc.value('(./@ConstOrder)[1]','nvarchar(10)') as ConstOrder
      ,cbc.bc.value('(./@WaitPeriod)[1]','int') as WaitPeriod
      ,cbc.bc.value('(./@Disabled)[1]','tinyint') as [Disabled]
      ,cbc.bc.value('(./@Ping)[1]','int') as Ping
  into #bc
  from @pContext.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)
  cross apply cb.b.nodes('./etl:Constraints/etl:Constraint') cbc(bc)
set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Shredding BC:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

;with xmlnamespaces('ETLController.XSD' as etl)
select
       cb.b.value('(./@BatchID)[1]','int') as BatchID
      ,cbc.bc.value('(./@ConstID)[1]','int') as ConstID
      ,cbca.bca.value('(./@Name)[1]','nvarchar(100)') as AttributeName
      ,cbca.bca.value('(.)[1]','nvarchar(4000)') as AttributeValue
  into #bca
  from @pContext.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)
  cross apply cb.b.nodes('./etl:Constraints/etl:Constraint') cbc(bc)
  cross apply cbc.bc.nodes('./etl:Attributes/etl:Attribute') cbca(bca)
  union select BatchID,ConstID,'DISABLED',cast([Disabled] as nvarchar(1000)) from #bc where [Disabled] is not null
  union select BatchID,ConstID,'PING',cast(Ping as nvarchar(1000)) from #bc where Ping is not null

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Shredding BCA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--s shred
;with xmlnamespaces('ETLController.XSD' as etl)
select
       cb.b.value('(./@BatchID)[1]','int') as BatchID
      ,cs.s.value('(./@StepID)[1]','int') as StepID
      ,cs.s.value('(./@StepName)[1]','nvarchar(100)') as StepName
      ,cs.s.value('(./@StepDesc)[1]','nvarchar(500)') as StepDesc
      ,cs.s.value('(./etl:Process/@ProcessID)[1]','int') as StepProcID
      ,cs.s.value('(./etl:OnSuccess/@ProcessID)[1]','int') as OnSuccessID
      ,cs.s.value('(./etl:OnFailure/@ProcessID)[1]','int') as OnFailureID
      ,cs.s.value('(./@IgnoreErr)[1]','tinyint') as IgnoreErr
      ,cs.s.value('(./@Restart)[1]','tinyint') as RestartOnErr
      ,cs.s.value('(./@StepOrder)[1]','nvarchar(10)') as StepOrder
      ,cs.s.value('(./@Disabled)[1]','tinyint') as [Disabled]
      ,cs.s.value('(./@SeqGroup)[1]','int') as SeqGroup
      ,cs.s.value('(./@PriGroup)[1]','int') as PriGroup
      ,cs.s.value('(./@Retry)[1]','int') as Retry
      ,cs.s.value('(./@Delay)[1]','int') as [Delay]
  into #s
  from @pContext.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)
  cross apply cb.b.nodes('./etl:Steps/etl:Step') cs(s)

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Shredding S:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--sa shred
;with xmlnamespaces('ETLController.XSD' as etl)
select
       cb.b.value('(./@BatchID)[1]','int') as BatchID
      ,cs.s.value('(./@StepID)[1]','int') as StepID
      ,csa.sa.value('(./@Name)[1]','nvarchar(100)') as AttributeName
      ,csa.sa.value('(.)[1]','nvarchar(4000)') as AttributeValue
  into #sa
  from @pContext.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)
  cross apply cb.b.nodes('./etl:Steps/etl:Step') cs(s)
  cross apply cs.s.nodes('./etl:Attributes/etl:Attribute') csa(sa)
  union select BatchID,StepID,'DISABLED',cast([Disabled] as nvarchar(1000)) from #s where [Disabled] is not null
  union select BatchID,StepID,'SEQGROUP',cast(SeqGroup as nvarchar(1000)) from #s where SeqGroup is not null
  union select BatchID,StepID,'PRIGROUP',cast(PriGroup as nvarchar(1000)) from #s where PriGroup is not null
  union select BatchID,StepID,'RETRY',cast(Retry as nvarchar(1000)) from #s where Retry is not null
  union select BatchID,StepID,'DELAY',cast([Delay] as nvarchar(1000)) from #s where [Delay] is not null
  union select BatchID,StepID,'RESTART',cast(RestartOnErr as nvarchar(1000)) from #s where RestartOnErr is not null

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Shredding SA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--sc shred
;with xmlnamespaces('ETLController.XSD' as etl)
select
       cb.b.value('(./@BatchID)[1]','int') as BatchID
      ,cs.s.value('(./@StepID)[1]','int') as StepID
      ,csc.sc.value('(./@ConstID)[1]','int') as ConstID
      ,csc.sc.value('(./etl:Process/@ProcessID)[1]','int') as ProcessID
      ,csc.sc.value('(./@ConstOrder)[1]','nvarchar(10)') as ConstOrder
      ,csc.sc.value('(./@WaitPeriod)[1]','int') as WaitPeriod
      ,csc.sc.value('(./@Disabled)[1]','tinyint') as [Disabled]
      ,csc.sc.value('(./@Ping)[1]','int') as Ping
  into #sc
  from @pContext.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)
  cross apply cb.b.nodes('./etl:Steps/etl:Step') cs(s)
  cross apply cs.s.nodes('./etl:Constraints/etl:Constraint') csc(sc)

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Shredding SC:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--sca shred
;with xmlnamespaces('ETLController.XSD' as etl)
select
       cb.b.value('(./@BatchID)[1]','int') as BatchID
      ,cs.s.value('(./@StepID)[1]','int') as StepID
      ,csc.sc.value('(./@ConstID)[1]','int') as ConstID
      ,csca.sca.value('(./@Name)[1]','nvarchar(100)') as AttributeName
      ,csca.sca.value('(.)[1]','nvarchar(4000)') as AttributeValue
  into #sca
  from @pContext.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)
  cross apply cb.b.nodes('./etl:Steps/etl:Step') cs(s)
  cross apply cs.s.nodes('./etl:Constraints/etl:Constraint') csc(sc)
  cross apply csc.sc.nodes('./etl:Attributes/etl:Attribute') csca(sca)
  union select BatchID,ConstID,ConstID,'DISABLED',cast([Disabled] as nvarchar(1000)) from #sc where [Disabled] is not null
  union select BatchID,ConstID,ConstID,'PING',cast(Ping as nvarchar(1000)) from #sc where Ping is not null

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Shredding SCA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

BEGIN TRAN

if (@Replace = 1)
begin

   exec @ProcErr = dbo.[prc_RemoveContext] @BatchName,@pHandle,@pOptions

   if (@debug = 1)
   begin
      SET @msg =  'Deleted all old records (Replace=1)...'
      exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
      exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
   end

end

--b persist
update d
   set
       d.BatchName = t.BatchName
      ,d.BatchDesc = t.BatchDesc
      ,d.OnSuccessID = t.OnSuccessID
      ,d.OnFailureID = t.OnFailureID
      ,d.IgnoreErr = t.IgnoreErr
      ,d.RestartOnErr = t.RestartOnErr
  from dbo.[ETLBatch] d
  join #b t on d.BatchID = t.BatchID

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Updated B:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

set identity_insert dbo.[ETLBatch] on
insert dbo.[ETLBatch]
    (BatchID,BatchName,BatchDesc,OnSuccessID,OnFailureID,IgnoreErr,RestartOnErr)
select
       t.BatchID
      ,t.BatchName
      ,t.BatchDesc
      ,t.OnSuccessID
      ,t.OnFailureID
      ,t.IgnoreErr
      ,t.RestartOnErr
  from #b t
  left join dbo.[ETLBatch] d on t.BatchID = d.BatchID
 where d.BatchID is null
set @cnt = @@ROWCOUNT
set identity_insert dbo.[ETLBatch] off

if (@debug = 1)
begin
   SET @msg =  'Inserted B:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--ba persist
update d
   set
       d.AttributeValue = t.AttributeValue
  from dbo.[ETLBatchAttribute] d
  join #ba t on d.BatchID = t.BatchID and d.AttributeName = t.AttributeName

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Updated BA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

insert dbo.[ETLBatchAttribute]
    (BatchID,AttributeName,AttributeValue)
select
       t.BatchID
      ,t.AttributeName
      ,t.AttributeValue
  from #ba t
  left join dbo.[ETLBatchAttribute] d on t.BatchID = d.BatchID and t.AttributeName = d.AttributeName
 where d.BatchID is null

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Inserted B:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--bc persist
update d
   set
       d.ProcessID = t.ProcessID
      ,d.ConstOrder = t.ConstOrder
      ,d.WaitPeriod = t.WaitPeriod
  from dbo.[ETLBatchConstraint] d
  join #bc t on d.BatchID = t.BatchID and d.ConstID = t.ConstID

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Updated BC:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

set identity_insert dbo.[ETLBatchConstraint] on
insert dbo.[ETLBatchConstraint]
    (BatchID,ConstID,ProcessID,ConstOrder,WaitPeriod)
select
       t.BatchID
      ,t.ConstID
      ,t.ProcessID
      ,t.ConstOrder
      ,t.WaitPeriod
  from #bc t
  left join dbo.[ETLBatchConstraint] d on t.BatchID = d.BatchID and d.ConstID = t.ConstID
 where d.BatchID is null
set @cnt = @@ROWCOUNT
set identity_insert dbo.[ETLBatchConstraint] off

if (@debug = 1)
begin
   SET @msg =  'Inserted BC:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--bca persist
update d
   set
       d.AttributeValue = t.AttributeValue
  from dbo.[ETLBatchConstraintAttribute] d
  join #bca t on t.BatchID = d.BatchID and t.ConstID = d.ConstID and t.AttributeName = d.AttributeName

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Updated BCA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

insert dbo.[ETLBatchConstraintAttribute]
    (BatchID,ConstID,AttributeName,AttributeValue)
select
       t.BatchID
      ,t.ConstID
      ,t.AttributeName
      ,t.AttributeValue
  from #bca t
  left join dbo.[ETLBatchConstraintAttribute] d on t.BatchID = d.BatchID and t.ConstID = d.ConstID and t.AttributeName = d.AttributeName
 where d.BatchID is null

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Inserted BCA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--s persist
update d
   set
       d.StepName = t.StepName
      ,d.StepDesc = t.StepDesc
      ,d.StepProcID = t.StepProcID
      ,d.OnSuccessID = t.OnSuccessID
      ,d.OnFailureID = t.OnFailureID
      ,d.IgnoreErr = t.IgnoreErr
      ,d.StepOrder = t.StepOrder
  from dbo.[ETLStep] d
  join #s t on d.BatchID = t.BatchID and t.StepID = d.StepID

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Updated S:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

set identity_insert dbo.[ETLStep] on
insert dbo.[ETLStep]
    (BatchID,StepID,StepName,StepDesc,StepProcID,OnSuccessID,OnFailureID,IgnoreErr,StepOrder)
select
       t.BatchID
      ,t.StepID
      ,t.StepName
      ,t.StepDesc
      ,t.StepProcID
      ,t.OnSuccessID
      ,t.OnFailureID
      ,t.IgnoreErr
      ,t.StepOrder
  from #s t
  left join dbo.[ETLStep] d on t.BatchID = d.BatchID
 where d.BatchID is null

set @cnt = @@ROWCOUNT
set identity_insert dbo.[ETLStep] off

if (@debug = 1)
begin
   SET @msg =  'Inserted S:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--sa persist
update d
   set
       d.AttributeValue = t.AttributeValue
  from dbo.[ETLStepAttribute] d
  join #sa t on d.BatchID = t.BatchID and d.StepID = t.StepID and d.AttributeName = t.AttributeName

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Updated SA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

insert dbo.[ETLStepAttribute]
    (BatchID,StepID,AttributeName,AttributeValue)
select
       t.BatchID
      ,t.StepID
      ,t.AttributeName
      ,t.AttributeValue
  from #sa t
  left join dbo.[ETLStepAttribute] d on t.BatchID = d.BatchID and d.StepID = t.StepID and t.AttributeName = d.AttributeName
 where d.BatchID is null

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Inserted SA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--sc persist
update d
   set
       d.ProcessID = t.ProcessID
      ,d.ConstOrder = t.ConstOrder
      ,d.WaitPeriod = t.WaitPeriod
  from dbo.[ETLStepConstraint] d
  join #sc t on d.BatchID = t.BatchID and d.StepID = t.StepID and d.ConstID = t.ConstID

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Updated SC:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

set identity_insert dbo.[ETLStepConstraint] on
insert dbo.[ETLStepConstraint]
    (BatchID,StepID,ConstID,ProcessID,ConstOrder,WaitPeriod)
select
       t.BatchID
      ,t.StepID
      ,t.ConstID
      ,t.ProcessID
      ,t.ConstOrder
      ,t.WaitPeriod
  from #sc t
  left join dbo.[ETLStepConstraint] d on t.BatchID = d.BatchID and d.StepID = t.StepID and d.ConstID = t.ConstID
 where d.BatchID is null

set @cnt = @@ROWCOUNT
set identity_insert dbo.[ETLStepConstraint] off

if (@debug = 1)
begin
   SET @msg =  'Inserted SC:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--sca persist
update d
   set
       d.AttributeValue = t.AttributeValue
  from dbo.[ETLStepConstraintAttribute] d
  join #sca t on t.BatchID = d.BatchID and d.StepID = t.StepID and t.ConstID = d.ConstID and t.AttributeName = d.AttributeName

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Updated SCA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

insert dbo.[ETLStepConstraintAttribute]
    (BatchID,StepID,ConstID,AttributeName,AttributeValue)
select
       t.BatchID
      ,t.StepID
      ,t.ConstID
      ,t.AttributeName
      ,t.AttributeValue
  from #sca t
  left join dbo.[ETLStepConstraintAttribute] d on t.BatchID = d.BatchID and d.StepID = t.StepID and t.ConstID = d.ConstID and t.AttributeName = d.AttributeName
 where d.BatchID is null

set @cnt = @@ROWCOUNT
if (@debug = 1)
begin
   SET @msg =  'Inserted CSA:' + cast(@cnt as nvarchar(10)) + ' rows'
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

COMMIT TRAN

IF (@debug = 1)
BEGIN
   SET @msg = 'END Procedure ' + @ProcName
   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
END

end try
begin catch
   if @Trancount < @@trancount
      ROLLBACK TRAN

   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   raiserror (@msg,11,11)
end catch

RETURN @Err