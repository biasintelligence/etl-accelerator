/*
declare @pHeader xml
declare @pCounters xml
declare @pValue nvarchar(1000)
exec dbo.prc_CreateHeader @pHeader out,-20,1,null,5,1
--select @pHeader
exec dbo.prc_CounterSet @pHeader,'test2','bbb'
exec dbo.prc_CounterGet @pValue out,@pHeader,'test2'
--select @pCounters
select @pValue

*/
create procedure [dbo].[prc_CounterSet] (
    @pHeader xml([ETLController])
   ,@pName varchar(100) = null
   ,@pValue nvarchar(max)

) as
begin
/******************************************************************************
** File:	[ETL_CounterSet].sql
** Name:	[dbo].[ETL_CounterSet]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Procedure/[ETL_CounterSet].sql:

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

exec @ProcErr = dbo.[prc_ReadHeader] @pHeader,@BatchID out,@StepID out,null,@RunID out,null,null

   if exists(select 1 from dbo.[ETLStepRunCounter]
                      where RunID = @RunID and StepID = @StepID
                        and BatchID = @BatchID and CounterName = @pName)
   begin
      if @pValue is null
      begin
         delete dbo.[ETLStepRunCounter]
          where RunID = @RunID and StepID = @StepID
            and BatchID = @BatchID and CounterName = @pName
         raiserror('record was deleted from dbo.ETLStepRunCounter',0,1)
      end
      else
      begin
         update dbo.[ETLStepRunCounter]
         set CounterValue = @pValue
          where RunID = @RunID and StepID = @StepID
            and BatchID = @BatchID and CounterName = @pName
         raiserror('record was updated in dbo.ETLStepRunCounter',0,1)
      end
   end
   else
   begin
      insert dbo.[ETLStepRunCounter]
      (RunID,StepID,CounterName,BatchID,CounterValue)
      values(@RunID,@StepID,@pName,@BatchID,@pValue)
      raiserror('record was inserted into dbo.ETLStepRunCounter',0,1)
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