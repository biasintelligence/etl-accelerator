--select * from ETLbatch
--select * from ETLstep where batchid = -20
--prc_execute 'NTBC01','debug,forcestart,slaveoff'
CREATE PROCEDURE [dbo].[prc_Execute]
    @pBatchName nvarchar(30)= NULL
   ,@Options nvarchar(100) = NULL
   ,@pHandle uniqueidentifier = NULL
   ,@pContext xml(ETLController) = NULL
As
/******************************************************************
**D File:             prc_Execute.SQL
**
**D Desc:         execute ETL process
**
**D Auth:         andreys
**D Date:         10/30/2007
**
** Param: 
          @pBatchName - BatchName from the ETLBatch Table. NULL if pContext is specified
         ,@Options    - debug(produce debug output)
                       ,forcestart(force the batch to start even when the other instance is running)
                       ,wait(wait <TIMEOUT> for the other instance to finish)
                       ,slaveoff(execute all steps on local service only. Communications to remote services will not start)
         ,@pHandle    - conversation handle to communicate back to parent process
         ,@pContext   - ad-hoc batch in xml format. see context element definition in ETLController schema

** Batch level system Attributes:
**  MAXTHREAD -- max number parallel threads
**  TIMEOUT   -- receive timeout in sec
**  LIFETIME  -- dialog timeout in sec
**  PING      -- receive wait break for external error checks in sec
**  HISTRET   -- time to retain processing history in days
** Step level system Attributes
**  DISABLED  -- YES/NO
**  SEQGROUP  -- sequence group. All steps in a group executed sequentially in StepOrder order
**  PRIGROUP  -- priority group. Groupes executed sequentially in PRIGROP order.
**               But steps in a group in parallel in StepOrder order
**  LOOPGROUP -- loop support. Steps in loop group are executed recursivly until BreakEvent is posted to any of LoogGroup steps
**  RESTART   -- 1/0 restart step on batch error
**  SVCNAME   -- SSB Service name to send a step for execution to (ANY = least busy, LOCAL = ETLController_Process (master node)
**               DEFAULT = LOCAL or configurable through systemparameters)
** Constraint level
**  PING      -- interval to call constraint process in sec (default to 10)
**  DISABLED  -- YES/NO

*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
**  7/29/2008        andreys            wait option
**  2009/12/04       andrey@biasintelligence.com           handle Exit Event
**  2009/12/27       andrey@biasintelligence.com           loop support
**  2010/03/30       andrey@biasintelligence.com           add Send Cancel request
**  2010/04/01       andrey@biasintelligence.com           loop bug. if BreakEvent is posted from the last step the workflow will hang 
**  2010/05/16       andrey@biasintelligence.com           the workflow is left in running state after contraints were not met.
**  2010/07/17       andrey@biasintelligence.com           Master/Slave services. See SVCNAME attribute. By default run all steps on LOCAL Service (Master)
**                                      can be disabled by passing <slaveoff> keyword in options
**  2010/08/13       andrey@biasintelligence.com           fix force start recovery code. ETLBatch and ETLStep tables were not updated properly
**  2012/01/06       andrey@biasintelligence.com           fix step execution order
**  2014/01/20       andrey             fix IgnoreErr logic
******************************************************************/
SET NOCOUNT ON;

DECLARE @Err INT;
DECLARE @ExitCode INT;
DECLARE @Cnt INT;
--DECLARE @StepTable sysname
--DECLARE @nStepTable nvarchar(100)
DECLARE @ProcName sysname;
DECLARE @Trancount int;
DECLARE @Timeout int;
DECLARE @Loop int;
DECLARE @StepID int;
DECLARE @BatchID int;
DECLARE @nSQL1 nvarchar(max);
DECLARE @StatusID tinyint;
DECLARE @StatusOrder tinyint;
DECLARE @BatchStatusID tinyint;
DECLARE @BatchErr INT;
DECLARE @Now varchar(30);
DECLARE @msg nvarchar(max);

DECLARE @OnBatchSuccess sysname;
DECLARE @OnBatchFailure sysname;
DECLARE @BatchIgnore tinyint;
DECLARE @Restart tinyint;
DECLARE @RunID int;
DECLARE @LastStatusID tinyint;
DECLARE @LastRunID int;
DECLARE @CleanupRunID int;
DECLARE @checktimeout int;
DECLARE @SeqGroup nvarchar(10);
DECLARE @pDebug tinyint;
DECLARE @pForceStart tinyint;
DECLARE @pWait tinyint;
declare @pSlaveOff tinyint;
DECLARE @bOptions int;
DECLARE @RaiserrMsg nvarchar(max);
DECLARE @Wait varchar(20);
declare @StartDate datetime;
declare @StatusDate datetime;
declare @LocalService sysname;
declare @SvcName sysname;

--@pDebug is bitmap placeholder for options
-- 1 - debug
-- 2 - forcestart

SET @Trancount = @@TRANCOUNT;
--SET @StepTable = 'tempdb.dbo.' + @pBatchName
--SET @nStepTable = CAST(@StepTable AS nvarchar(100))
SET @ProcName = OBJECT_NAME(@@PROCID);
SET @Err = 0;
SET @ExitCode = 0;
SET @BatchErr = 0;
SET @LocalService = 'ETLController_Process'

SET @pDebug = CASE WHEN CHARINDEX('debug',@Options) > 0 THEN 1 ELSE 0 END;
SET @pForceStart = CASE WHEN CHARINDEX('forcestart',@Options) > 0 THEN 1 ELSE 0 END;
SET @pWait = CASE WHEN CHARINDEX('wait',@Options) > 0 THEN 1 ELSE 0 END;
SET @pSlaveOff = CASE WHEN CHARINDEX('slaveoff',@Options) > 0 THEN 1 ELSE 0 END;
SET @bOptions = isnull(@pDebug,0) + isnull(@pForceStart,0) * 2;

SET @checktimeout = 0;
SET @StartDate = GETDATE();

DECLARE @STAT_AVAILABLE TINYINT;
DECLARE @STAT_STARTED TINYINT;
DECLARE @STAT_SUCCESS TINYINT;
DECLARE @STAT_FAILURE TINYINT;
DECLARE @STAT_ERROR TINYINT;
DECLARE @STAT_WARNING TINYINT;
DECLARE @STAT_FAILURE_IMMEDIATE TINYINT;

SET @STAT_AVAILABLE = 0;
SET @STAT_STARTED = 1;
SET @STAT_SUCCESS = 2;
SET @STAT_FAILURE = 3;
SET @STAT_ERROR = 4;
SET @STAT_WARNING = 5;
SET @STAT_FAILURE_IMMEDIATE = 6;
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
DECLARE @ATT_MAXTHREAD TINYINT   -- max number parallel threads
DECLARE @ATT_TIMEOUT INT         -- receive timeout in sec
DECLARE @ATT_LIFETIME INT        -- dialog timeout in sec
DECLARE @ATT_PING_MAX INT        -- max PING value
DECLARE @ATT_PING INT            -- receive wait break for external error checks in sec
                                 -- defaults to @ATT_PING_MAX
DECLARE @ATT_HISTRET INT         -- time to retain processing history in days

--to test without broker
--create table #tmp(StepID int not null,StatusID tinyint not null)


DECLARE @handle AS UNIQUEIDENTIFIER
DECLARE @message_type NVARCHAR(256)
DECLARE @message NVARCHAR(MAX)
DECLARE @ProcessRequest AS XML (ETLController)
DECLARE @ProcessReceipt AS XML (ETLController)
DECLARE @ProcessInfo AS XML (ETLController)
DECLARE @Context AS XML (ETLController)
DECLARE @Header AS XML (ETLController)
DECLARE @BatchHeader AS XML (ETLController)
DECLARE @StepHeader AS XML (ETLController)
DECLARE @ReceiptHeader AS XML (ETLController)
DECLARE @ThreadCount TINYINT
declare @ServiceStatusID tinyint

raiserror ('
 Use ETLMonitor.exe to monitor workflow progress. All informational messages are directed to Log only.
 Error output is still multi-casted to both log and console.
 ',0,1) with nowait;  


BEGIN TRY --Main block
BEGIN TRY --validations

--dummy header
EXEC @ExitCode = dbo.prc_CreateHeader @Header out,0,0,0,0,@bOptions

SET @StatusID = NULL
IF (@pDebug = 1)
BEGIN
   SET @msg =  'BEGIN Procedure ' + @ProcName + ' with @pBatchName=' + isnull(@pBatchName,'@pContext')
   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
   exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
END


-----------------------------------------------------------------------------------
-- validate input parameters
-----------------------------------------------------------------------------------
if (@pBatchName is null)
begin
   if (@pContext is null)
   BEGIN
      SET @Err = 50101
      SET @StatusID = @STAT_ERROR
      SET @RaiserrMsg = '   ERROR @pContext is expected'
      RAISERROR(@RaiserrMsg,11,11) 
   END

  ;with xmlnamespaces('ETLController.XSD' as ETL)
  select @BatchID = @pContext.value('(/ETL:Context/@BatchID)[1]','int')
        ,@pBatchName = @pContext.value('(/ETL:Context/@BatchName)[1]','nvarchar(30)')

   if (@BatchID is null or @pBatchName is null)
   BEGIN
      SET @Err = 50101
      SET @StatusID = @STAT_ERROR
      SET @RaiserrMsg = '   ERROR @pContext: attribute BatchID and BatchName are required'
      RAISERROR(@RaiserrMsg,11,11) 
   END

   IF (@pDebug = 1)
   BEGIN
      SET @msg =  ' Context @pBatchName=' + @pBatchName
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@Header,@msg
      exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
   END


   if exists (select 1 from dbo.ETLBatch where BatchName = @pBatchName and BatchID <> @BatchID)
   BEGIN
      SET @Err = 50101
      SET @StatusID = @STAT_ERROR
      SET @RaiserrMsg = '   ERROR pContext: BatchName=' + @pBatchName + ' already exists with different BatchID'
      RAISERROR(@RaiserrMsg,11,11) 
   END

   set @Options = @Options + ',replace'
   EXEC @ExitCode = dbo.prc_PersistContext @pContext,@pHandle,@Options
end
else
begin 

   SELECT @BatchID = BatchID
     FROM dbo.ETLBatch  WHERE BatchName = @pBatchName

   IF (@BatchID IS NULL)
   BEGIN
      SET @Err = 50101
      SET @StatusID = @STAT_ERROR
      SET @RaiserrMsg = '   ERROR invalid input parameter @pBatchName=' + @pBatchName
      RAISERROR(@RaiserrMsg,11,11)
   END
end

--dont have runid yet
EXEC @ExitCode = dbo.prc_CreateHeader @BatchHeader out,@BatchID,null,null,0,@bOptions,1--batch

IF (@pDebug = 1)
BEGIN
   SET @msg = '   Batch found @BatchID=' + CAST(@BatchID as nvarchar(30))
   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
   exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
END

EXEC @ExitCode = dbo.prc_CreateContext @Context out,@BatchHeader

IF (@pDebug = 1)
BEGIN
   SET @msg = '   Batch Context=' + CAST(@Context as nvarchar(max))
   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
   exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
END

;with xmlnamespaces('ETLController.XSD' as ETL)
select
       @OnBatchSuccess = 'exec @ExitCode = ' + cb.b.value('(./ETL:OnSuccess/ETL:Process)[1]','nvarchar(max)') + ' @pRequest=@Request,@pReceipt=@Receipt out' 
                       + isnull(',' + cb.b.value('(./ETL:OnSuccess/ETL:Param)[1]','nvarchar(max)'),'')
      ,@OnBatchFailure = 'exec @ExitCode = ' + cb.b.value('(./ETL:OnFailure/ETL:Process)[1]','nvarchar(max)') + ' @pRequest=@Request,@pReceipt=@Receipt out' 
                       + isnull(',' + cb.b.value('(./ETL:OnFailure/ETL:Param)[1]','nvarchar(max)'),'')
      ,@BatchIgnore = NULLIF(cb.b.value('(./@IgnoreErr)[1]','tinyint'),0)
      ,@Restart = NULLIF(cb.b.value('(./@Restart)[1]','tinyint'),0)
      ,@ATT_MAXTHREAD = NULLIF(cb.b.value('(./@MaxThread)[1]','tinyint'),0)
      ,@ATT_TIMEOUT = NULLIF(cb.b.value('(./@Timeout)[1]','int'),0)
      ,@ATT_LIFETIME = NULLIF(cb.b.value('(./@Lifetime)[1]','int'),0)
      ,@ATT_PING = NULLIF(cb.b.value('(./@Ping)[1]','int'),0)
      ,@ATT_HISTRET = NULLIF(cb.b.value('(./@HistRet)[1]','int'),0)
  from @Context.nodes('/ETL:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)

-------------------------------------------------------------------
--Set defaults
-------------------------------------------------------------------
SET @ATT_PING_MAX = 120 --sec
SET @ATT_MAXTHREAD = CASE WHEN ISNULL(@ATT_MAXTHREAD,0) <= 0 THEN 1 ELSE @ATT_MAXTHREAD END; --num
SET @ATT_LIFETIME = CASE WHEN ISNULL(@ATT_LIFETIME,0) <= 0 THEN 7200 ELSE @ATT_LIFETIME END; --sec
SET @ATT_TIMEOUT = CASE WHEN ISNULL(@ATT_TIMEOUT,0) <= 0 THEN @ATT_LIFETIME ELSE @ATT_TIMEOUT END; --sec
--Ping must be less then 1 min
--it is used to reset the broker thread Cancel job timer
--which is hardcoded to 60 sec. if ping is not received within 1 min
--all worker threads will be terminated for that conversation
-- this is done to allow to kill the prc_execute process
SET @ATT_PING = case when ISNULL(@ATT_PING,0) < 1 then @ATT_PING_MAX
                     when @ATT_PING > @ATT_PING_MAX then @ATT_PING_MAX
                     else @ATT_PING
                 end
SET @ATT_HISTRET = CASE WHEN ISNULL(@ATT_HISTRET,0) <= 0 THEN 100 ELSE @ATT_HISTRET END; --days

-------------------------------------------------------------------
--Check if this batch is running
-------------------------------------------------------------------
SELECT @LastStatusID = StatusID
      ,@LastRunID    = RunID
  FROM dbo.ETLBatchRun
 WHERE RunID = (SELECT MAX(RunID) FROM dbo.ETLStepRun
                 WHERE BatchID = @BatchID)

IF (@LastRunID IS NOT NULL)
BEGIN
    IF (@pForceStart = 0)
    BEGIN
       -- wait <TIMEOUT> <PING>
       SET @Wait = right('00' + cast((@ATT_PING)/3600 as varchar(10)),3)
                + ':' + right('0' + cast(((@ATT_PING)/60)%60 as varchar(10)),2)
                + ':' + right('0' + cast((@ATT_PING)%60 as varchar(10)),2)

       SET @checktimeout = 0
       WHILE (@pWait = 1 AND @checktimeout < @ATT_TIMEOUT)
       BEGIN
          IF (@pDebug = 1)
          BEGIN
             SET @msg = '   Waiting (' + @Wait + ')on previous run to finish RunID=' + cast(@LastRunID as varchar(10)) 
             exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
             exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
          END
          WAITFOR DELAY @wait
          SET @LastStatusID = NULL
          SELECT @LastStatusID = StatusID
                ,@LastRunID    = RunID
            FROM dbo.ETLBatchRun
           WHERE RunID = (SELECT MAX(RunID) FROM dbo.ETLStepRun
                          WHERE BatchID = @BatchID)

           IF (ISNULL(@LastStatusID,@STAT_AVAILABLE) <> @STAT_STARTED)
              BREAK

          SET @checktimeout = @checktimeout + @ATT_PING
       END
       IF (ISNULL(@LastStatusID,@STAT_AVAILABLE) = @STAT_STARTED)
       BEGIN
          SET @Err = 50103
          SET @StatusID = @STAT_FAILURE_IMMEDIATE
          SET @RaiserrMsg = '   ERROR Batch=' + @pBatchName
                + ' is already running with RunID=' + cast(@LastRunID as varchar(10))
                + ', process can not continue '
          exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@RaiserrMsg,@Err
          exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
          RAISERROR(@RaiserrMsg,11,11)
       END
     END
     ELSE
     BEGIN
       SET @msg = '   WARNING Batch=' + @pBatchName
             + ' is force started from RunID=' + cast(@LastRunID as varchar(10))
       exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
       exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
       exec @ExitCode = dbo.prc_Finalize @BatchHeader,@pHandle,@STAT_FAILURE;
    END
END

SELECT @LastStatusID = StatusID
      ,@LastRunID    = RunID
 FROM dbo.ETLBatchRun
WHERE RunID = (SELECT MAX(RunID) FROM dbo.ETLBatchRun
                WHERE BatchID = @BatchID)

end try --validations
begin catch
  set @msg = error_message();
  raiserror('Validation Block error=%d,status=%d: %s',11,11,@Err,@StatusID,@msg)
end catch

begin try -- workflow body block
begin try -- workflow constraints check block

SET @checktimeout = 0
-------------------------------------------------------------------
--Generate new runid
--Prepare Processing Step records
-------------------------------------------------------------------
INSERT dbo.ETLBatchRun
(BatchID,StatusDT,StatusID,Err,StartTime,EndTime)
SELECT @BatchID,getdate(),1,0,getdate(),cast(null as datetime)
SELECT @RunID = SCOPE_IDENTITY()


-------------------------------------------------------------------
--Process the Batch Constraints
-------------------------------------------------------------------
EXEC @ExitCode = dbo.prc_CreateHeader @Header out,@BatchID,null,null,@RunID,@bOptions,5 --b and bc
exec @ExitCode = dbo.prc_CreateContext @Context out,@Header
exec @ExitCode = dbo.prc_CreateProcessRequest @ProcessRequest out,@Header,@Context,@pHandle

--IF (@pDebug = 1)
--BEGIN
--   SET @msg = '   Batch Constraint Context=' + CAST(@Context as nvarchar(max))
--   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
--   exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
--END

SET @ProcessReceipt = NULL
EXEC @ExitCode = dbo.prc_ConstraintCheck
                  @pRequest = @ProcessRequest
                 ,@pReceipt = @ProcessReceipt output

EXEC @ExitCode = dbo.prc_ReadProcessReceipt @ProcessReceipt,null,@StatusID out,@Err out,@msg out
SET @StatusID = ISNULL(@StatusID,@STAT_ERROR)

IF (@StatusID = @STAT_FAILURE)
BEGIN
   SET @RaiserrMsg = '   ERROR Batch Constraints were not met'
END
ELSE IF (@StatusID = @STAT_ERROR or @Err <> 0)
BEGIN
   SET @RaiserrMsg = '   ERROR Batch Constraints check failed'
END

IF (@pDebug = 1)
BEGIN
   SET @msg = '   Batch Constraint Check returns StatusID=' + CAST(@StatusID as nvarchar(30))
   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
   exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
END

IF (@RaiserrMsg is not null)
BEGIN
   RAISERROR(@RaiserrMsg,11,11)
END

end try --workflow constraints check block
begin catch
  set @msg = ERROR_MESSAGE()
  raiserror('Workflow Constraints Check Block error=%d,status=%d: %s',11,11,@Err,@StatusID,@msg)
end catch

begin try --workflow steps block

set @BatchStatusID = @STAT_SUCCESS;
-------------------------------------------------------------------
--Retrieve destination service list
--and start conversation to all available services
-------------------------------------------------------------------
declare @service table (SvcID int identity(1,1),[SvcName] sysname,StatusID smallint null
                       ,Handle uniqueidentifier null,grpHandle uniqueidentifier null,StatusDate datetime null)
declare @svcid int
declare @svcStatusID smallint
declare @grpHandle uniqueidentifier

insert @service (SvcName,StatusID)
select [name],@STAT_STARTED
  from sys.services where [name] = @LocalService;

if (@pSlaveOff = 0)
   insert @service (SvcName,StatusID)
   select [remote_service_name],@STAT_STARTED
     from sys.routes where [remote_service_name] like @LocalService + '%'
     and [remote_service_name] <> @LocalService;

set @svcid = 0
SET @grpHandle = newid()
while (1=1)
begin
   set @svcName = null
   select @svcname = SvcName
         ,@svcid = SvcID
     from @service where SvcId = (select min(SvcID) from  @service where SvcID > @SvcID)
   if @svcName is null
      break


--Start conversation
   SET @Handle = null
   BEGIN DIALOG CONVERSATION @handle
   FROM SERVICE  [ETLController_Request]
   TO SERVICE @svcName
--select family_id from master.sys.databases where database_id = db_id()
--                           ,'E995CF15-A383-4FBB-8FE3-1D6C129F190C'
   ON CONTRACT  [ETLController]
   WITH LIFETIME = @ATT_LIFETIME
   ,RELATED_CONVERSATION_GROUP = @grpHandle
   ,ENCRYPTION = OFF

   if (@handle is null)
      continue

   IF (@pDebug = 1)
   BEGIN
      SET @msg = '   Start Conversation to service:' + @svcName
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
      exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
   END

   update @service
      set Handle = @handle
         ,grpHandle = @GrpHandle
         ,StatusID = @STAT_STARTED
         ,StatusDate = GETDATE()
    where SvcID = @SvcID

   --overload StepID with SvcID in test msg
   exec @ExitCode = dbo.prc_CreateHeader @Header out,@BatchID,@SvcID,null,@RunID,@bOptions,0
   exec @ExitCode = dbo.prc_CreateProcessRequest @ProcessRequest out,@Header,null,null;
   
   SEND ON CONVERSATION @handle 
   MESSAGE TYPE [ETLController_Test]
   (CAST(@ProcessRequest AS VARBINARY(MAX)));

   BEGIN CONVERSATION TIMER (@handle) TIMEOUT = @ATT_PING ;

end

if not exists(select 1 from @service where handle is not null)
begin
  SET @RaiserrMsg = '   ERROR no available services found'
  RAISERROR(@RaiserrMsg,11,11)
end

-------------------------------------------------------------------
--Prepare the step Queue table
-------------------------------------------------------------------

EXEC @ExitCode = dbo.prc_CreateHeader @BatchHeader out,@BatchID,null,null,@RunID,@bOptions,1--batch

INSERT dbo.ETLStepRun
(RunID,BatchID,StepID
,StatusDT,StatusID
,SPID,StepOrder,IgnoreErr
,Err,StartTime,EndTime,SeqGroup,PriGroup,SvcName)

SELECT @RunID,s.BatchID,s.StepID
      ,getdate(),@STAT_AVAILABLE
      ,null,s.StepOrder,ISNULL(NULLIF(@BatchIgnore,0),NULLIF(s.IgnoreErr,0))
      ,null,null,null,sg.AttributeValue,isnull(pg.AttributeValue,'zzz')
      ,case when coalesce(sn.AttributeValue,bsn.AttributeValue,'LOCAL') IN ('LOCAL','DEFAULT') then @LocalService
            else isnull(sn.AttributeValue,bsn.AttributeValue)
       end
  FROM dbo.ETLStep s
  LEFT JOIN dbo.ETLStepAttribute a ON s.BatchID = a.BatchID AND s.StepID = a.StepID
   AND a.AttributeName = 'DISABLED' AND a.AttributeValue = '1'
  LEFT JOIN dbo.ETLStepAttribute sg ON s.BatchID = sg.BatchID AND s.StepID = sg.StepID
   AND sg.AttributeName = 'SEQGROUP'
  LEFT JOIN dbo.ETLStepAttribute pg ON s.BatchID = pg.BatchID AND s.StepID = pg.StepID
   AND pg.AttributeName = 'PRIGROUP'
  LEFT JOIN dbo.ETLStepAttribute rs ON s.BatchID = rs.BatchID AND s.StepID = rs.StepID
   AND rs.AttributeName = 'RESTART'
  LEFT JOIN dbo.ETLStepAttribute sn ON s.BatchID = sn.BatchID AND s.StepID = sn.StepID
   AND sn.AttributeName = 'SVCNAME'
  LEFT JOIN dbo.ETLBatchAttribute bsn ON s.BatchID = bsn.BatchID
   AND bsn.AttributeName = 'SVCNAME'  
 WHERE (s.BatchID = @BatchID and a.StepID IS NULL  --enebled steps only
   AND (ISNULL(@LastStatusID,@STAT_AVAILABLE) = @STAT_SUCCESS --succeeded batches 
    OR ((ISNULL(@Restart,0) <> 0) or (isnull(cast(rs.AttributeValue as tinyint),0) <> 0)) --always restartable steps
    OR (ISNULL(s.StatusID,@STAT_AVAILABLE) <> @STAT_SUCCESS) --never executed or failed steps
       ));

    SELECT @Cnt = @@ROWCOUNT
    IF (@Cnt = 0)
    BEGIN
       --this code also will work when last run failed on batch success process 
       --SET @StatusID = @STAT_SUCCESS
       SET @StatusID = @STAT_WARNING
       SET @msg = '   WARNING no steps found for the batch'
       exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
       exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
       --SET @RaiserrMsg = '   WARNING no steps found for the batch'
       --RAISERROR(@RaiserrMsg,11,11)
    END

    IF (@pDebug = 1)
    BEGIN
       SET @msg = '   Selected ' + CAST(@Cnt as nvarchar(30)) + ' steps into dbo.ETLStepRun with RunID=' + cast(@RunID as varchar(10)) 
       exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
       exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
    END
  

-------------------------------------------------------------------
--LOOP support
-------------------------------------------------------------------
declare @LoopGroup table (StepID int,GroupCode nvarchar(100),StatusID int, primary key (StepID,GroupCode))

insert @LoopGroup (StepID,GroupCode,StatusID)
select s.StepID, sa.AttributeValue,@STAT_STARTED
  from dbo.ETLStepRun s
  join dbo.ETLStepAttribute sa on sa.BatchID = s.BatchID and sa.StepID = s.StepID
   and sa.AttributeName = 'LOOPGROUP'
 where s.BatchID = @BatchID and s.RunId = @RunID


-------------------------------------------------------------------
--Loop through the Batch Steps
-------------------------------------------------------------------
SET @ThreadCount = 0
SET @Handle = null
Send_Message:
   IF(@ThreadCount >= @ATT_MAXTHREAD
   --wait for all services to responde
   --or exists (select 1 from @service where StatusID <> @STAT_SUCCESS)
   --wait for at least 1 service to respond
   or not exists (select 1 from @service where StatusID = @STAT_SUCCESS)
     )
      GOTO Receive_Message


-------------------------------------------------------------------
--Lock next available step for execution
-------------------------------------------------------------------
   SET @StepID = NULL
   SET @StatusID = NULL

    UPDATE  sr
    SET @StepID = sr1.StepID,sr.SPID = @@SPID,sr.StatusID = @STAT_STARTED,sr.StartTime = getdate()
	FROM dbo.ETLStepRun sr
	JOIN (SELECT TOP (1) sr.BatchID,sr.StepID
    FROM  dbo.ETLStepRun sr WHERE sr.RunID = @RunID AND sr.BatchID = @BatchID AND sr.StatusID = @STAT_AVAILABLE
	--requested step service should be running
    AND NOT EXISTS (SELECT 1 FROM @service svc WHERE (svc.SvcName = sr.SvcName AND svc.StatusID = @STAT_STARTED))
	--Seqgroup and PriGroup requirements should be met
    AND NOT EXISTS (SELECT 1 FROM dbo.ETLStepRun sr1 WHERE sr.BatchID = sr1.BatchID AND sr.RunID = sr1.RunID
    AND ((sr.SeqGroup = sr1.SeqGroup AND sr.StepOrder > sr1.StepOrder)
        OR sr.PriGroup > sr1.priGroup) AND sr1.StatusID IN (@STAT_STARTED,@STAT_AVAILABLE))
	--no errors unless IgnoreErr is specified
	AND NOT EXISTS (SELECT 1 FROM dbo.ETLStepRun sr2 WHERE sr.BatchID = sr2.BatchID AND sr.RunID = sr2.RunID
	AND (sr2.StatusID IN (@STAT_ERROR,@STAT_FAILURE) AND (@BatchIgnore IS NULL AND sr2.IgnoreErr IS NULL)))
		ORDER BY sr.PriGroup,sr.StepOrder,sr.SeqGroup
		) sr1
	ON sr.BatchID = sr1.BatchID AND sr.StepID = sr1.StepID;
 
-------------------------------------------------------------------
-------------------------------------------------------------------
   -- exit with no error indicates that all steps are taken 
   IF (@StepID IS NULL)
   BEGIN
      IF (@pDebug = 1)
      BEGIN
         SET @msg = '   No more available steps found'
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      END
      SET @StatusID = @BatchStatusID
      IF (@ThreadCount > 0)
         GOTO Receive_Message
      ELSE
      BEGIN
         GOTO Exit_Loop
      END
   END

   exec @ExitCode = dbo.prc_CreateHeader @StepHeader out,@BatchID,@StepID,null,@RunID,@bOptions,11 --b,s and sc
 
   IF (@pDebug = 1)
   BEGIN
      SET @msg = '   Processing Step ' + CAST(@StepID as nvarchar(30))
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@StepHeader,@msg
      exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
   END
  
-------------------------------------------------------------------
--Skip step on Exit Event
-------------------------------------------------------------------
   set @StatusID = cast(dbo.fn_ETLCounterGet (@BatchID,@StepID,@RunID,'ExitEvent') as tinyint)
   IF (@StatusID is not null)
   BEGIN
      -- Update Step Status
      UPDATE s
         SET s.StatusID = @StatusID,s.EndTime = getdate(),s.Err = @Err
        FROM dbo.ETLStepRun s
       WHERE s.RunID = @RunID AND s.StepID = @StepID AND s.BatchID = @BatchID


      IF (@pDebug = 1)
      BEGIN
         SET @msg = '   Step ' + CAST(@StepID as nvarchar(30)) + ' skipped on ExitEvent with StatusID=' + CAST(@StatusID as nvarchar(30))
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@StepHeader,@msg
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      END
     
      GOTO Send_Message
   END
 
   exec @ExitCode = dbo.prc_CreateContext @Context out,@StepHeader
   exec @ExitCode = dbo.prc_CreateProcessRequest @ProcessRequest out,@StepHeader,@Context

--   IF (@pDebug = 1)
--   BEGIN
--      SET @msg = '   Step Context=' + CAST(@Context as nvarchar(max))
--      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@StepHeader,@msg
--      exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
--   END

  set @Handle = null;
  select @SvcName = SvcName from dbo.ETLStepRun
   where BatchID = @BatchID and StepID = @StepID and RunID = @RunID;
   
  if (@SvcName = 'ANY')
  begin
-- less used
  select top(1) @Handle = h.Handle
               ,@SvcName = h.SvcName
    from @service h
    left join (
      select s.SvcName,count(*) as cnt
        from dbo.ETLStepRun s
       where s.BatchID = @BatchID and s.RunID = @RunID
         and s.SvcName is not null and s.StatusID = @STAT_STARTED
       group by s.SvcName) s on h.SvcName = s.SvcName
      where h.StatusID = @STAT_SUCCESS
      order by isnull(s.cnt,0);     
  end
  else if not exists (select 1 from @service where SvcName = @SvcName)
  begin
  --use local service for unknown Services
     set @SvcName = @LocalService;
     select top(1) @Handle = h.Handle
                  ,@SvcName = h.SvcName
       from @service h
      where h.SvcName = @SvcName
        and h.StatusID = @STAT_SUCCESS;
  end
  else
  begin
  --use service from metadata
     select top(1) @Handle = h.Handle
                  ,@SvcName = h.SvcName
       from @service h
      where h.SvcName = @SvcName
        and h.StatusID = @STAT_SUCCESS;
  end
   
   IF (@Handle IS NULL)
   BEGIN
      SET @Err = 50110;
      SET @StatusID = @STAT_ERROR;
      SET @RaiserrMsg = '   ERROR Service ' + @SvcName + ' is not available';
      RAISERROR(@RaiserrMsg,11,11);
   END

  update dbo.ETLSteprun
     set SvcName = @SvcName
   where BatchID = @BatchID and StepID = @StepID and RunID = @RunID
     and SvcName <> @SvcName;

   IF (@pDebug = 1)
   BEGIN
      SET @msg = '   Step ' + CAST(@StepID as nvarchar(30)) + ' was sent to service ' + @SvcName
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@StepHeader,@msg
      exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
   END
   
                      
   ;SEND ON CONVERSATION @handle 
   MESSAGE TYPE [ETLController_Request]
   (CAST(@ProcessRequest AS VARBINARY(MAX)))
   
--to test without broker
--insert #tmp(StepID,StatusID)
--values (@StepID,2)

   SET @ThreadCount = @ThreadCount + 1
   GOTO Send_Message

Receive_Message:
   WAITFOR(
   RECEIVE top(1)
       @handle = conversation_handle,
       @message_type=message_type_name, 
       @message = message_body 
    FROM [ETLController_Receipt_Queue]
    WHERE conversation_group_id = @grpHandle
       )--, TIMEOUT @ATT_PING * 1000

   SELECT @Cnt = @@ROWCOUNT
   IF (@Cnt = 0)
   BEGIN
      SET @StatusID = @STAT_ERROR
      SET @Err = 50111
      SET @RaiserrMsg = '   ERROR conversation ' + CAST(@handle as nvarchar(36)) + ' received no message'
      raiserror (@RaiserrMsg,11,11)
   END
   
-------------------------------------------------------------------
--Exit Batch on Exit Event
-------------------------------------------------------------------
   set @StatusID = cast(dbo.fn_ETLCounterGet (@BatchID,0,@RunID,'ExitEvent') as tinyint)
   IF (@StatusID is not null)
   BEGIN
      IF (@pDebug = 1)
      BEGIN
         SET @msg = '   Batch Exit on ExitEvent with StatusID=' + CAST(@StatusID as nvarchar(30))
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      END
     
      GOTO Exit_Loop
   END

   
/*
--to test without broker
waitfor delay '000:00:05'
set @message_type = 'BATCHPROCESS_Receipt'
select top 1 @xml_Receipt = --NCHAR(0xFEFF) +
         N'<cait:ProcessReceipt xmlns:cait="BATCHPROCESS_Receipt.XSD">'
      + N'<cait:BatchID>' + CAST(@BatchID as nvarchar(30)) + N'</cait:BatchID>'
      + N'<cait:StepID>' + CAST(StepID as nvarchar(30)) + N'</cait:StepID>'
      + N'<cait:StatusID>' + CAST(StatusID as nvarchar(30)) + N'</cait:StatusID>'
      + N'<cait:Error>0</cait:Error>'
      + N'<cait:RunID>' + CAST(@RunID as nvarchar(30)) + N'</cait:RunID>'
      + N'</cait:ProcessReceipt>'
,@StepID = StepID
 from #tmp
set @cnt = 1
set @message = cast(@xml_Receipt as varbinary(max))
delete #tmp where StepId = @stepID
*/

   IF (@message_type = 'http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer')
   BEGIN

	  IF (@pDebug = 1)
	  BEGIN
	     SET @msg = '   DialogTimer message is received'
	     exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
	     exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
	  END
	  --StartDate = workflow instance start date
	  SET @checktimeout = DATEDIFF(SECOND,@StartDate,GETDATE())
	  IF (@checktimeout + @ATT_PING >= @ATT_LIFETIME) -- check dialog lifetime
      BEGIN
         SET @StatusID = @STAT_ERROR
         SET @Err = 50111
         SET @RaiserrMsg = '   ERROR Dialog Lifetime is exceeded'
         raiserror(@RaiserrMsg,11,11)
      END
	  ELSE IF (@checktimeout >= @ATT_TIMEOUT) -- check timeout
      BEGIN
         SET @StatusID = @STAT_ERROR
         SET @Err = 50112
         SET @RaiserrMsg = '   ERROR Workflow timeout is exceeded'
         raiserror(@RaiserrMsg,11,11)
      END
      ELSE
	  BEGIN
         
         select top(1) @svcid = h.SvcID
                      ,@SvcName = h.SvcName
                      ,@StatusDate = h.StatusDate
                      ,@ServiceStatusID = h.StatusID
           from @service h
          where h.Handle = @Handle
          
          --StatusDate = Last TEST message delivery
          --if test message didnt make thru until next Timer Ping print the warning
	      SET @checktimeout = DATEDIFF(SECOND,@StatusDate,GETDATE())	      
          if (@checktimeout > @ATT_PING)
          begin
            SET @msg = '   WARNING Workflow did not receive the Ping responce from service ' + @SvcName + ' in time'
	        exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
	        exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
	      end
            
          --overload StepID with SvcID in test msg
          exec @ExitCode = dbo.prc_CreateHeader @Header out,@BatchID,@SvcID,null,@RunID,@bOptions,0
          exec @ExitCode = dbo.prc_CreateProcessRequest @ProcessRequest out,@Header,null,null;
   
          --send another test message to check conversation status
          if (@ServiceStatusID = @STAT_SUCCESS)
          begin

             ;SEND ON CONVERSATION @handle 
             MESSAGE TYPE [ETLController_Test]
            (CAST(@ProcessRequest AS VARBINARY(MAX)));

             BEGIN CONVERSATION TIMER (@handle) TIMEOUT = @ATT_PING ;
          end
          else
          --never received the original response from Remote service: disable it
          begin
	         IF (@pDebug = 1)
	         BEGIN
	            SET @msg = '   WARNING: Service ' + @SvcName + ' will be disabled';
	            exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg;
	            exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle;
	         END
             ;END CONVERSATION @handle;
             
             delete @service where SvcID = @svcid;            
             if not exists(select 1 from @service)
             begin
                SET @StatusID = @STAT_ERROR
                SET @Err = 50113
                SET @RaiserrMsg = '   ERROR All Services failed to start'
                raiserror(@RaiserrMsg,11,11)
             end
             goto Send_Message                                  
          end
          GOTO Receive_Message
      END

   END
   --step processing message 
   ELSE IF (@message_type = 'ETLController_Receipt')
   BEGIN
      SET @ProcessReceipt = @message
      EXEC @ExitCode = dbo.prc_ReadProcessReceipt @ProcessReceipt,@ReceiptHeader out,@StatusID out,@Err out,@msg out
      EXEC @ExitCode = dbo.prc_ReadHeader @ReceiptHeader,null,@StepID out

-------------------------------------------------------------------
--Finish the step
-------------------------------------------------------------------
      -- Update Step Status
      UPDATE s
         SET s.StatusID = @StatusID,s.EndTime = getdate(),s.Err = @Err
        FROM dbo.ETLStepRun s
       WHERE s.RunID = @RunID AND s.StepID = @StepID AND s.BatchID = @BatchID


      IF (@pDebug = 1)
      BEGIN
         SET @msg = '   Step ' + CAST(@StepID as nvarchar(30)) + ' completed with StatusID=' + CAST(@StatusID as nvarchar(30))
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@ReceiptHeader,@msg
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      END

-------------------------------------------------------------------
--LOOP support. Loop steps reset.
--Exit loop on receiving BreakEvent with the LoopGroup value
-------------------------------------------------------------------

      update lg
         set lg.StatusID = @STAT_SUCCESS
        from @LoopGroup lg
        join (select distinct rc.CounterValue as GroupCode
            from dbo.ETLStepRunCounter rc
           where rc.BatchID = @BatchID and rc.RunID = @RunID
             and rc.CounterName = 'BreakEvent') c
          on lg.GroupCode = c.GroupCode
        where lg.StatusID = @STAT_STARTED;
    
--update all the steps in a LoopGroup to available if loop is completed without BreakEvent
      update sr
         set StatusID = @STAT_AVAILABLE
            ,StatusDT = getdate()
            ,Err = 0
            ,EndTime = null
        from dbo.ETLStepRun sr
        join @LoopGroup r on sr.StepID = r.StepID and r.StatusID = @STAT_STARTED
         and r.GroupCode in (select r1.GroupCode
                           from @LoopGroup r1
                           join dbo.ETLStepRun sr1
                             on sr1.StepID = r1.StepID and sr1.BatchID = @BatchID and r1.StatusID = @STAT_STARTED
                        group by r1.GroupCode having COUNT(*) = SUM(case sr1.StatusID when 2 then 1 else 0 end))
       where sr.BatchID = @BatchID and sr.RunID = @RunID;

--update all not started steps in a LoopGroup to finished if loop receives a BreakEvent
      update sr
         set StatusID = @STAT_SUCCESS
            ,StatusDT = getdate()
            ,Err = 0
            ,EndTime = getdate()
        from dbo.ETLStepRun sr
        join @LoopGroup r on sr.StepID = r.StepID and r.StatusID = @STAT_SUCCESS
       where sr.BatchID = @BatchID and sr.StatusID = @STAT_AVAILABLE and sr.RunID = @RunID;

-------------------------------------------------------------------
--Check Batch Status
--keep batch running if IgnorErr in on but batch should still fail at the end
-------------------------------------------------------------------
      IF @BatchStatusID NOT IN (@STAT_ERROR,@STAT_FAILURE,@STAT_FAILURE_IMMEDIATE)
	  BEGIN
	     SET @BatchStatusID = @StatusID
      END
      SET @StatusID = NULL
      SET @StatusOrder = NULL

      -- Check Batch Status
      SELECT @StatusOrder = MIN(CASE
                WHEN StatusID IN (@STAT_FAILURE,@STAT_ERROR) AND IgnoreErr is not null THEN 10
                WHEN StatusID IN (@STAT_FAILURE,@STAT_ERROR) THEN 2
                ELSE 10 END)
        FROM dbo.ETLStepRun WHERE RunID = @RunID AND BatchID = @BatchID

      SELECT @StatusOrder = MIN(CASE
                WHEN sr.StatusID = @STAT_AVAILABLE THEN ISNULL(NULLIF(@StatusOrder,10),1)
                WHEN sr.StatusID = @STAT_SUCCESS and lg.StatusID = @STAT_STARTED THEN ISNULL(NULLIF(@StatusOrder,10),1)
                WHEN sr.StatusID = @STAT_STARTED THEN 1
                WHEN sr.StatusID = @STAT_SUCCESS THEN 4
                WHEN sr.StatusID IN (@STAT_FAILURE,@STAT_ERROR) AND IgnoreErr is not null THEN 4
                WHEN sr.StatusID = @STAT_FAILURE THEN 3
                WHEN sr.StatusID = @STAT_ERROR THEN 2
                WHEN sr.StatusID = @STAT_WARNING THEN 10
                WHEN sr.StatusID = @STAT_FAILURE_IMMEDIATE THEN 10
                ELSE 11 END)
        FROM dbo.ETLStepRun sr
        left join @LoopGroup lg on sr.StepID = lg.StepID 
       WHERE sr.RunID =  @RunID AND sr.BatchID = @BatchID

      SET @StatusID = CASE @StatusOrder
                WHEN 10 THEN @STAT_AVAILABLE
                WHEN 1 THEN @STAT_STARTED
                WHEN 4 THEN @STAT_SUCCESS
                WHEN 3 THEN @STAT_FAILURE
                WHEN 2 THEN @STAT_ERROR
                ELSE NULL END
-------------------------------------------------------------------
-------------------------------------------------------------------

      IF (@StatusID IS NULL)
      BEGIN
         SET @RaiserrMsg = '   ERROR failed to check the Status'
         SET @StatusID = @STAT_ERROR
         SET @Err = 50105
         raiserror(@RaiserrMsg,11,11)
      END

      IF (@pDebug = 1)
      BEGIN
         SET @msg = '   Batch Run StatusID=' + CAST(@StatusID as nvarchar(30))
         SET @msg = @msg + '; Overall Batch StatusID=' + CAST(@BatchStatusID as nvarchar(30))
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      END


      IF (@StatusID <> @STAT_STARTED)
      BEGIN
         SET @StatusID = @BatchStatusID
         GOTO Exit_Loop
      END

      SET @ThreadCount = @ThreadCount - 1
       GOTO Send_Message
    END--Receipt
    ELSE IF (@message_type = 'ETLController_InfoMessage')
    BEGIN
      set @ProcessInfo = @message
      exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      GOTO Receive_Message
    END--InfoMessage
    ELSE IF (@message_type = 'ETLController_Test')
    BEGIN
      set @ProcessReceipt = @message
      EXEC @ExitCode = dbo.prc_ReadProcessReceipt @ProcessReceipt,@ReceiptHeader out,@ServiceStatusID out,@Err out,@msg out
      EXEC @ExitCode = dbo.prc_ReadHeader @ReceiptHeader,null,@SvcID out
      set @ServiceStatusID = isnull(@ServiceStatusID,@STAT_ERROR)
      

      if (@pDebug = 1 or @ServiceStatusID <> @STAT_SUCCESS)
      begin
         select @SvcName = SvcName from @service where SvcID = @SvcID
         SET @msg = '   Service test ' + @SvcName + ' returns StatusID=' + CAST(@ServiceStatusID as nvarchar(30))
                  + ' with message: ' + @msg;
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      end

      update @service
         set StatusID = @ServiceStatusID
            ,StatusDate = GETDATE()
       where SvcID = @SvcID
       
      GOTO Send_Message
    END--TESTMESSAGE
    ELSE 
    BEGIN 
      --GOTO Receive_Message
      SET @StatusID = @STAT_ERROR
      SET @Err = 50110
      RAISERROR ('   ERROR Message received:[%s] %s',11,11,@message_type,@message)
    END--UNKNOWN

-- Conversation Completed
Exit_Loop:

IF(@StatusID <> @STAT_SUCCESS)
BEGIN
   SET @RaiserrMsg = '   ERROR Step processing block return StatusID=' + cast(@StatusID as nvarchar(10))
   RAISERROR(@RaiserrMsg,11,11)
END
END TRY --workflow steps block
BEGIN CATCH

IF (@Trancount < @@TRANCOUNT)
  ROLLBACK TRAN

SET @Err = case @Err when 0 then ERROR_NUMBER() else @Err end;
IF (@RaiserrMsg IS NULL)
BEGIN
   set @StatusID = @STAT_ERROR
   set @RaiserrMsg = ERROR_MESSAGE()
END

exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@RaiserrMsg,@Err
exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
SET @RaiserrMsg = null

--if previous batch failed on success or failure with all steps succeeded
--we need to try finishing it again
IF (@StatusID = @STAT_WARNING and @LastStatusID <> @STAT_SUCCESS)
BEGIN
   SET @msg = '   WARNING Trying to finish last successfull run again'
   set @StatusID = @STAT_SUCCESS
END
ELSE IF @StatusID = @STAT_ERROR
BEGIN
   SET @msg = '   ERROR Process failed with Status 4(ERROR)'
   SET @RaiserrMsg = @msg
END
ELSE IF @StatusID = @STAT_FAILURE
BEGIN
   SET @msg = '   ERROR Process failed with Status 3(FAILURE)'
   SET @RaiserrMsg = @msg
END
ELSE IF @StatusID = @STAT_WARNING
BEGIN
   SET @msg = '   WARNING Process exited with Status 5(WARNING)'
END
ELSE IF @StatusID = @STAT_FAILURE_IMMEDIATE
BEGIN
   SET @msg = '   ERROR Process exited with Status 6(FAILURE WITH EXIT_IMMEDIATE)'
   SET @RaiserrMsg = @msg
END

IF (@msg IS NOT NULL)
BEGIN
   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg,@Err
   exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
END

END CATCH

-- End All conversations
WHILE (1 = 1)
BEGIN
   set @handle = null;
   SELECT top(1) @handle = Handle
                ,@grpHandle = grpHandle
                ,@SvcID = SvcID
                ,@svcname = SvcName
                ,@ServiceStatusID = StatusID
     from @Service where Handle is not null

   if (@Handle is null)
      break

   --send Cancel request message to the other threads to Cancel any outstanding executions
   --overload StepID with SvcID in test msg
   exec @ExitCode = dbo.prc_CreateHeader @Header out,@BatchID,@SvcID,null,@RunID,@bOptions,0
   exec @ExitCode = dbo.prc_CreateProcessRequest @ProcessRequest out,@Header,null,null;
 
   if (@ServiceStatusID = @STAT_SUCCESS)
   begin  
       begin try
  --for some reason EndDialog or Error message is not getting picked up from the Queue
   --right away. Using Cancel request instead
      ;SEND ON CONVERSATION @handle 
       MESSAGE TYPE [ETLController_Cancel]
       (CAST(@ProcessRequest AS VARBINARY(MAX)));

         IF (@pDebug = 1)
         BEGIN
            SET @msg = '   Cancel request was sent to service = ' + @SvcName
            exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
            exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
         END
         
         --wait for EndDialog message
         while (1 = 1)
         begin 
           set @checktimeout = @ATT_PING * 1000;
           WAITFOR(
           RECEIVE top(1)
             @handle = conversation_handle,
             @message_type=message_type_name, 
             @message = message_body 
           FROM [ETLController_Receipt_Queue]
           WHERE conversation_handle = @Handle
           ), TIMEOUT @checktimeout;
          
           --do not wait longer that ping interval
           set @Cnt = @@ROWCOUNT
           if (@Cnt = 0)
             break;
          
           if (@message_type in ('http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog'
                               ,'http://schemas.microsoft.com/SQL/ServiceBroker/Error'))
              break;     
         end
         end try
         begin catch
             SET @msg = '   ERROR failed to SEND Cancel request to service = ' + @SvcName
             exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
             exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
         end catch
    end
    --try to end conversation     
    begin try
       END CONVERSATION @handle;
    end try
    begin catch
       IF (@pDebug = 1)
       BEGIN
          SET @msg = ERROR_MESSAGE();
          exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
          exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
       END    
    end catch
    delete @Service where SvcID = @SvcID
    SET @handle = null
END

end try --workflow body block
begin catch
   set @StatusId = case isnull(@StatusID,@STAT_ERROR) when @STAT_SUCCESS then @STAT_ERROR else isnull(@StatusID,@STAT_ERROR) end;
   set @Err = case @Err when 0 then ERROR_NUMBER() else @Err end;
end catch

-------------------------------------------------------------------
--Finish the batch
-------------------------------------------------------------------
SET @StepID = 0
IF (@RunID IS NOT NULL AND @StatusID <> @STAT_FAILURE_IMMEDIATE)
BEGIN
   --OnBatchSuccess\OnBatchFailure
   SET @BatchStatusID = @StatusID
   SET @BatchErr = @Err
   SET @nSQL1 = NULL
   IF (@StatusID = @STAT_SUCCESS)
   BEGIN
      SET @nSQL1 = @OnBatchSuccess
      IF (@pDebug = 1)
      BEGIN
         SET @msg = '   OnBatchSuccess: ' + ISNULL(@OnBatchSuccess,'NULL')
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      END
   END
   ELSE IF (@StatusID IN (@STAT_FAILURE,@STAT_ERROR))
   BEGIN
      SET @nSQL1 = @OnBatchFailure
      IF (@pDebug = 1)
      BEGIN
         SET @msg = '   OnBatchFailure: ' + ISNULL(@OnBatchFailure,'NULL')
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      END
   END

   IF (@nSQL1 IS NOT NULL)
   BEGIN
      BEGIN TRY
      exec @ExitCode = dbo.prc_CreateHeader @Header out,@BatchID,null,null,@RunID,@bOptions,1 --b
      exec @ExitCode = dbo.prc_CreateContext @Context out,@Header
      exec @ExitCode = dbo.prc_CreateProcessRequest @ProcessRequest out,@Header,@Context,@phandle
      SET @ProcessReceipt = NULL
      EXEC sp_ExecuteSQL @nSql1
                  ,N'@ExitCode int output,@Request xml,@Receipt xml output'
                  ,@ExitCode = @ExitCode output
                  ,@Request = @ProcessRequest
                  ,@Receipt = @ProcessReceipt output
      
      EXEC @ExitCode = dbo.prc_ReadProcessReceipt @ProcessReceipt,null,@StatusID out,@Err out,@msg out

      SET @StatusID = isnull(@StatusID,@STAT_ERROR)
      IF (@Err <> 0 OR @StatusID <> @STAT_SUCCESS)
      BEGIN
         SET @RaiserrMsg = '   ERROR failed to execute OnSuccess/OnFailure code: return StatusID=' + cast(@StatusID as nvarchar(10))
                         + ' with msg: ' +  isnull(@msg,'null')
         RAISERROR(@RaiserrMsg,11,11)
      END
      
      END TRY
      BEGIN CATCH
         SET @Err = isnull(nullif(@Err,0),ERROR_NUMBER())
         SET @RaiserrMsg = '   ERROR OnSuccess/OnFailure:' + ERROR_MESSAGE()
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@RaiserrMsg,@Err
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      END CATCH

      IF (@pDebug = 1)
      BEGIN
         SET @msg = '   Completed with StatusID=' + CAST(@StatusID as nvarchar(30))
         exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg,@Err
         exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
      END

   END
   IF(@StatusID = @STAT_SUCCESS)
   BEGIN
      SET @StatusID = @BatchStatusID
      SET @Err = @BatchErr
   END
END

IF (@RunID IS NOT NULL)
BEGIN
   exec @ExitCode = dbo.prc_Finalize @BatchHeader,@pHandle,@StatusID;

   --processing history clean up
   SELECT @CleanupRunID = max(RunID)
     FROM dbo.ETLBatchRun
    WHERE BatchID = @BatchID and StatusDT <= dateadd(dd,-@ATT_HISTRET,getdate())

   IF (@pDebug = 1)
   BEGIN
      SET @msg = '   Retention for BatchID=' + CAST(@BatchID as nvarchar(30)) + ' is set to ' + CAST(@ATT_HISTRET as nvarchar(30)) + ' days.'
               + ' All history prior to RunID=' + ISNULL(CAST(@CleanupRunID as nvarchar(30)),'null') + ' will be deleted'
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
      exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
   END

   IF (@CleanupRunID IS NOT NULL)
   BEGIN
      DELETE dbo.ETLStepRunHistoryLog
        FROM dbo.ETLStepRunHistoryLog h
        JOIN dbo.ETLBatchRun b on h.RunID = b.RunID AND b.BatchID = @BatchID
       WHERE  h.RunID <= @CleanupRunID

      DELETE dbo.ETLStepRunHistory WHERE RunID <= @CleanupRunID and BatchID = @BatchID
      DELETE dbo.ETLStepRunCounter WHERE RunID <= @CleanupRunID and BatchID = @BatchID
      --DELETE dbo.ETLStepRun WHERE RunID <= @CleanupRunID and BatchID = @BatchID
      DELETE dbo.ETLBatchRun WHERE RunID <= @CleanupRunID and BatchID = @BatchID
   END

   IF (@pDebug = 1)
   BEGIN
      SET @msg = '   Completed BatchID=' + CAST(@BatchID as nvarchar(30)) + ' with StatusID=' + CAST(@StatusID as nvarchar(30))
      exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg,@Err
      exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
   END

   if (@RaiserrMsg is not null)
      RAISERROR(@RaiserrMsg,11,11)

END

END TRY --Main block
BEGIN CATCH
   SET @Err = case @Err when 0 then ERROR_NUMBER() else @Err end
   SET @RaiserrMsg = ERROR_MESSAGE()
END CATCH

IF (@StatusID IN (@STAT_SUCCESS,@STAT_WARNING)) SET @Err = 0
IF (@pDebug = 1)
BEGIN
   SET @msg = 'END Procedure ' + @ProcName
   exec @ExitCode = dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg
   exec @ExitCode = dbo.prc_Print @ProcessInfo,@pHandle
END

IF (@RaiserrMsg is not null)
   RAISERROR(@RaiserrMsg,11,11)

RETURN @Err;