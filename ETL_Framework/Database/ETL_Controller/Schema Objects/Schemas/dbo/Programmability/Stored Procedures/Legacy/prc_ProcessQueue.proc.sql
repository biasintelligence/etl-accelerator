/*
declare @BatchID int
set @BatchID = -20
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @pProcessReceipt xml
exec dbo.prc_CreateHeader @pHeader out,@BatchID,1,2,4,1
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext
select @pProcessRequest
exec dbo.prc_ConstraintCheck @pProcessRequest,@pProcessReceipt out
select @pProcessReceipt
*/
CREATE PROCEDURE [dbo].[prc_ProcessQueue]
As
/******************************************************************
**D File:         prc_ProcessQueue.SQL
**
**D Desc:         Process Queue record
**
**D Auth:         andreys
**D Date:         10/27/2007
**
** Param:
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
**  2010-03-28       andrey@biasintelligence.com           kill all threads on the same conversation on Cancel
**  2010-04-06       andrey@biasintelligence.com           try/catch to improve OnSuccess/OnFailure 
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ExitCode INT
DECLARE @StepErr INT
DECLARE @Cnt INT
DECLARE @ProcName sysname
DECLARE @StatusID tinyint
DECLARE @StepStatusID tinyint
DECLARE @msg nvarchar(max)
DECLARE @RaiserrMsg nvarchar(max)
DECLARE @RetryCnt smallint

DECLARE @nSql nvarchar(max)

DECLARE @BatchID INT
DECLARE @StepID INT
DECLARE @RunID int
DECLARE @Options INT
DECLARE @debug TINYINT

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @ExitCode = 0
SET @Err = 0

DECLARE @OnStepSuccess nvarchar(max)
DECLARE @OnStepFailure nvarchar(max)
DECLARE @StepProcess nvarchar(max)
DECLARE @StepIgnore tinyint

DECLARE @STAT_AVAILABLE TINYINT
DECLARE @STAT_STARTED TINYINT
DECLARE @STAT_SUCCESS TINYINT
DECLARE @STAT_FAILURE TINYINT
DECLARE @STAT_ERROR TINYINT
DECLARE @STAT_WARNING TINYINT
DECLARE @STAT_FAILURE_IMMEDIATE TINYINT

SET @STAT_AVAILABLE = 0
SET @STAT_STARTED = 1
SET @STAT_SUCCESS = 2
SET @STAT_FAILURE = 3
SET @STAT_ERROR = 4
SET @STAT_WARNING = 5
SET @STAT_FAILURE_IMMEDIATE = 6
-------------------------------------------------------------------
--Step Statuses
--0 - Available
--1 - Started
--2 - Success
--3 - Failure
--4 - Error
--5 - Warning
--6 - Failure with abort.Used only in constraint procs to abort constraint check
-------------------------------------------------------------------
DECLARE @srcHandle AS UNIQUEIDENTIFIER
DECLARE @srcGrpHandle AS UNIQUEIDENTIFIER
DECLARE @Handle AS UNIQUEIDENTIFIER
DECLARE @GrpHandle AS UNIQUEIDENTIFIER
DECLARE @message AS NVARCHAR(MAX)
DECLARE @message_type AS NVARCHAR(256)
DECLARE @ProcessRequest AS xml (ETLController)
DECLARE @ProcessReceipt AS xml (ETLController)
DECLARE @ProcessInfo AS xml (ETLController)
DECLARE @Header AS xml (ETLController)
DECLARE @BatchHeader AS xml (ETLController)
DECLARE @Context AS xml (ETLController)
DECLARE @RetryDelay smallint
DECLARE @Wait nvarchar(30)
DECLARE @ErrXML xml
DECLARE @dialogtable sysname
DECLARE @spid int
DECLARE @timeout int

--if timer is not restarted by main thread test message request executed on @PING <= 120 sec intervals
--the conversation will be terminated by target thread
--this is done to terminate the remote processes on the main thread abort
set @timeout = 300; --sec 

begin try --external main block

--dummy header
EXEC @ExitCode = dbo.[prc_CreateHeader] @Header out,0,0,0,0,0

TryAgain:

SET @handle = null
;RECEIVE TOP (1) 
 @Handle = conversation_handle
,@message = cast(message_body as nvarchar(max))
,@message_type = message_type_name
FROM [ETLController_Request_Queue]

IF (@handle IS NULL)
BEGIN
  RETURN @Err
END


--check the queue for TEST messages before cancelling on DialogTimer
--Broker may not spawn threads on user messages when sql server is too busy
IF (@message_type = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer')
BEGIN
  select @message = cast(message_body as nvarchar(max))
    from [ETLController_Request_Queue]
   where conversation_handle = @Handle
     and message_type_name = 'ETLController_Test';
       
  if (@message is not null)
  begin
     IF (@debug = 1)
     BEGIN
         SET @msg = 'WARNING: Found ETLController_Test message in the queue on DialogTimer'
         --cant send this print to the log
         PRINT @msg
      END
      SET @message_type = 'ETLController_Test'
  end
END


set @dialogtable = 'dbo.' + quotename('dialog_' + cast(@Handle as nvarchar(36)));
IF (@message_type in ('http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog' --end conversation request
                     ,'http://schemas.microsoft.com/SQL/ServiceBroker/Error' --error
                     ,'ETLController_Cancel' -- cancel request
                     ,'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer')) --on timeout
BEGIN


   if (OBJECT_ID(@dialogtable,'u') is not null)
   begin
   --kill all active sessions on this conversation
   --except itself
      set @nSql = '
   declare @nsql nvarchar(4000);
      set @nsql = '''';
   select @nsql = @nsql + ''kill '' + cast(t.spid as nvarchar(30)) + '';''
     from ' + @dialogtable + ' t
     join sys.dm_broker_activated_tasks s
       on t.spid = s.spid
      and t.queue_id = s.queue_id 
    where t.spid <> @@spid;
   if (len(@nsql) > 0)               
      exec (@nsql);
   drop table ' + @dialogtable + ';
   '
      exec sp_executesql @nSql;
   end
    
   END CONVERSATION @Handle
   SET @Handle = null
   RETURN @Err
END


IF isnull(@message_type,'Unknown') not in ('ETLController_Request','ETLController_Test')
BEGIN
   SET @msg = '   WARNING: Message Type: [' + @message_type + '] is received. The message is discarded.'
   --cant send this print to the log
   PRINT @msg

   RETURN @Err
   --GOTO TryAgain
END

-- create dialogtable for a new conversation
-- this table will hold all active session_ids for this conversation
exec ('
if object_id(''' + @dialogtable + ''',''u'') is null
  create table ' + @dialogtable + '
  (spid int primary key,queue_id int,start_time datetime,runid int)
')

begin try --internal main

SET @ProcessRequest = @message
exec @ExitCode = dbo.[prc_ReadProcessRequest] @ProcessRequest,@Header out,@Context out,@srcHandle out,@srcGrpHandle out
exec @ExitCode = dbo.[prc_ReadHeader] @Header,@BatchID out,@StepID out,null,@RunID out,@Options out

SET @debug = isnull(@Options & 1,0)
SET @StatusID = @STAT_SUCCESS

EXEC @ExitCode = dbo.[prc_CreateHeader] @BatchHeader out,@BatchID,null,null,@RunID,@Options,1--batch

IF @message_type = 'ETLController_Request'
BEGIN

   SET @msg =  'BEGIN Procedure ' + @ProcName + ' with message type ' + @message_type
                     + ' BatchID=' + CAST(@BatchID as nvarchar(30))
                     + ' StepID=' + isnull(CAST(@StepID as nvarchar(30)),0)
   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle

-------------------------------------------------------------------
--register with @dialogtable
-------------------------------------------------------------------
   set @nSql = '
insert ' + @dialogtable + '
  (spid,queue_id,start_time,runid)
select spid,queue_id,getdate(),@RunID
  from sys.dm_broker_activated_tasks
 where spid = @@spid;
'
   exec sp_executesql @nsql,N'@RunID int',@RunID = @RunID;

   IF (@debug = 1)
   BEGIN
      SET @msg = '   session_id=' + CAST(@@spid AS nvarchar(30))
               + ' added to ' + @dialogtable + ' table'
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg,@Err
      exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
   END

   exec @ExitCode = dbo.[prc_CreateProcessRequest] @ProcessRequest out,@Header,@Context,@handle
-------------------------------------------------------------------
--Check Step Constraints
-------------------------------------------------------------------
begin try --constraints
   SET @ProcessReceipt = NULL
   EXEC @ExitCode = dbo.[prc_ConstraintCheck] @ProcessRequest,@ProcessReceipt out
   EXEC @ExitCode = dbo.[prc_ReadProcessReceipt] @ProcessReceipt,null,@StatusID out,@Err out,@msg out

   SET @StatusID = ISNULL(@StatusID,@STAT_ERROR)
   IF (@debug = 1)
   BEGIN
      SET @msg = '   prc_ConstraintCheck returns StatusID=' + CAST(@StatusID AS nvarchar(30))
               + ' with msg:' + isnull(@msg,'null')
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg,@Err
      exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
   END


   SET @msg = null
   IF (@StatusID = @STAT_FAILURE_IMMEDIATE)
   BEGIN
      SET @RaiserrMsg = '   ERROR receive abort from Step Constraints check'
      RAISERROR(@RaiserrMsg,11,11)
   END
   ELSE IF (@StatusID = @STAT_FAILURE)
   BEGIN
      SET @msg = '   ERROR Step Constraints were not met'
   END
   ELSE IF (@Err <> 0 OR @StatusID = @STAT_ERROR)
   BEGIN
      SET @msg = '   ERROR checking Step Constraints'
   END

   IF (@msg IS NOT NULL)
   BEGIN
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg,@Err
      exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
   END

end try --constraints
begin catch
   set @RaiserrMsg = ERROR_MESSAGE()
   raiserror (@RaiserrMsg,11,11)
end catch

   --ELSE --IF (@StatusID = @STAT_SUCCESS)
   --BEGIN
-------------------------------------------------------------------
--Process Step
-------------------------------------------------------------------
      ;with xmlnamespaces('ETLController.XSD' as etl)
      select
             @OnStepSuccess = 'exec @ExitCode = ' + cs.s.value('(./etl:OnSuccess/etl:Process)[1]','nvarchar(max)') + ' @pRequest=@Request,@pReceipt=@Receipt out' 
                            + isnull(',' + cs.s.value('(./etl:OnSuccess/etl:Param)[1]','nvarchar(max)'),'')
            ,@OnStepFailure = 'exec @ExitCode = ' + cs.s.value('(./etl:OnFailure/etl:Process)[1]','nvarchar(max)') + ' @pRequest=@Request,@pReceipt=@Receipt out' 
                            + isnull(',' + cs.s.value('(./etl:OnFailure/etl:Param)[1]','nvarchar(max)'),'')
            ,@StepProcess = 'exec @ExitCode = ' + cs.s.value('(./etl:Process/etl:Process)[1]','nvarchar(max)') + ' @pRequest=@Request,@pReceipt=@Receipt out' 
                            + isnull(',' + cs.s.value('(./etl:Process/etl:Param)[1]','nvarchar(max)'),'')
            ,@StepIgnore  = coalesce(cs.s.value('(./@IgnoreErr)[1]','tinyint'),cb.b.value('(./@IgnoreErr)[1]','tinyint'),0)
            ,@RetryCnt = coalesce(cs.s.value('(./@Retry)[1]','smallint'),cb.b.value('(./@Retry)[1]','smallint'),0)
            ,@RetryDelay = coalesce(cs.s.value('(./@Delay)[1]','smallint'),cb.b.value('(./@Delay)[1]','smallint'),60) --sec
        from @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)
        cross apply cb.b.nodes('./etl:Steps/etl:Step[@StepID=(sql:variable("@StepID"))]') cs(s)


      IF (@debug = 1)
      BEGIN
         SET @msg = '   Retrieve Step Parameters'
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
         exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
      END


      SET @Wait = right('00' + cast(@RetryDelay/3600 as varchar(10)),3)
                + ':' + right('0' + cast((@RetryDelay/60)%60 as varchar(10)),2)
                + ':' + right('0' + cast(@RetryDelay%60 as varchar(10)),2)


      IF (@StepProcess IS NULL)
      BEGIN
         SET @StatusID = @STAT_ERROR
         SET @Err = 50120
         SET @RaiserrMsg = '   ERROR Step Process is not found'
         RAISERROR (@RaiserrMsg,11,11)
      END
      ELSE IF (@StatusID IN (@STAT_SUCCESS,@STAT_WARNING))
      BEGIN

begin try --process step

         IF (@debug = 1)
         BEGIN
            SET @msg = '   Step Process: ' + @StepProcess
            exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
            exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
         END

         WHILE (1 = 1)
         BEGIN
            SET @ProcessReceipt = NULL
            EXEC sp_ExecuteSQL @StepProcess
                  ,N'@ExitCode int output,@Request xml,@Receipt xml output'
                  ,@ExitCode = @ExitCode output
                  ,@Request = @ProcessRequest
                  ,@Receipt = @ProcessReceipt output
      
            EXEC @ExitCode = dbo.[prc_ReadProcessReceipt] @ProcessReceipt,null,@StatusID out,@Err out,@msg out
            SET @StatusID = ISNULL(@StatusID,@STAT_ERROR)

            --need to make sure the conversation is still active before executing onSuccess
            --just trying to send a message would do
            --IF (@debug = 1)
            --BEGIN 
               SET @msg = '   Retry(' + CAST(@RetryCnt AS nvarchar(30))
                        + ' with delay ' + CAST(@RetryDelay AS nvarchar(30)) +' sec)'
                        + ' Step process returns StatusID=' + CAST(@StatusID AS nvarchar(30))
                        + ' with msg: ' + isnull(@msg,'null')
               exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg,@Err
               exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
            --END
            IF (@StatusID IN (@STAT_SUCCESS,@STAT_WARNING,@STAT_FAILURE_IMMEDIATE) OR @RetryCnt = 0 )
               BREAK

            WAITFOR DELAY @wait 
            IF (@RetryCnt <= -32000)
               SET @RetryCnt = 0

            SET @RetryCnt = @RetryCnt - 1
         END
      --END

end try
begin catch
   set @StatusID = @STAT_ERROR;
   set @Err = case @Err when 0 then ERROR_NUMBER() else @Err end;
end catch
-------------------------------------------------------------------
--OnSuccess or OnFailure
-------------------------------------------------------------------
      SET @StepStatusID = @StatusID
      SET @StepErr = @Err
      SET @nSql = NULL
      IF (@StatusID IN (@STAT_SUCCESS,@STAT_WARNING))
      BEGIN
         SET @nSql = @OnStepSuccess
         IF (@debug = 1)
         BEGIN
            SET @msg = '   OnStepSuccess: ' +ISNULL(@OnStepSuccess,'NULL')
            exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
            exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
         END
      END
      ELSE IF (@StatusID IN (@STAT_FAILURE,@STAT_ERROR))
      BEGIN
         SET @nSql = @OnStepFailure
         IF (@debug = 1)
         BEGIN
            SET @msg = '   OnStepFailure: ' +ISNULL(@OnStepFailure,'NULL')
            exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
            exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
         END
      END

      IF (@nSql IS NOT NULL)
      BEGIN
         SET @ProcessReceipt = NULL
         EXEC sp_ExecuteSQL @nSql
                  ,N'@ExitCode int output,@Request xml,@Receipt xml output'
                  ,@ExitCode = @ExitCode output
                  ,@Request = @ProcessRequest
                  ,@Receipt = @ProcessReceipt output
      
         EXEC @ExitCode = dbo.[prc_ReadProcessReceipt] @ProcessReceipt,null,@StatusID out,@Err out,@msg out

         SET @StatusID = ISNULL(@StatusID,@STAT_ERROR)

         IF (@debug = 1)
         BEGIN
            SET @msg = '  OnSuccess/OnFailure Completed with StatusID=' + CAST(@StatusID AS nvarchar(100))
                        + ' with msg: ' + isnull(@msg,'null')
            exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg,@Err
            exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
         END
         IF (@Err <> 0 OR @StatusID = @STAT_ERROR)
         BEGIN
            SET @msg = '   ERROR failed to execute OnSuccess/OnFailure code'
            exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg,@Err
            exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
        END


         IF(@StatusID IN (@STAT_SUCCESS,@STAT_WARNING))
         BEGIN
            SET @StatusID = @StepStatusID
            SET @Err = @StepErr
         END

      END
   END
-------------------------------------------------------------------
--Finish the step
-------------------------------------------------------------------

--remove current spid from @dialogtable
   set @nSql = 'delete ' + @dialogtable + ' where spid = @@spid;'
   exec sp_executesql @nSql,N'@RunID int',@RunID = @RunID

   IF (@debug = 1)
   BEGIN
      SET @msg = '   StepID=' + CAST(@StepID AS nvarchar(30)) + ' completed with StatusID=' + CAST(@StatusID AS nvarchar(30))
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg,@Err
      exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
   END

END
ELSE IF @message_type = 'ETLController_Test'
BEGIN
   if (@debug = 1)
   begin
      SET @msg =  'BEGIN Procedure ' + @ProcName + ' with message type ' + @message_type
                     + ' BatchID=' + CAST(@BatchID as nvarchar(30))
                     + ' SvcID=' + isnull(CAST(@StepID as nvarchar(30)),0)
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
      exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
   end
   --restart the timer
   BEGIN CONVERSATION TIMER (@handle) TIMEOUT = @timeout ;
END
ELSE
BEGIN
   IF (@debug = 1)
   BEGIN
      SET @msg = '   Message Type: [' + @message_type + '] ' + @message
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
      exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
   END
END
end try --internal main
BEGIN CATCH
SET @Err = case @Err when 0 then ERROR_NUMBER() else @Err end;
SET @RaiserrMsg = isnull(@RaiserrMsg,ERROR_MESSAGE())
SET @StatusID = isnull(nullif(@StatusID,@STAT_SUCCESS),@STAT_ERROR)
IF (@RaiserrMsg IS NOT NULL)
BEGIN
   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@RaiserrMsg,@Err
   exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle
END
END CATCH

exec @ExitCode = dbo.prc_CreateProcessReceipt @ProcessReceipt out,@Header,@StatusID,@Err,@RaiserrMsg


IF (@message_type = 'ETLController_Request')
begin
   SET @msg =  'END Procedure ' + @ProcName + ' for message type ' + @message_type
                     + ' BatchID=' + isnull(CAST(@BatchID as nvarchar(30)),'null')
                     + ' StepID=' + isnull(CAST(@StepID as nvarchar(30)),'null')
   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg,@Err
   exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle;

    SEND ON CONVERSATION @handle
    MESSAGE TYPE [ETLController_Receipt]
   (CAST(@ProcessReceipt AS varbinary(MAX)));
end
ELSE IF (@message_type = 'ETLController_Test')
begin
   if (@debug = 1)
   begin
      SET @msg =  'END Procedure ' + @ProcName + ' for message type ' + @message_type
                     + ' BatchID=' + isnull(CAST(@BatchID as nvarchar(30)),'null')
                     + ' SvcID=' + isnull(CAST(@StepID as nvarchar(30)),'null')
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg,@Err
      exec @ExitCode = dbo.[prc_Print] @ProcessInfo,@handle;
   end;
   
    SEND ON CONVERSATION @handle
    MESSAGE TYPE [ETLController_Test]
   (CAST(@ProcessReceipt AS varbinary(MAX)))

end
end try --main block
begin catch
   SET @Err = case @Err when 0 then ERROR_NUMBER() else @Err end;
   SET @RaiserrMsg = ERROR_MESSAGE()
   raiserror ('%d, %s',11,17,@Err,@RaiserrMsg);
end catch

RETURN @Err