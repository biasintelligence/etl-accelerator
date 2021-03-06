﻿/*
select * from ETLBatch
select * from ETLStep where batchid = -320
prc_ETLAttributeSet 60,null,null,'DsvVersion','2'
prc_ETLAttributeSet 60,3,null,'DsvObject','LastHourMR'
prc_ETLAttributeSet 60,2,null,'DsvObject','AdType'
declare @pHeader xml
declare @pContext xml
exec dbo.prc_CreateHeader @pHeader out,340,2,null,4,1,15
--select @pHeader
exec dbo.prc_CreateContext @pContext out,@pHeader
select @pContext
*/
CREATE PROCEDURE [dbo].[prc_CreateContext]
    @pContext xml([ETLController]) output
   ,@pHeader xml([ETLController])
As
/******************************************************************
**D File:         prc_CreateContext.SQL
**
**D Desc:         return Context object
**
**D Auth:         andreys
**D Date:         10/27/2007
**
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
**  5/20/2008        andreys            remove context resolution
**  10/15/2011       andreys            add controller and node globals
**  2/3/2018         andrey             set unknown process default to 0
**  2/20/2018        andrey             add Disabled to batch level
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ProcErr INT
DECLARE @Cnt INT
DECLARE @ProcName sysname
DECLARE @msg nvarchar(max)

declare @BatchID int
declare @StepID int
declare @ConstID int
declare @RunID int
declare @Options int
declare @debug int
declare @Scope int
declare @AttributeScope int

declare @id int
declare @type tinyint
declare @b int
declare @s int
declare @c int
declare @Name nvarchar(100)
declare @Value1 nvarchar(max)
declare @Value2 nvarchar(max)
declare @nValue nvarchar(max)
declare @ProcessInfo xml(ETLController)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0

declare @a as table
 (id int identity(1,1)
 ,BatchID int
 ,StepID int null
 ,ConstID int null
 ,AttributeName nvarchar(100)
 ,AttributeValue nvarchar(max)
 ,AttributeScope tinyint
 ,CompleteFlag tinyint null
 ,unique (BatchID,StepID,ConstId,AttributeName,AttributeScope)
)

declare @ab as table
 (aid int
 ,bid int
 ,isexec tinyint
)

begin try
exec @ProcErr = dbo.[prc_ReadHeader] @pHeader,@BatchID out,@StepID out,@ConstID out,@RunID out,@Options out,@Scope out
set @debug = nullif(@Options & 1,0)


if (@debug = 1)
begin
   SET @msg =  'BEGIN Procedure ' + @ProcName + ' with context'
         + ' BatchID=' + isnull(cast(@BatchID as nvarchar(10)),'null')
         + ' StepID=' + isnull(cast(@StepID as nvarchar(10)),'null')
         + ' ConstID=' + isnull(cast(@ConstID as nvarchar(10)),'null')
         + ' RunID=' + isnull(cast(@RunID as nvarchar(10)),'null')
         + ' Scope=' + isnull(cast(@Scope as nvarchar(10)),'null')

   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@pHeader,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo
end
-------------------------------------------------------------------
--Process context attributes
-------------------------------------------------------------------
if (@StepID is null and (@Scope is null or @Scope & 4 = 4))
begin
   insert @a (BatchID,StepID,ConstID,AttributeName,AttributeValue,AttributeScope)
   select BatchID,null,ConstID,AttributeName,AttributeValue,1
     from dbo.[ETLBatchConstraintAttribute]
    where BatchID = @BatchID  and (ConstID = @ConstID or @ConstID is null)
end

if ((@StepID is not null or (@StepID is null and @ConstID is null)) and (@Scope is null or @Scope & 8 = 8))
begin
   insert @a (BatchID,StepID,ConstID,AttributeName,AttributeValue,AttributeScope)
   select  BatchID,StepID,ConstID,AttributeName,AttributeValue,2
     from dbo.[ETLStepConstraintAttribute]
    where BatchID = @BatchID and (StepID = @StepID or @StepID is null) and (ConstID = @ConstID or @ConstID is null)
end

if ((@StepID is not null or @ConstID is null) and (@Scope is null or @Scope & 2 = 2))
begin
   insert @a (BatchID,StepID,ConstID,AttributeName,AttributeValue,AttributeScope)
   select i.BatchID,i.StepID,null,i.AttributeName,i.AttributeValue,3
     from dbo.[ETLStepAttribute] i
    where i.BatchID = @BatchID and (i.StepID = @StepID or @StepID is null)
end

if(@Scope is null or @Scope & 1 = 1)
begin
    insert @a (BatchID,StepID,ConstID,AttributeName,AttributeValue,AttributeScope)
    select i.BatchID,null,null,i.AttributeName,i.AttributeValue,4
      from dbo.[ETLBatchAttribute] i
     where i.BatchID = @BatchID
end

--execution context
insert @a (BatchID,StepID,ConstID,AttributeName,AttributeValue,AttributeScope)
select null,null,null,'@BatchID' as AttributeName,cast(@BatchID as nvarchar(30)) as AttributeValue,0
union all
select null,null,null,'@StepID' as AttributeName,cast(@StepID as nvarchar(30)) as AttributeValue,0
union all
select null,null,null,'@ConstID' as AttributeName,cast(@ConstID as nvarchar(30)) as AttributeValue,0
union all
select null,null,null,'@RunID' as AttributeName,cast(@RunID as nvarchar(30)) as AttributeValue,0
union all
select null,null,null,'@Options' as AttributeName,cast(@Options as nvarchar(30)) as AttributeValue,0

-------------------------------------------------------------------
--add dsvobjectproperty attributes if any
-------------------------------------------------------------------
--declare @DsvID int
--select top (1) @DsvID = AttributeValue from @a where AttributeName = 'DsvVersion'
--if (@DsvID is not null)
--begin
--   insert @a (BatchID,StepID,ConstID,AttributeName,AttributeValue,AttributeScope)
--   select a.BatchID,a.StepID,a.ConstID,b.oName,b.oValue,a.AttributeScope
--     from @a a
--   cross apply dbo.fn_ETLObjectProperty(@DsvID,a.AttributeValue) as b
--    where a.AttributeName = 'DsvObject'
--end

/* dont need to do this here
** move all attribute resolution code to prc_ReadContextAttributes
-------------------------------------------------------------------
--relationship table
-------------------------------------------------------------------
insert @ab
select a.id,b.id,0
from @a a
join @a b on charindex('<' + a.AttributeName + '>',b.AttributeValue) > 0
      and (b.BatchID = a.BatchID or (a.BatchID is null and b.AttributeScope <= a.AttributeScope))
      and ((b.BatchID = a.BatchID and b.StepID = a.StepID) or (a.StepID  is null and b.AttributeScope <= a.AttributeScope))
      and ((b.BatchID = a.BatchID and b.StepID = a.StepID and b.ConstID = a.ConstID)
       or  (b.BatchID = a.BatchID and b.StepID is null and b.ConstID = a.ConstID)
       or (a.ConstID is null and b.AttributeScope <= a.AttributeScope))

union all select a.id,b.id,1
from @a a
join @a b on charindex('<' + a.AttributeName + '*>',b.AttributeValue) > 0
      and (b.BatchID = a.BatchID or (a.BatchID is null and b.AttributeScope <= a.AttributeScope))
      and ((b.BatchID = a.BatchID and b.StepID = a.StepID) or (a.StepID  is null and b.AttributeScope <= a.AttributeScope))
      and ((b.BatchID = a.BatchID and b.StepID = a.StepID and b.ConstID = a.ConstID)
       or  (b.BatchID = a.BatchID and b.StepID is null and b.ConstID = a.ConstID)
       or (a.ConstID is null and b.AttributeScope <= a.AttributeScope))

-------------------------------------------------------------------
--replace dynamic parameters if any
-------------------------------------------------------------------
WHILE (1=1)
BEGIN
    SET @id = null
    SELECT top(1) @id = t.id
                 ,@Name = t.AttributeName
                 ,@b = t.BatchID
                 ,@s = t.StepID
                 ,@c = t.ConstID
                 ,@Value1 = t.AttributeValue
                 ,@AttributeScope = t.AttributeScope
      FROM @a t
      WHERE t.CompleteFlag IS NULL
        AND NOT EXISTS(SELECT 1 FROM @ab t1
                         JOIN @a t2 ON t1.aid = t2.id WHERE  t.id = t1.bid AND t2.CompleteFlag IS NULL) --no parents
        AND EXISTS(SELECT 1 FROM @ab t2 WHERE  t.id = t2.aid) --have children
      order by t.AttributeScope,t.id

    IF (@id is null)
       BREAK

    update a set a.AttributeValue = REPLACE(a.AttributeValue,'<' + @Name + '>',isnull(@Value1,''))
      from @a a
      join @ab ab on a.id = ab.bid and ab.aid = @id and ab.isexec = 0

   --execute value to get the value
   IF (@Value1 is not null
       AND EXISTS(SELECT 1 FROM @ab ab WHERE ab.aid = @id and ab.isexec = 1)
       AND )
   BEGIN
      SET @nValue = 'select top 1 @value = (' + @Value1 + ')'
      SET @Value2 = null
      EXEC sp_executesql @nValue,N'@value varchar(max) output',@value = @value2 out

      update a set a.AttributeValue = REPLACE(a.AttributeValue,'<' + @Name + '*>',isnull(@Value2,''))
        from @a a
        join @ab ab on a.id = ab.bid and ab.aid = @id and ab.isexec = 1
   END


   update @a set CompleteFlag = 1 where id = @id
END
*/
-------------------------------------------------------------------
--build context object
-------------------------------------------------------------------

;WITH XMLNAMESPACES ('ETLController.XSD' as etl)
select @pContext = 
 (select b.BatchID as '@BatchID'
,b.BatchName as '@BatchName'
,b.BatchDesc as '@BatchDesc'
,b.IgnoreErr as '@IgnoreErr'
,b.RestartOnErr as '@Restart'
,(select top 1 ba1.AttributeValue from dbo.[ETLBatchAttribute] ba1 where b.BatchID = ba1.BatchID and ba1.AttributeName in ('HISTRET','etl:HistRet')) as '@HistRet'
,(select top 1 ba2.AttributeValue from dbo.[ETLBatchAttribute] ba2 where b.BatchID = ba2.BatchID and ba2.AttributeName in ('MaxThread','etl:MaxThread')) as '@MaxThread'
,(select top 1 ba3.AttributeValue from dbo.[ETLBatchAttribute] ba3 where b.BatchID = ba3.BatchID and ba3.AttributeName in ('Ping','etl:Ping')) as '@Ping'
,(select top 1 ba4.AttributeValue from dbo.[ETLBatchAttribute] ba4 where b.BatchID = ba4.BatchID and ba4.AttributeName in ('Timeout','etl:Timeout')) as '@Timeout'
,(select top 1 ba5.AttributeValue from dbo.[ETLBatchAttribute] ba5 where b.BatchID = ba5.BatchID and ba5.AttributeName in ('Lifetime','etl:Lifetime')) as '@Lifetime'
,(select top 1 ba6.AttributeValue from dbo.[ETLBatchAttribute] ba6 where b.BatchID = ba6.BatchID and ba6.AttributeName in ('Retry','etl:Retry')) as '@Retry'
,(select top 1 ba7.AttributeValue from dbo.[ETLBatchAttribute] ba7 where b.BatchID = ba7.BatchID and ba7.AttributeName in ('Delay','etl:Delay')) as '@Delay'
,(select top 1 ba8.AttributeValue from dbo.[ETLBatchAttribute] ba8 where b.BatchID = ba8.BatchID and ba8.AttributeName in ('Disabled','etl:Disabled')) as '@Disabled'
--,b.BatchDesc as 'Desc'
,p1.ProcessID as 'etl:OnSuccess/@ProcessID', p1.ScopeID as 'etl:OnSuccess/@ScopeID',p1.Process as 'etl:OnSuccess/etl:Process',p1.Param as 'etl:OnSuccess/etl:Param'
,p2.ProcessID as 'etl:OnFailure/@ProcessID', p2.ScopeID as 'etl:OnFailure/@ScopeID',p2.Process as 'etl:OnFailure/etl:Process',p2.Param as 'etl:OnFailure/etl:Param'
--batch attributes
--,(select ba.AttributeName as '@Name',ba.AttributeValue as '*' from dbo.ETLBatchAttribute ba
--   where b.BatchID = ba.BatchID
--     and ba.AttributeName not in ('HISTRET','MAXTHREAD','PING','TIMEOUT','LIFETIME','RETRY','DELAY')
--     for xml path('etl:Attribute'),type) as 'etl:Attributes'
,(select ba.AttributeName as '@Name',ba.AttributeValue as '*' from @a ba
   where b.BatchID = ba.BatchID and ba.StepID is null and ba.ConstID is null
     and ba.AttributeName not in ('HISTRET','MAXTHREAD','PING','TIMEOUT','LIFETIME','RETRY','DELAY','DISABLED',
     'etl:HISTRET','etl:MAXTHREAD','etl:PING','etl:TIMEOUT','etl:LIFETIME','etl:RETRY','etl:DELAY','etl:DISABLED')
     for xml path('etl:Attribute'),type) as 'etl:Attributes'

--
--batch constraints
,(select
 bc.ConstID as '@ConstID'
,bc.ConstOrder as '@ConstOrder'
,bc.WaitPeriod as '@WaitPeriod'
,(select top 1 bca1.AttributeValue from dbo.[ETLBatchConstraintAttribute] bca1 where bc.BatchID = bca1.BatchID and bc.ConstID = bca1.ConstID and bca1.AttributeName in ('Disabled','etl:Disabled')) as '@Disabled'
,(select top 1 bca2.AttributeValue from dbo.[ETLBatchConstraintAttribute] bca2 where bc.BatchID = bca2.BatchID and bc.ConstID = bca2.ConstID and bca2.AttributeName in ('Ping','etl:Ping')) as '@Ping'
,isnull(p0.ProcessID,0) as 'etl:Process/@ProcessID', isnull(p0.ScopeID,0) as 'etl:Process/@ScopeID',isnull(p0.Process,'Undefined') as 'etl:Process/etl:Process',isnull(p0.Param,'') as 'etl:Process/etl:Param'
--batch constraint attributes
--,(select bca.AttributeName as '@Name',bca.AttributeValue as '*'
--    from dbo.ETLBatchConstraintAttribute bca
--   where bc.BatchID = bca.BatchID and bc.ConstID = bca.ConstID
--     and bca.AttributeName not in ('DISABLED','PING')
--     for xml path('etl:Attribute'),type) as 'etl:Attributes'
,(select bca.AttributeName as '@Name',bca.AttributeValue as '*'
    from @a bca
   where bc.BatchID = bca.BatchID and bc.ConstID = bca.ConstID and bca.StepID is null
     and bca.AttributeName not in ('DISABLED','PING','etl:DISABLED','etl:PING')
     for xml path('etl:Attribute'),type) as 'etl:Attributes'
--
   from dbo.[ETLBatchConstraint] bc
   left join dbo.[ETLProcess] p0 on bc.ProcessID = p0.ProcessID
  where b.BatchID = bc.BatchID and (@StepID is null and (bc.ConstID = @ConstID or @ConstID is null)) and (@Scope is null or @Scope & 4 = 4) 
  for xml path('etl:Constraint'),type) as 'etl:Constraints'
--
--step
,(select s.StepID as '@StepID'
,s.StepName as '@StepName'
,s.StepDesc as '@StepDesc'
,s.IgnoreErr as '@IgnoreErr'
,s.StepOrder as '@StepOrder'
,(select top 1 sa1.AttributeValue from dbo.[ETLStepAttribute] sa1 where s.BatchID = sa1.BatchID and s.StepID = sa1.StepID and sa1.AttributeName in ('DISABLED','etl:Disabled')) as '@Disabled'
,(select top 1 sa2.AttributeValue from dbo.[ETLStepAttribute] sa2 where s.BatchID = sa2.BatchID and s.StepID = sa2.StepID and sa2.AttributeName in ('SeqGroup','etl:SeqGroup')) as '@SeqGroup'
,(select top 1 sa3.AttributeValue from dbo.[ETLStepAttribute] sa3 where s.BatchID = sa3.BatchID and s.StepID = sa3.StepID and sa3.AttributeName in ('PriGroup','etl:PriGroup')) as '@PriGroup'
,(select top 1 sa4.AttributeValue from dbo.[ETLStepAttribute] sa4 where s.BatchID = sa4.BatchID and s.StepID = sa4.StepID and sa4.AttributeName in ('Retry','etl:Retry')) as '@Retry'
,(select top 1 sa5.AttributeValue from dbo.[ETLStepAttribute] sa5 where s.BatchID = sa5.BatchID and s.StepID = sa5.StepID and sa5.AttributeName in ('Delay','etl:Delay')) as '@Delay'
,(select top 1 sa6.AttributeValue from dbo.[ETLStepAttribute] sa6 where s.BatchID = sa6.BatchID and s.StepID = sa6.StepID and sa6.AttributeName in ('Restart','etl:Restart')) as '@Restart'
,(select top 1 sa7.AttributeValue from dbo.[ETLStepAttribute] sa7 where s.BatchID = sa7.BatchID and s.StepID = sa7.StepID and sa7.AttributeName in ('LoopGroup','etl:LoopGroup')) as '@LoopGroup'
,(select top 1 sa8.AttributeValue from dbo.[ETLStepAttribute] sa8 where s.BatchID = sa8.BatchID and s.StepID = sa8.StepID and sa8.AttributeName in ('Timeout','etl:Timeout')) as '@Timeout'
--,s.StepDesc as 'Desc'
,isnull(p0.ProcessID,0) as 'etl:Process/@ProcessID', isnull(p0.ScopeID,0) as 'etl:Process/@ScopeID',isnull(p0.Process,'Undefined') as 'etl:Process/etl:Process',isnull(p0.Param,'') as 'etl:Process/etl:Param'
,p1.ProcessID as 'etl:OnSuccess/@ProcessID', p1.ScopeID as 'etl:OnSuccess/@ScopeID',p1.Process as 'etl:OnSuccess/etl:Process',p1.Param as 'etl:OnSuccess/etl:Param'
,p2.ProcessID as 'etl:OnFailure/@ProcessID', p2.ScopeID as 'etl:OnFailure/@ScopeID',p2.Process as 'etl:OnFailure/etl:Process',p2.Param as 'etl:OnFailure/etl:Param'
--step attributes
--,(select sa.AttributeName as '@Name',sa.AttributeValue as '*' from dbo.ETLStepAttribute sa
--   where s.BatchID = sa.BatchID and s.StepID = sa.StepID
--     and sa.AttributeName not in ('DISABLED','SEQGROUP','PRIGROUP','RETRY','RESTART')
--     for xml path('etl:Attribute'),type) as 'etl:Attributes'
,(select sa.AttributeName as '@Name',sa.AttributeValue as '*' from @a sa
   where s.BatchID = sa.BatchID and s.StepID = sa.StepID and sa.ConstID is null
     and sa.AttributeName not in ('DISABLED','SEQGROUP','PRIGROUP','RETRY','DELAY','RESTART','LOOPGROUP','TIMEOUT')
     and sa.AttributeName not in ('etl:DISABLED','etl:SEQGROUP','etl:PRIGROUP','etl:RETRY','etl:DELAY','etl:RESTART','etl:LOOPGROUP','etl:TIMEOUT')
     for xml path('etl:Attribute'),type) as 'etl:Attributes'
--
--step constraints
,(select
 sc.ConstID as '@ConstID'
,sc.ConstOrder as '@ConstOrder'
,sc.WaitPeriod as '@WaitPeriod'
,(select top 1 sca1.AttributeValue from dbo.[ETLStepConstraintAttribute] sca1 where sc.BatchID = sca1.BatchID and sc.StepID = sca1.StepID and sc.ConstID = sca1.ConstID and sca1.AttributeName in ('DISABLED','etl:Disabled')) as '@Disabled'
,(select top 1 sca2.AttributeValue from dbo.[ETLStepConstraintAttribute] sca2 where sc.BatchID = sca2.BatchID and sc.StepID = sca2.StepID and sc.ConstID = sca2.ConstID and sca2.AttributeName in ('Ping','etl:Ping')) as '@Ping'
,isnull(p0.ProcessID,0) as 'etl:Process/@ProcessID', isnull(p0.ScopeID,0) as 'etl:Process/@ScopeID',isnull(p0.Process,'Undefind') as 'etl:Process/etl:Process',isnull(p0.Param,'') as 'etl:Process/etl:Param'
--step constraint attributes
--,(select sca.AttributeName as '@Name',sca.AttributeValue as '*'
--    from dbo.ETLStepConstraintAttribute sca where sc.BatchID = sca.BatchID and sc.StepID = sca.StepID and sc.ConstID = sca.ConstID
--     and sca.AttributeName not in ('DISABLED','PING')
--    for xml path('etl:Attribute'),type) as 'etl:Attributes'
,(select sca.AttributeName as '@Name',sca.AttributeValue as '*'
    from @a sca where sc.BatchID = sca.BatchID and sc.StepID = sca.StepID and sc.ConstID = sca.ConstID
     and sca.AttributeName not in ('DISABLED','PING','etl:DISABLED','etl:PING')
    for xml path('etl:Attribute'),type) as 'etl:Attributes'
--
   from dbo.[ETLStepConstraint] sc
   left join dbo.[ETLProcess] p0 on sc.ProcessID = p0.ProcessID
   where s.BatchID = sc.BatchID and s.StepID = sc.StepID  and (sc.ConstID = @ConstID or @ConstID is null) and (@Scope is null or @Scope & 8 = 8)
   for xml path('etl:Constraint'),type) as 'etl:Constraints'
--
 from dbo.[ETLStep] s
 left join dbo.[ETLProcess] p0 on s.StepProcID = p0.ProcessID
 left join dbo.[ETLProcess] p1 on s.OnSuccessID = p1.ProcessID
 left join dbo.[ETLProcess] p2 on s.OnFailureID = p2.ProcessID
 where b.BatchID = s.BatchID and (s.StepID = @StepID or @StepID is null) and (@Scope is null or @Scope & 2 = 2)
   for xml path('etl:Step'),type) as 'etl:Steps'
 from dbo.[ETLBatch] b
 left join dbo.[ETLProcess] p1 on b.OnSuccessID = p1.ProcessID
 left join dbo.[ETLProcess] p2 on b.OnFailureID = p2.ProcessID
 where (b.BatchID = @BatchID) and (@Scope is null or @Scope & 1 = 1)
for xml path ('etl:Context'),type)

end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pHeader = null
   raiserror (@msg,11,11)
end catch

if (@debug = 1)
begin
   SET @msg =  'END Procedure ' + @ProcName
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@pHeader,@msg,@Err
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo
end

RETURN @Err