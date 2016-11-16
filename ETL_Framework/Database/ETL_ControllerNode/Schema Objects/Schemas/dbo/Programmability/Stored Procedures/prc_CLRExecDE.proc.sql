-- Stored procedure
/*
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @Attributes xml
declare @Timeout int
--declare @pProcessReceipt xml
exec dbo.prc_CreateHeader @pHeader out,-10,1,null,4,1
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext
--select @pProcessRequest
exec dbo.prc_ReadContextAttributes @pProcessRequest,@Attributes out
exec dbo.prc_ReadAttribute @Attributes,'etl:Timeout',@Timeout out

select @Timeout
select @Attributes

--exec dbo.prc_ClrExecDE @pProcessRequest,@pProcessReceipt out
select @pProcessReceipt
*/
CREATE PROCEDURE [dbo].[prc_CLRExecDE]
    @pRequest xml([ETLController])
   ,@pReceipt  xml([ETLController]) = NULL OUTPUT
   ,@pDELocation nvarchar(100) = NULL
As
/******************************************************************
**D File:         prc_ExecCmd.SQL
**
**D Desc:         Run Delta Extractor
**
**D Auth:         andreys
**D Date:         12/15/2007
**
** Param: @pRequest  - BatchID info
                  @pReceipt results (StatusID - 2 - SUCCESS, 3 - FAILURE, 4 - ERROR) (OUTPUT only)
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
**  2009/11/10       andrey@biasintelligence.com           use ExecuteDE sqlclr function

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
DECLARE @RunID int
DECLARE @Options int
DECLARE @Name nvarchar(100)
DECLARE @Timeout int
DECLARE @deOptions nvarchar(100)

DECLARE @nSql nvarchar(max)
DECLARE @nCmd nvarchar(max)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0
SET @pDELocation = ISNULL(@pDELocation,'DEPath')

DECLARE @STAT_SUCCESS TINYINT
DECLARE @STAT_FAILURE TINYINT
DECLARE @STAT_ERROR TINYINT

SET @STAT_SUCCESS = 2
SET @STAT_FAILURE = 3
SET @STAT_ERROR = 4
-------------------------------------------------------------------
--Return Statuses
--2 - Success
--3 - Failure
--4 - Error
-------------------------------------------------------------------
DECLARE @Request AS xml (ETLController)
DECLARE @Receipt AS xml (ETLController)
DECLARE @Header AS xml (ETLController)
DECLARE @Context AS xml (ETLController)
DECLARE @ProcessInfo AS xml (ETLController)
DECLARE @Attributes AS xml (ETLController)
DECLARE @Parameters as xml (ETLClient_DE)

begin try
exec @ProcErr = dbo.[prc_ReadProcessRequest] @pRequest,@Header out,@Context out,@handle out
exec @ProcErr = dbo.[prc_ReadHeader] @Header,@BatchID out,@StepID out,null,@RunID out,@Options out

set @debug = @Options & 1
IF (@debug = 1)
BEGIN
   SET @msg = 'BEGIN Procedure ' + @ProcName
                           + ' with @BatchID=' + CAST(@BatchID AS nvarchar(30))
                           + ISNULL( ', @StepID=' +CAST(@StepID AS nvarchar(30)),'')

   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END


-------------------------------------------------------------------
--Retrieve DEPath Attribute
-------------------------------------------------------------------

exec @ProcErr = dbo.[prc_ReadContextAttributes] @pRequest,@Attributes out
exec @ProcErr = dbo.[prc_ReadAttribute] @Attributes,@pDELocation,@nCmd out

IF (@nCmd IS NULL)
BEGIN
  SET @ErrMsg = '   ERROR 50110: failed to retrieve Attribute ' + @pDELocation + '('
                       + CAST(@BatchID AS nvarchar(30))
                       + ISNULL( ',' +CAST(@StepID AS nvarchar(30)),'') + ')'
  SET @StatusID = @STAT_ERROR
  SET @Err = 50110
  raiserror (@ErrMsg,11,11)
END

-------------------------------------------------------------------
--Timeout
-------------------------------------------------------------------
exec @ProcErr = dbo.[prc_ReadAttribute] @Attributes,'etl:Timeout',@Timeout out
set @Timeout = ISNULL(@Timeout,0)

-------------------------------------------------------------------
--Execute
-------------------------------------------------------------------
exec @ProcErr = dbo.prc_de_CreateParameters @Parameters out,@pRequest


IF (@debug = 1)
BEGIN
   SET @msg = '   Executing: ' + @nCmd + ' with:' + cast(@Parameters as nvarchar(max))
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END

--set @nSql = '"' + dbo.fn_GetBase64String('<?xml version="1.0"?>' + cast(@Parameters as nvarchar(max))) + '"'
set @nSql = '"' + replace('<?xml version="1.0"?>' + cast(@Parameters as nvarchar(max)),'"','\"') + '"'

declare @results table  (id int not null identity(1,1),msg nvarchar(max) null)
declare @rows int, @loop int

begin try
set @deOptions = case @debug when 1 then 'debug;' else '' end + 'rowset'
insert @results (msg)
exec @err = dbo.[prc_CLRExecuteDE] @nCmd,@nSql,@Timeout,@deOptions
set @rows = @@ROWCOUNT

if (@err <> 0) raiserror ('ERROR  dbo.[prc_CLRExecuteDE] exits with status %d',11,11,@Err)
if (@debug = 1)
begin
   while (@rows > 0)
   begin
      set @loop = null
      select top 1 @msg = msg,@loop = id from @results
      if (@loop is null)
         break
      set @msg = isnull(rtrim(@msg),'')
      if (len(@msg) > 0)
      begin
         exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg,@Err
         exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
      end
      delete @results where id = @loop
      set @rows = @rows - 1
   end
end
end try
begin catch
   set @Err = error_number()
   while (@rows > 0)
   begin
      set @loop = null
      select top 1 @msg = msg,@loop = id from @results
      if (@loop is null)
         break
      set @msg = isnull(rtrim(@msg),'')
      if (len(@msg) > 0)
      begin
         exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg,@Err
         exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
      end
      delete @results where id = @loop
      set @rows = @rows - 1
   end
   set @msg = error_message()
   raiserror('ERROR %d: %s',11,11,@Err,@msg)
end catch

SET @StatusID = @STAT_SUCCESS
end try
begin catch
set @ErrMsg = isnull(@ErrMsg,error_message())
set @err = error_number()
IF (@ErrMsg IS NOT NULL)
BEGIN
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@ErrMsg,@Err
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END
set @StatusID = @STAT_ERROR
--set @StatusID = @STAT_FAILURE
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