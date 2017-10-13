/*
declare @m nvarchar(max)
declare @pHeader xml
declare @pProcessInfo xml
exec dbo.prc_CreateHeader @pHeader out,1,null,null,4,1
select @pHeader
exec dbo.prc_CreateProcessInfo @pProcessInfo out,@pHeader,'xxx'
select @pProcessInfo
exec dbo.prc_Print @pProcessInfo
*/

CREATE PROCEDURE [dbo].[prc_Print]
      @pProcessInfo xml([ETLController])
     ,@pConversation uniqueidentifier = NULL
     ,@pConversationGrp uniqueidentifier = NULL
As
/******************************************************************
**D File:         prc_Print.SQL
**
**D Desc:         Print or Send a processing Info message
**
**D Auth:         andreys
**D Date:         10/27/2007
**
** Param:
        @pProcessInfo       - message object
        @pConversation      - conversation handle to send the message
        @pConversationGrp   - conversation group handle
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @Now nvarchar(30)
DECLARE @msg nvarchar(max)
DECLARE @RunID int
DECLARE @BatchID int
DECLARE @StepID int
DECLARE @Options int
DECLARE @debug int
DECLARE @Header xml(ETLController)

SET @Err = 0
-------------------------------------------------------------------
--Return Statuses
--2 - Success
--3 - Failure
--4 - Error
-------------------------------------------------------------------
BEGIN TRY
-- DE should be provided with Master Srv and Dd which will not work with send
-- since conversation handle comes from Slave
-- disable send and rely on standard output for Master Print instead
-- and use send on Slave only. Slave node should not do table insert
set @pConversation = null;
IF (@pConversation IS NOT NULL)
BEGIN

   RAISERROR ('not implemented',0,1) WITH NOWAIT
  --;SEND ON CONVERSATION @pConversation
  -- MESSAGE TYPE [ETLController_InfoMessage]
  --    (CAST(@pProcessInfo AS varbinary(max)));

END
ELSE
BEGIN
   exec dbo.[prc_ReadProcessInfo] @pProcessInfo,@Header out,@msg out,@Err out;
   exec dbo.[prc_ReadHeader] @Header,@BatchID out,@StepID out,null,@RunID out,@Options out;

   SET @debug = ISNULL(@Options & 1,0);
   IF (isnull(@RunID,0) <> 0 OR @debug = 1)
   BEGIN
     --PRINT @msg;

     INSERT dbo.[ETLStepRunHistoryLog]
      (RunID,BatchID,StepID,LogDT,Err,LogMessage)
     VALUES(@RunID,isnull(@BatchID,0),isnull(@StepID,0),getdate(),isnull(@Err,0),@msg);
   END
     
   SET @Err = 0;
END

END TRY
BEGIN CATCH
    SET @Err = ERROR_NUMBER();
    SET @msg = ERROR_MESSAGE();
    RAISERROR('Error %d %s',11,11,@Err,@msg);
END CATCH

RETURN @Err