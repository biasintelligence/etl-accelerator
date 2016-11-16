/*
exec dbo.prc_ETLCounterSet -20,1,0,'test2','bbb'
select dbo.fn_ETLCounterGet (-20,1,0,'test2')

*/
create procedure [dbo].[prc_ETLCounterSet] (
    @pBatchID int
   ,@pStepID int = null
   ,@pRunID int = null
   ,@pName varchar(100) = null
   ,@pValue nvarchar(max)

) as
begin
/******************************************************************************
** File:	[prc_ETLCounterSet].sql
** Name:	[dbo].[prc_ETLCounterSet]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Procedure/[prc_ETLCounterSet].sql:

** Desc:	set client Counter value
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
** 2012-01-09       andreys                             validate inpits

*/

set nocount on
declare @err                int
declare @proc               sysname
declare @msg                nvarchar(1000)
declare @debug              tinyint
declare @Options            int
declare @query              nvarchar(max)

declare @BatchID int
declare @StepID int
declare @RunID int
declare @LGRunID int
declare @ProcErr int
declare @ProcName sysname
declare @Counters xml(ETLController)

set @err = 0
begin try

set @BatchID = @pBatchID
set @StepID = isnull(@pStepID,0)
set @RunID = isnull(@pRunID,0)

   if (not exists(select 1 from dbo.ETLBatch where BatchID = @BatchID))
      raiserror('Invalid Parameter b=%d',11,11,@BatchID);
   if (@RunID <> 0 and not exists(select 1 from dbo.ETLBatchRun where BatchID = @BatchID and RunID = @RunID))
      raiserror('Invalid Parameter br=%d',11,11,@BatchID,@RunID);
   if (@StepID <> 0 and not exists(select 1 from dbo.ETLStep where BatchID = @BatchID and StepID = @StepID))
      raiserror('Invalid Parameter bs=%d',11,11,@BatchID,@StepID);


   if exists(select 1 from dbo.[ETLStepRunCounter]
                      where RunID = @RunID and StepID = @StepID
                        and BatchID = @BatchID and CounterName = @pName)
   begin
      if @pValue is null
      begin
         delete dbo.[ETLStepRunCounter]
          where RunID = @RunID and StepID = @StepID
            and BatchID = @BatchID and CounterName = @pName
         --raiserror('record was deleted from dbo.ETLStepRunCounter',0,1)
      end
      else
      begin
         update dbo.[ETLStepRunCounter]
         set CounterValue = @pValue
          where RunID = @RunID and StepID = @StepID
            and BatchID = @BatchID and CounterName = @pName
         --raiserror('record was updated in dbo.ETLStepRunCounter',0,1)
      end
   end
   else
   begin
      insert dbo.[ETLStepRunCounter]
      (RunID,StepID,CounterName,BatchID,CounterValue)
      values(@RunID,@StepID,@pName,@BatchID,@pValue)
      --raiserror('record was inserted into dbo.ETLStepRunCounter',0,1)
   end


end try
begin catch
   set @Proc = ERROR_PROCEDURE()
   set @pValue = null
   set @Msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   raiserror ('ERROR: PROC %s, MSG: %s',11,11,@Proc,@Msg) 
end catch

return @err
end