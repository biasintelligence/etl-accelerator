/*
select * from ETLSteprunhistory where batchid = -15

declare @pHeader xml
exec dbo.prc_CreateHeader @pHeader out,-15,null,null,6,1
exec dbo.prc_AttributeSet @pHeader,'CheckFile','c:\20080707_DailyMapsReady.txt'
--xp_cmdshell 'dir c:\20080707_DailyMapsReady.txt'

declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @pProcessReceipt xml
exec dbo.prc_CreateHeader @pHeader out,-15,1,null,5,1
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext
--select @pProcessRequest
exec dbo.prc_FileCheck @pProcessRequest,@pProcessReceipt out
select @pProcessReceipt
*/
CREATE PROCEDURE dbo.prc_FileCheck
    @pRequest xml([ETLController])
   ,@pReceipt  xml([ETLController]) = NULL OUTPUT
As
/******************************************************************
**D File:         prc_FileCheck.SQL
**
**D Desc:         Check the file presence
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
DECLARE @FileName varchar(1000)
DECLARE @nValue nvarchar(max)
DECLARE @RunID int
DECLARE @Options int

DECLARE @CheckContext nvarchar(1000)

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

exec @ProcErr = dbo.[prc_ReadAttribute] @Attributes,'CheckFile',@CheckContext out
if (@CheckContext is null)
BEGIN
   SET @ErrMsg = '   ERROR 50110: File Location is required:(CheckFile = <fullpath>)'
   SET @StatusID = @STAT_ERROR
   SET @Err = 50110
   raiserror (@ErrMsg,11,11)
END

----------------------------------------------------------------------------------
--Checking...
----------------------------------------------------------------------------------
set @FileName = reverse(@CheckContext)
set @FileName = reverse(left(@FileName,isnull(nullif(charindex('\',@FileName),0),len(@FileName) + 1) - 1))

--IF (@debug IS NOT NULL)
--BEGIN
   SET @msg = '   Waiting for File: ' + @CheckContext
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
--END

declare @t table (msg nvarchar(1000))
set @Value = '/C dir ' + @CheckContext + ' /B'
insert @t
exec @ProcErr = prc_CLRExecuteDE 'cmd',@Value,0,'rowset'
--if (@ProcErr <> 0)
--BEGIN
   --SET @ErrMsg = '   ERROR 50111: prc_CLRExecuteDE returned %d'
   --SET @StatusID = @STAT_ERROR
   --SET @Err = 50111
   --raiserror (@ErrMsg,11,11,@ProcErr)
--END


if exists (select 1 from @t where @Filename = msg)
   set @StatusID = @STAT_SUCCESS
else
   set @StatusID = @STAT_FAILURE

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