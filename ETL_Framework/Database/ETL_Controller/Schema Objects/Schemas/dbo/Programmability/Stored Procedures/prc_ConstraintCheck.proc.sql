/*
declare @BatchID int
set @BatchID = 310
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @pProcessReceipt xml
exec dbo.prc_CreateHeader @pHeader out,@BatchID,1,1,138,1
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext
--select @pProcessRequest
exec dbo.prc_ConstraintCheck @pProcessRequest,@pProcessReceipt out
--select @pProcessReceipt
*/
CREATE PROCEDURE [dbo].[prc_ConstraintCheck]
    @pRequest xml([ETLController])
   ,@pReceipt  xml([ETLController]) = NULL OUTPUT
As
/******************************************************************
**D File:         prc_ConstraintCheck.SQL
**
**D Desc:         Check Constraints
**
**D Auth:         andreys
**D Date:         10/30/2007
**
** Param: @pRequest  - BatchID info
                  @pReceipt results (StatusID - 2 - SUCCESS, 3 - FAILURE) (OUTPUT only)
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
**  2010/07/12       andrey@biasintelligence.com           stop constraint processing on ExitEvent
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ExitCode INT
DECLARE @Cnt INT
DECLARE @ProcName sysname
DECLARE @StartDT datetime
DECLARE @BatchID INT
DECLARE @StepID INT
DECLARE @ConstID int
DECLARE @StatusID tinyint
DECLARE @BatchStatusID tinyint
DECLARE @debug tinyint
DECLARE @Options int
DECLARE @msg nvarchar(max)
DECLARE @RaiserrMsg nvarchar(max)
DECLARE @handle uniqueidentifier
DECLARE @RunID int
DECLARE @Scope int

DECLARE @Process nvarchar(max)
DECLARE @WaitPeriod int    -- in seconds
DECLARE @Wait varchar(20)
DECLARE @Disabled int

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ExitCode = 0

DECLARE @ATT_PING INT

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
--5 - Failure with abort
-------------------------------------------------------------------
DECLARE @ProcessInfo AS xml (ETLController)
DECLARE @Header AS xml (ETLController)
DECLARE @Context AS xml (ETLController)
DECLARE @cHeader AS xml (ETLController)
DECLARE @cRequest AS xml (ETLController)
DECLARE @cReceipt AS xml (ETLController)

BEGIN TRY

exec @ExitCode = dbo.[prc_ReadProcessRequest] @pRequest,@Header out,@Context out,@handle out
exec @ExitCode = dbo.[prc_ReadHeader] @Header,@BatchID out,@StepID out,null,@RunID out,@Options out,@Scope out

set @debug = isnull(@Options & 1,0)
IF (@debug = 1)
BEGIN
   SET @msg = 'BEGIN Procedure ' + @ProcName
                           + ' with @BatchID=' + CAST(@BatchID AS nvarchar(30))
                           + ISNULL( ', @StepID=' +CAST(@StepID AS nvarchar(30)),'')

   exec @ExitCode = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
END

-------------------------------------------------------------------
--Check for the constraints existance
-------------------------------------------------------------------
SET @ExitCode = 0
IF (@StepID is null)
  SET @ExitCode = @Context.exist('declare namespace etl="ETLController.XSD";(/etl:Context[@BatchID=(sql:variable("@BatchID"))]/etl:Constraints)')
ELSE
  SET @ExitCode = @Context.exist('declare namespace etl="ETLController.XSD";
                                 (/etl:Context[@BatchID=(sql:variable("@BatchID"))]/etl:Steps/etl:Step[@StepID=(sql:variable("@StepID"))]/etl:Constraints)')

IF (@ExitCode = 0)
BEGIN
  SET @StatusID = @STAT_SUCCESS
END
ELSE
BEGIN

declare @const table
(ConstID int
,Process nvarchar(max)
,WaitPeriod int null
,Ping int null
,[Disabled] tinyint null
,ConstOrder nvarchar(10)
)

if (@StepID is null)
begin
   ;with xmlnamespaces ('ETLController.XSD' as etl)
   insert @const
   SELECT
    c.const.value('./@ConstID','int') as ConstID
   ,'exec @ExitCode = ' + c.const.value('(./etl:Process/etl:Process)[1]','nvarchar(max)') + ' @pRequest=@Request, @pReceipt=@Receipt out' 
    + isnull(',' + c.const.value('(./etl:Process/etl:Param)[1]','nvarchar(max)'),'') as Process
   ,c.const.value('./@WaitPeriod','int') as WaitPeriod
   ,isnull(c.const.value('./@Ping','int'),10) as Ping
   ,isnull(c.const.value('./@Disabled','int'),0) as [Disabled]
   ,c.const.value('./@ConstOrder','int') as ConstOrder
   FROM @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]/etl:Constraints/etl:Constraint') c(const)
end
else
begin
   ;with xmlnamespaces ('ETLController.XSD' as etl)
   insert @const
   SELECT
    c.const.value('./@ConstID','int') as ConstID
   ,'exec @ExitCode = ' + c.const.value('(./etl:Process/etl:Process)[1]','nvarchar(max)') + ' @pRequest=@Request,@pReceipt=@Receipt out' 
    + isnull(',' + c.const.value('(./etl:Process/etl:Param)[1]','nvarchar(max)'),'') as Process
   ,c.const.value('./@WaitPeriod','int') as WaitPeriod
   ,isnull(c.const.value('./@Ping','int'),10) as Ping
   ,isnull(c.const.value('./@Disabled','int'),0) as [Disabled]
   ,c.const.value('./@ConstOrder','int') as ConstOrder
   FROM @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]/etl:Steps/etl:Step[@StepID=(sql:variable("@StepID"))]/etl:Constraints/etl:Constraint') c(const)
end

-------------------------------------------------------------------
--Loop through the Step Constraints
-------------------------------------------------------------------
DECLARE StepConstCursor CURSOR LOCAL FAST_FORWARD
FOR SELECT c.ConstID,c.Process,c.WaitPeriod,c.Ping,c.Disabled
FROM @const c
ORDER BY c.ConstOrder

OPEN StepConstCursor
WHILE (1=1)
BEGIN
   SET @StartDT = getdate()
   SET @ConstID = NULL
   FETCH NEXT FROM StepConstCursor INTO @ConstID,@Process,@WaitPeriod,@ATT_PING,@Disabled
   IF (@@FETCH_STATUS <> 0 OR @ConstID IS NULL)
      BREAK

   IF isnull(@Disabled,0) = 1 
   BEGIN
      IF (@debug = 1)
      BEGIN
         SET @msg = '   Constraint=' +  CAST(@ConstID AS nvarchar(30)) + ' is disabled:  ' + @Process
         exec @ExitCode = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
         exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
      END
      CONTINUE
   END

   SET @Wait = right('00' + cast(@ATT_PING/3600 as varchar(10)),3)
             + ':' + right('0' + cast((@ATT_PING/60)%60 as varchar(10)),2)
             + ':' + right('0' + cast(@ATT_PING%60 as varchar(10)),2)

   IF (@debug = 1)
   BEGIN
      SET @msg = '   Constraint=' +  CAST(@ConstID AS nvarchar(30))
               + ' wait=' + CAST(@WaitPeriod AS nvarchar(30)) + '(' + isnull(@Wait,'null') + ')'
               + ':  ' + @Process
      exec @ExitCode = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
      exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
   END

   IF (@Process IS NOT NULL)
   BEGIN
       exec @ExitCode = dbo.[prc_CreateHeader] @cHeader out,@BatchID,@StepID,@ConstID,@RunID,@Options,@Scope
       exec @ExitCode = dbo.[prc_CreateProcessRequest] @cRequest out,@cHeader,@Context,@handle
      --Wait Until WaitPeriod is expired
      WHILE (1=1)
      BEGIN
         SET @StatusID = NULL
         SET @cReceipt = NULL
         EXEC sp_ExecuteSQL @Process
                  ,N'@ExitCode int output,@Request xml(ETLController),@Receipt xml(ETLController) output'
                  ,@ExitCode = @ExitCode output
                  ,@Request = @cRequest
                  ,@Receipt = @cReceipt output

         exec @ExitCode = dbo.[prc_ReadProcessReceipt] @cReceipt,null,@StatusID out,@Err out--,@msg out

         SET @StatusID = ISNULL(@StatusID,@STAT_ERROR)
         IF (@debug = 1)
         BEGIN
            SET @msg = '   Constraint=' +  CAST(@ConstID AS nvarchar(30)) + ' returns StatusID=' + CAST(@StatusID AS nvarchar(30))
            exec @ExitCode = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
            exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
         END

-------------------------------------------------------------------
--Exit Batch on Exit Event for batch constraints only
-------------------------------------------------------------------
         if (@StepID is null)
         begin
            set @BatchStatusID = cast(dbo.[fn_ETLCounterGet] (@BatchID,0,@RunID,'ExitEvent') as tinyint)
            IF (@BatchStatusID is not null)
            BEGIN
               SET @StatusID = @BatchStatusID
               IF (@debug = 1)
               BEGIN
                  SET @msg = '   Costraint Check Exit on ExitEvent with StatusID=' + CAST(@StatusID as nvarchar(30))
                  exec @ExitCode = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
                  exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
               END
            END
         end


         IF (@StatusID = @STAT_ERROR)
         BEGIN
            SET @RaiserrMsg = '   ERROR Constraint=' +  CAST(@ConstID AS nvarchar(30)) + ' failed'
            BREAK
         END
         ELSE IF(@StatusID = @STAT_SUCCESS)
         BEGIN
            BREAK
         END
         ELSE
         BEGIN
            IF (@StatusID = @STAT_FAILURE_IMMEDIATE OR DATEDIFF(minute,@StartDT,getdate()) > @WaitPeriod)
            BEGIN
               SET @RaiserrMsg = '   ERROR Constraint=' +  CAST(@ConstID AS nvarchar(30)) + ' was not met'
               SET @StatusID = @STAT_FAILURE
               SET @Err = 50107 -- constraint was not met
               BREAK
            END

            IF (@debug = 1)
            BEGIN
               SET @msg = '   Constraint=' +  CAST(@ConstID AS nvarchar(30)) + ' wait=' + isnull(@Wait,'null')
               exec @ExitCode = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
               exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
            END
            WAITFOR DELAY @Wait

            --check conversation
            IF (@handle IS NOT NULL)
               IF EXISTS (SELECT * FROM sys.conversation_endpoints WHERE [conversation_handle] = @handle AND  state IN ('DI','DO','ER','CD'))
               BEGIN
                  SET @RaiserrMsg = '   ERROR conversation is not active'
                  SET @StatusID = @STAT_ERROR
                  SET @Err = 50110
                  BREAK
               END

         END
      END --WHILE(@Err = 0)
      IF @StatusID IN (@STAT_FAILURE,@STAT_ERROR,@STAT_FAILURE_IMMEDIATE)
         BREAK
    END
END --WHILE (@Err = 0)

DEALLOCATE StepConstCursor
IF (@RaiserrMsg is not null )
BEGIN
   RAISERROR(@RaiserrMsg,11,11)
END

SET @StatusID = @STAT_SUCCESS
END
END TRY
BEGIN CATCH
SET @Err = ISNULL(NULLIF(@Err,0),ERROR_NUMBER())
SET @RaiserrMsg = ISNULL(@RaiserrMsg,ERROR_MESSAGE())
SET @StatusID = isnull(@StatusID,@STAT_ERROR)
IF (@RaiserrMsg IS NOT NULL)
BEGIN
      exec @ExitCode = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@RaiserrMsg,@Err
      exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
END
END CATCH

if (@pReceipt is null)
   exec @ExitCode = dbo.[prc_CreateProcessReceipt] @pReceipt out,@Header,@StatusID,@Err,@RaiserrMsg

IF (@debug = 1)
BEGIN
   SET @msg = 'END Procedure ' + @ProcName
   exec @ExitCode = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg,@Err
   exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
END
RETURN @Err