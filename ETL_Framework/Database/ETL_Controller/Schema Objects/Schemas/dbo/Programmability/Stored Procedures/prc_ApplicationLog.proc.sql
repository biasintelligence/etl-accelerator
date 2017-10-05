/*
exec dbo.prc_ApplicationLog 'test message',1,1,1,1
*/

CREATE PROCEDURE [dbo].[prc_ApplicationLog]
      @pMessage nvarchar(max)
     ,@pErr int = null
     ,@pBatchId int = null
     ,@pStepId int = null
     ,@pRunId int = 0
     ,@pConversation uniqueidentifier = NULL
     ,@pConversationGrp uniqueidentifier = NULL
     ,@pOptions nvarchar(100) = null
As
/******************************************************************
**D File:         prc_ApplicationLog.SQL
**
**D Desc:         Log external message
**
**D Auth:         andreys
**D Date:         06/27/2009
**
** Param:
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
DECLARE @ProcessInfo xml(ETLController)

SET @Err = 0
-------------------------------------------------------------------
--Return Statuses
--2 - Success
--3 - Failure
--4 - Error
-------------------------------------------------------------------
BEGIN TRY
    

set @BatchID = ISNULL(@pBatchID,0)
set @StepID = ISNULL(@pStepID,0)
set @RunID = ISNULL(@pRunID,0)
set @debug = CASE WHEN CHARINDEX('debug',@pOptions) > 0 THEN 1 ELSE 0 END;


--use conversation only for Slave; use table insert for master
set @pConversation = NULL;
IF (@pConversation IS NOT NULL)
BEGIN
   exec dbo.[prc_CreateHeader] @Header out,@BatchId,@StepId,null,@RunId,@debug
   exec dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@pMessage,@pErr

   --RAISERROR ('not implemented',0,1) WITH NOWAIT
  ;SEND ON CONVERSATION @pConversation
   MESSAGE TYPE [ETLController_InfoMessage]
      (CAST(@ProcessInfo AS varbinary(max)))

END
ELSE
BEGIN

   if (@debug = 1)
      PRINT @pMessage;
      
   IF (isnull(@RunID,0) <> 0)
     INSERT dbo.[ETLStepRunHistoryLog]
      (RunID,BatchID,StepID,LogDT,Err,LogMessage)
     VALUES(@RunID,isnull(@BatchID,0),isnull(@StepID,0),getdate(),isnull(@pErr,0),@pMessage)
END

END TRY
BEGIN CATCH
    SET @Err = ERROR_NUMBER()
    SET @msg = ERROR_MESSAGE()
    RAISERROR('Error %d %s',10,11,@Err,@msg)
END CATCH

RETURN @Err