CREATE PROCEDURE [dbo].[prc_EventCheck_Process2]
    @pRequest xml([ETLController])
   ,@pReceipt  xml([ETLController]) = NULL OUTPUT
As
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

DECLARE @WatermarkEventType nvarchar(1000)
DECLARE @WatermarkEventID uniqueidentifier
DECLARE @WatermarkEventReceived datetime
DECLARE @WatermarkEventPosted datetime
DECLARE @WatermarkEventArgs xml

DECLARE @EventType nvarchar(1000)
DECLARE @EventID uniqueidentifier
DECLARE @EventReceived datetime
DECLARE @EventPosted datetime
DECLARE @EventArgs xml

DECLARE @EventServer sysname
DECLARE @EventDatabase sysname

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

exec @ProcErr = dbo.[prc_ReadAttribute] @Attributes,'WatermarkEventType',@WatermarkEventType out
exec @ProcErr = dbo.[prc_ReadAttribute] @Attributes,'EventType',@EventType out
exec @ProcErr = dbo.[prc_ReadAttribute] @Attributes,'EventServer',@EventServer out
exec @ProcErr = dbo.[prc_ReadAttribute] @Attributes,'EventDatabase',@EventDatabase out
if (@EventType is null)
BEGIN
   SET @ErrMsg = '   ERROR 50110: EvenType is required'
   SET @StatusID = @STAT_ERROR
   SET @Err = 50110
   raiserror (@ErrMsg,11,11)
END

if (@WatermarkEventType is null)
BEGIN
   SET @ErrMsg = '   ERROR 50110: WatermarkEventType is required'
   SET @StatusID = @STAT_ERROR
   SET @Err = 50110
   raiserror (@ErrMsg,11,11)
END

set @EventServer = ISNULL(@EventServer,@@SERVERNAME)
set @EventDatabase = ISNULL(@EventDatabase,DB_NAME())

----------------------------------------------------------------------------------
--Checking...
----------------------------------------------------------------------------------
exec dbo.[prc_CLREventReceive] @EventServer,@EventDatabase
   ,@WatermarkEventID out,@WatermarkEventPosted out,@WatermarkEventReceived out,@WatermarkEventArgs out
   ,@WatermarkEventType,@Options

exec dbo.[prc_CLREventReceive] @EventServer,@EventDatabase
   ,@EventID out,@EventPosted out,@EventReceived out,@EventArgs out
   ,@EventType,@Options

--IF (@debug IS NOT NULL)
--BEGIN
   SET @msg = '   Waiting for Event condition: ' + @EventType + ' > ' + @WatermarkEventType + ' and ' + @EventType + ' > 24 hours ago'
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
--END

set @StatusID = 
case when @EventReceived is null then @STAT_FAILURE
     when @WatermarkEventReceived is null and (@EventReceived > DATEADD(DAY,-1,GETDATE())) then @STAT_SUCCESS
     when (@WatermarkEventReceived < @EventReceived) and (@EventReceived > DATEADD(DAY,-1,GETDATE())) then @STAT_SUCCESS
     else @STAT_FAILURE
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
RETURN @Err;