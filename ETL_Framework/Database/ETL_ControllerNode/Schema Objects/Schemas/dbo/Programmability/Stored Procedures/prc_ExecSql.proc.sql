/*
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @pProcessReceipt xml
exec dbo.prc_CreateHeader @pHeader out,-11,2,null,4,1
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext
select @pProcessRequest
exec dbo.prc_ExecSql @pProcessRequest,@pProcessReceipt out
select @pProcessReceipt
*/
CREATE PROCEDURE [dbo].[prc_ExecSql]
    @pRequest xml([ETLController])
   ,@pReceipt  xml([ETLController]) = NULL OUTPUT
   ,@pSQLAttribute varchar(100) = NULL
As
/******************************************************************
**D File:         prc_ExecSql.SQL
**
**D Desc:         Execute Sql Statement
**
**D Auth:         andreys
**D Date:         10/28/2007
**
** Param: @pRequest  - BatchID info
                  @pReceipt results (StatusID - 2 - SUCCESS, 3 - FAILURE, 4 - ERROR) (OUTPUT only)
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

DECLARE @nSql nvarchar(max)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0
SET @pSQLAttribute = ISNULL(@pSQLAttribute,'SQL')

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

begin try
exec @ProcErr = dbo.[prc_ReadProcessRequest] @pRequest,@Header out,@Context out,@handle out
exec @ProcErr = dbo.[prc_ReadHeader] @Header,@BatchID out,@StepID out,null,@RunID out,@Options out

set @debug = nullif(@Options & 1,0)
IF (@debug IS NOT NULL)
BEGIN
   SET @msg = 'BEGIN Procedure ' + @ProcName
                           + ' with @BatchID=' + CAST(@BatchID AS nvarchar(30))
                           + ISNULL( ', @StepID=' +CAST(@StepID AS nvarchar(30)),'')

   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END


-------------------------------------------------------------------
--Retrieve SQL Attribute
-------------------------------------------------------------------

exec @ProcErr = dbo.[prc_ReadContextAttributes] @pRequest,@Attributes out
exec @ProcErr = dbo.[prc_ReadAttribute] @Attributes,@pSQLAttribute,@nSql out

IF (@nSql IS NULL)
BEGIN
  SET @ErrMsg = '   ERROR 50110: failed to retrieve Attribute ' + @pSQLAttribute + '('
                       + CAST(@BatchID AS nvarchar(30))
                       + ISNULL( ',' +CAST(@StepID AS nvarchar(30)),'') + ')'
  SET @StatusID = @STAT_ERROR
  SET @Err = 50110
  raiserror (@ErrMsg,11,11)
END
-------------------------------------------------------------------
--Execute
-------------------------------------------------------------------
IF (@debug IS NOT NULL)
BEGIN
   SET @msg = '   Executing: ' + @nSql
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END

SET @nSql = ' begin try ' + @nSql + ' end try'
          + ' begin catch' 
          + ' declare @msg nvarchar(max),@err int'
          + ' set @msg = error_message()'
          + ' set @Err = error_number()'
          + ' raiserror(''ERROR %d: %s'',11,11,@Err,@msg)'
          + ' end catch'

begin try
EXEC sp_executesql
            @nSql
           ,N'@receipt xml(ETLController) output,@request xml(ETLController)'
           ,@request = @pRequest
           ,@receipt = @pReceipt out
end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg,@Err
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
   set @ErrMsg = '   ERROR failed to execute: '  + isnull(@nSql,'null')
   raiserror (@ErrMsg,11,11)
end catch

SET @StatusID = @STAT_SUCCESS
end try
begin catch
set @err = error_number()
set @ErrMsg = isnull(@ErrMsg,error_message())
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