/*
select * from ETLSteprunhistory where batchid = -20

declare @pHeader xml
exec dbo.prc_CreateHeader @pHeader out,-20,null,null,6,1
exec dbo.prc_AttributeSet @pHeader,'CheckRunID',6

declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @pProcessReceipt xml
exec dbo.prc_CreateHeader @pHeader out,-20,1,null,5,1
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext
--select @pProcessRequest
exec dbo.prc_StatusCheck @pProcessRequest,@pProcessReceipt out
select @pProcessReceipt
*/
CREATE PROCEDURE [dbo].[prc_StatusCheck]
    @pRequest xml([ETLController])
   ,@pReceipt  xml([ETLController]) = NULL OUTPUT
As
/******************************************************************
**D File:         prc_StatusCheck.SQL
**
**D Desc:         Check execution Status of the request
**
**D Auth:         andreys
**D Date:         10/28/2007
**
** Param: @pRequest  - BatchID info
                  @pReceipt results (StatusID - 2 - SUCCESS, 3 - FAILURE, 4 - ERROR, 6 - FAILURE_IMMEDIATE) (OUTPUT only)
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ProcErr INT
DECLARE @ProcName sysname
DECLARE @StatusID tinyint
DECLARE @msg nvarchar(max)
DECLARE @ErrMsg nvarchar(max)
DECLARE @BatchID int
DECLARE @StepID int
DECLARE @ConstID int
DECLARE @debug tinyint
DECLARE @handle uniqueidentifier
DECLARE @Value varchar(1000)
DECLARE @nValue nvarchar(max)
DECLARE @RunID int
DECLARE @Options int

DECLARE @CheckRunID int
DECLARE @CheckBatchID int
DECLARE @CheckStepID int
DECLARE @CheckBatchName nvarchar(100)
DECLARE @CheckStepName nvarchar(100)
DECLARE @CheckContext nvarchar(100)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0

DECLARE @STAT_SUCCESS TINYINT
DECLARE @STAT_FAILURE TINYINT
DECLARE @STAT_ERROR TINYINT
DECLARE @STAT_FAILURE_IMMEDIATE TINYINT

SET @STAT_SUCCESS = 2
SET @STAT_FAILURE = 3
SET @STAT_ERROR = 4
SET @STAT_FAILURE_IMMEDIATE = 6
-------------------------------------------------------------------
--Return Statuses
--2 - Success
--3 - Failure
--4 - Error
--6 - failure and exit
-------------------------------------------------------------------
DECLARE @Request AS xml (ETLController)
DECLARE @Receipt AS xml (ETLController)
DECLARE @Header AS xml (ETLController)
DECLARE @Context AS xml (ETLController)
DECLARE @ProcessInfo AS xml (ETLController)
DECLARE @Attributes AS xml (ETLController)

begin try
exec @ProcErr = dbo.[prc_ReadProcessRequest] @pRequest,@Header out,@Context out,@handle out
exec @ProcErr = dbo.[prc_ReadHeader] @Header,@BatchID out,@StepID out,null,@RunID out,@Options out

set @debug = nullif(@Options & 1,0)
IF (@debug IS NOT NULL)
BEGIN
   SET @msg = 'BEGIN Procedure ' + @ProcName
                           + ' with @BatchID=' + CAST(@BatchID AS nvarchar(30))
                           + ISNULL( ', @StepID=' +CAST(@StepID AS nvarchar(30)),'')
                           + ISNULL( ', @RunID=' +CAST(@RunID AS nvarchar(30)),'')

   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END


-------------------------------------------------------------------
--Retrieve Attributes
-------------------------------------------------------------------
exec @ProcErr = dbo.[prc_ReadContextAttributes] @pRequest,@Attributes out

exec @ProcErr = dbo.prc_ReadAttribute @Attributes,'CheckContext',@CheckContext out
if (@CheckContext is null)
   set @CheckContext = 'Current'

exec @ProcErr = dbo.prc_ReadAttribute @Attributes,'CheckBatchID',@CheckBatchID out
if (@CheckBatchID is null)
begin
   exec @ProcErr = dbo.prc_ReadAttribute @Attributes,'CheckBatchName',@CheckBatchName out
   select @CheckBatchID = BatchID from dbo.[ETLBatch] where BatchName = @CheckBatchName
   IF (@CheckBatchID is null and @CheckBatchName is not null)
   BEGIN
     SET @ErrMsg = '   ERROR 50110: unknown CheckBatchName ' + @CheckBatchName
     SET @StatusID = @STAT_ERROR
     SET @Err = 50110
     raiserror (@ErrMsg,11,11)
   END
end

if (@CheckBatchID is null)
   set @CheckBatchID = @BatchID


exec @ProcErr = dbo.prc_ReadAttribute @Attributes,'CheckRunID',@CheckRunID out
if (@CheckRunID is null)
begin
   if (@CheckBatchID = @BatchID)
      set @CheckRunID = @RunID
   else
      select @CheckRunID = max(RunID) from dbo.[ETLBatchRun] where BatchID = @CheckBatchID
end


if (@CheckContext = 'Step' or (@CheckContext = 'Current' and @StepID is not null))
begin
   exec @ProcErr = dbo.prc_ReadAttribute @Attributes,'CheckStepID',@CheckStepID out
   if (@CheckStepID is null)
   begin
      exec @ProcErr = dbo.prc_ReadAttribute @Attributes,'CheckStepName',@CheckStepName out
      select @CheckStepID = StepID from dbo.[ETLStep] where BatchID = @CheckBatchID and StepName = @CheckStepName
      IF (@CheckStepID is null and @CheckStepName is not null)
      BEGIN
        SET @ErrMsg = '   ERROR 50110: unknown CheckStepName ' + @CheckStepName
        SET @StatusID = @STAT_ERROR
        SET @Err = 50110
        raiserror (@ErrMsg,11,11)
      END
   end

   if (@CheckStepID is null)
      set @CheckStepID = @StepID
end

if (@CheckBatchID = @BatchID and @CheckStepID = @StepID and @CheckRunID = @RunID)
begin
   SET @ErrMsg = '   ERROR 50110: check information is required: BATCHID/BATCHNAME;STEPID/STEPNAME;RUNID'
   SET @StatusID = @STAT_ERROR
   SET @Err = 50110
   raiserror (@ErrMsg,11,11)
end
----------------------------------------------------------------------------------
--Checking...
----------------------------------------------------------------------------------
--checking Batch
if (@CheckStepID is null)
begin
   SELECT @StatusID = StatusID
     FROM dbo.[ETLBatchRun] r
    WHERE r.RunID = @CheckRunID
      AND r.BatchID = @CheckBatchID

   IF (@debug IS NOT NULL)
   BEGIN
      SET @msg = '   Check: BatchID=' + cast(@CheckBatchID as nvarchar(30))
               + ',RunpID=' + cast(@CheckRunID as nvarchar(30))
               + ',Status=' + isnull(cast(@StatusID as varchar(10)),'null') 
      exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
      exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
   END

   SET @StatusID =
               CASE
                  WHEN @StatusID = 1 THEN @STAT_FAILURE --running
                  WHEN @StatusID in (0,2) THEN @STAT_SUCCESS      --finished with success or never been run
                  WHEN @StatusID in (3,4) THEN @STAT_FAILURE_IMMEDIATE --failed
                  ELSE @STAT_SUCCESS --not found therefore no reason to wait
                END
end
--checking Step
else
begin
   if (@CheckRunID = @RunID)
      SELECT @StatusID = StatusID
        FROM dbo.[ETLStepRun] r
       WHERE r.RunID = @CheckRunID
         AND r.BatchID = @CheckBatchID
         AND r.StepID = @CheckStepID
   else
      SELECT @StatusID = StatusID
        FROM dbo.[ETLStepRunHistory] r
       WHERE r.RunID = @CheckRunID
         AND r.BatchID = @CheckBatchID
         AND r.StepID = @CheckStepID


   IF (@debug IS NOT NULL)
   BEGIN
      SET @msg = '   Check: BatchID=' + cast(@CheckBatchID as nvarchar(30))
               + ',StepID=' + cast(@CheckStepID as nvarchar(30))
               + ',RunpID=' + cast(@CheckRunID as nvarchar(30))
               + ',Status=' + isnull(cast(@StatusID as varchar(10)),'null') 
      exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
      exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
   END

   SET @StatusID =
               CASE
                  WHEN @StatusID in (0,1) THEN @STAT_FAILURE --running or not started
                  WHEN @StatusID = 2 THEN @STAT_SUCCESS      --finished with success
                  WHEN @StatusID in (3,4) THEN @STAT_FAILURE_IMMEDIATE --failed
                  ELSE @STAT_SUCCESS --not found therefore no reason to wait
                END
end

end try
begin catch
set @err = error_number()
set @ErrMsg = isnull(@ErrMsg,error_message())
IF (@ErrMsg IS NOT NULL)
BEGIN
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@ErrMsg,@Err
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END
set @StatusID = @STAT_FAILURE_IMMEDIATE
end catch

if (@pReceipt is null)
   exec @ProcErr = dbo.prc_CreateProcessReceipt @pReceipt out,@Header,@StatusID,@Err,@ErrMsg

IF (@debug IS NOT NULL)
BEGIN
   SET @msg = 'END Procedure ' + @ProcName
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END
RETURN @Err