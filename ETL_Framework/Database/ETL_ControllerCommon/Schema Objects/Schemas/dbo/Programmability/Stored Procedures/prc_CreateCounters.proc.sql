
/*
select * from ETLBatchRun where batchid = 130

--select * from ETLStepRunCounter
--insert ETLStepRunCounter
--values(-20,1,4,'test','xxx')
--insert ETLStepRunCounter
--values(-20,1,3,'test1','yyy')
declare @pHeader xml
declare @pCounters xml
exec dbo.prc_CreateHeader @pHeader out,-20,1,null,4,1
--select @pHeader
exec dbo.prc_CreateCounters @pCounters out,@pHeader
select @pCounters

*/
CREATE procedure [dbo].[prc_CreateCounters] (
    @pCounters xml ([ETLController]) output
   ,@pHeader xml([ETLController])
   ,@pName nvarchar(100) = null
) as
begin
/******************************************************************************
** File:	[prc_CreateCounters].sql
** Name:	[dbo].[prc_CreateCounters]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Procedure/[prc_ETLStepCounterGet].sql:

** Desc:	return Counters object for a header context
**          
**
** Params:
** Returns:
**
** Author:	andreys
** Date:	10/30/2007
** ****************************************************************************
** CHANGE HISTORY
** ****************************************************************************
** Date				Author	version	4	#bug			Description
** ----------------------------------------------------------------------------------------------------------

*/

set nocount on

declare @msg                nvarchar(1000)
declare @debug              tinyint
declare @Options            int
declare @query              nvarchar(max)

declare @BatchID int
declare @StepID int
declare @RunID int
declare @ProcName sysname
declare @ProcessInfo xml(ETLController)
declare @Err int

set @ProcName = object_name(@@procid)
begin try

exec dbo.[prc_ReadHeader] @pHeader,@BatchID out,@StepID out,null,@RunID out,@Options out
set @debug = nullif(@Options & 1,0)

if (@debug = 1)
begin
   SET @msg =  'BEGIN Procedure ' + @ProcName + ' with context'
         + ' BatchID=' + isnull(cast(@BatchID as nvarchar(10)),'null')
         + ' StepID=' + isnull(cast(@StepID as nvarchar(10)),'null')
         + ' RunID=' + isnull(cast(@RunID as nvarchar(10)),'null')

   exec dbo.[prc_CreateProcessInfo] @ProcessInfo out,@pHeader=@pHeader,@pMsg=@msg
   exec dbo.[prc_Print] @pProcessInfo=@ProcessInfo
end

set @StepID = isnull(@StepID,0)

--return last known value for all counters
;with xmlnamespaces('ETLController.XSD' as etl)
select @pCounters = 
(select @BatchID as '@BatchID',nullif(@StepID,0) as '@StepID',@RunID as '@RunID'
,(select
        c.CounterName as '@Name'
       ,c.RunID as '@RunID'
       ,c.CounterValue as '*'
    from dbo.[ETLStepRunCounter] c (nolock)
    join (select max(c.RunID) as RunID, c.CounterName
            from dbo.[ETLStepRunCounter] c (nolock)
           where c.RunID <= @RunID and c.StepID = @StepID and c.BatchID = @BatchID
             and (@pName is null or c.CounterName = @pName)
           group by c.CounterName) p
      on c.RunID = p.RunID and c.CounterName = p.CounterName
   where (c.StepID = @StepID and c.BatchID = @BatchID
     and (@pName is null or c.CounterName = @pName))
     for xml path('etl:Counter'),type)
for xml path('etl:Counters'),type
)

if (@debug = 1)
begin
   SET @msg =  'END Procedure ' + @ProcName
   exec dbo.[prc_CreateProcessInfo] @ProcessInfo out,@pHeader=@pHeader,@pMsg=@msg
   exec dbo.[prc_Print] @pProcessInfo=@ProcessInfo
end

end try
begin catch	
	if @@trancount > 0 rollback tran
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pCounters = null
   raiserror (@msg,11,11)
end catch
   return 0
end