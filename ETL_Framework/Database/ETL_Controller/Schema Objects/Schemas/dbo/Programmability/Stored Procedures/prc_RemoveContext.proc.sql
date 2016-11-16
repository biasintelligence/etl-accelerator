/*

declare @Header xml
declare @pBatchName nvarchar(30)
set @pBatchName = 'xxx' 
exec prc_RemoveContext @pBatchName,null,'debug'
*/
CREATE PROCEDURE [dbo].[prc_RemoveContext]
    @pBatchName nvarchar(30)
   ,@pHandle uniqueidentifier = null 
   ,@pOptions nvarchar(100) = null
As
/******************************************************************
**D File:         prc_PersistContext.SQL
**
**D Desc:         create persist context into ETLBatch tables
**
** @Options       debug

**D Auth:         andreys
**D Date:         10/27/2007
**
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ProcErr INT
DECLARE @Cnt INT
DECLARE @ProcName sysname
DECLARE @msg nvarchar(max)
DECLARE @trancount int
DECLARE @debug tinyint

DECLARE @BatchID int

DECLARE @Header xml(ETLController)
DECLARE @ProcessInfo xml(ETLController)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0
SET @trancount = @@trancount


begin try
set @debug = case when charindex('debug',@pOptions) > 0 then 1 else 0 end

select @BatchID = BatchID
  from dbo.[ETLBatch] where BatchName = @pBatchName

if (@BatchID is null)
BEGIN
   --SET @Err = 50101
   --SET @msg = '   ERROR pContext: BatchName=' + @pBatchName + ' not found'
   --RAISERROR(@msg,11,11)
   --batch not found and nothing to remove
   SET @msg = '   WARNING pContext: BatchName=' + @pBatchName + ' not found'
   RAISERROR(@msg,0,1)
   RETURN @Err 
END

exec [prc_CreateHeader] @Header out,@BatchID,null,null,0,@debug,15
if (@debug = 1)
begin
   SET @msg =  'BEGIN Procedure ' + @ProcName + ' for BatchName=' + isnull(@pBatchName,'NULL')
            + ' (' + isnull(cast(@BatchID as nvarchar(10)),'NULL') + ')'

   exec @ProcErr = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@pHandle
end

--BEGIN TRAN

   delete dbo.[ETLStepConstraintAttribute] where BatchID = @BatchID
   delete dbo.[ETLStepConstraint] where BatchID = @BatchID
   delete dbo.[ETLStepAttribute] where BatchID = @BatchID
   delete dbo.[ETLStep] where BatchID = @BatchID
   delete dbo.[ETLBatchConstraintAttribute] where BatchID = @BatchID
   delete dbo.[ETLBatchConstraint] where BatchID = @BatchID
   delete dbo.[ETLBatchAttribute] where BatchID = @BatchID
   delete dbo.[ETLBatch] where BatchID = @BatchID

--COMMIT TRAN

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