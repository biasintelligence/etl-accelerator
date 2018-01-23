
CREATE PROCEDURE dbo.prc_ConversationCleanup
@options varchar(100) = null
As
/******************************************************************
**D File:         prc_ConversationCleanup.SQL
**
**D Desc:         clean up broker communication leftovers
**
**D Auth:         andreys
**D Date:         07/05/2007
**
** Param:
** @options:      all - remove all conversation including CO
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @Now nvarchar(30)
DECLARE @msg nvarchar(max)
DECLARE @SPID int
DECLARE @RunID int
DECLARE @handle uniqueidentifier
DECLARE @prevhandle uniqueidentifier
DECLARE @All tinyint

SET @Err = 0
SET @SPID = @@SPID
SET @All = case when @options like '%all%' then 1 else 0 end
-------------------------------------------------------------------
-------------------------------------------------------------------
--just for testing
--waitfor delay '00:00:01'

BEGIN TRY

   SELECT top 1 @handle = conversation_handle FROM sys.conversation_endpoints sc
     JOIN sys.services ss ON ((sc.service_id = ss.service_id AND ss.[name] like ('ETLController[_]%'))
       OR (sc.service_id = 0 AND far_service like ('ETLController[_]%')))
    WHERE (sc.state IN ('ER','DI','DO','CD') or @all = 1)

   WHILE @handle IS NOT NULL
   BEGIN
      SET @msg = 'Ending conversation [' + CAST(@handle AS nvarchar(100)) + ']'
      RAISERROR (@msg,0,1)
      ;END CONVERSATION @handle WITH CLEANUP

      SET @prevhandle = @handle
      SET @handle = NULL
      SELECT top 1 @handle = conversation_handle FROM sys.conversation_endpoints sc
        JOIN sys.services ss ON ((sc.service_id = ss.service_id AND ss.[name] like ('ETLController[_]%'))
          OR (sc.service_id = 0 AND far_service like ('ETLController[_]%')))
       WHERE (sc.state IN ('ER','DI','DO','CD') or @All = 1) 

      IF (@handle IS NULL)
         BREAK

      IF (@handle = @prevhandle)
      BEGIN
         SET @msg = 'Failed to end conversation [' + CAST(@handle AS nvarchar(100)) + ']'
         RAISERROR(@msg,11,11)
         BREAK
      END
   END

END TRY
BEGIN CATCH
    SET @Err = ERROR_NUMBER()
    SET @msg = ERROR_MESSAGE()
    RAISERROR('Error %d %s',11,11,@Err,@msg)
END CATCH

RETURN @Err