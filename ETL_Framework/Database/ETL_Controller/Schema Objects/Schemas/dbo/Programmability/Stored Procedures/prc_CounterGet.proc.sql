/*
select * from ETLBatchRun where batchid = 130

--select * from ETLStepRunCounter
--insert ETLStepRunCounter
--values(-20,1,4,'test','xxx')
--insert ETLStepRunCounter
--values(-20,1,3,'test1','yyy')
declare @pHeader xml
declare @pCounters xml
declare @pValue nvarchar(1000)
exec dbo.prc_CreateHeader @pHeader out,-20,1,null,4,1
--select @pHeader
exec dbo.prc_CreateCounters @pCounters out,@pHeader,'test'
exec dbo.prc_CounterGet @pValue out,@pHeader,'test'
select @pCounters
select @pValue

*/
create procedure [dbo].[prc_CounterGet] (
    @pValue nvarchar(max) output
   ,@pHeader xml([ETLController])
   ,@pName varchar(100) = null
) as
begin
/******************************************************************************
** File:	[ETL_CounterGet].sql
** Name:	[dbo].[ETL_CounterGet]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Procedure/[ETL_CounterGet].sql:

** Desc:	return Counter value to the client
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

exec @ProcErr = dbo.[prc_ReadHeader] @pHeader,null,null,null,@RunID out,null,null
exec @ProcErr = dbo.[prc_CreateCounters] @Counters out, @pHeader, @pName
exec @ProcErr = dbo.[prc_ReadCounter] @Counters,@pName,@pValue out,@RunID

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