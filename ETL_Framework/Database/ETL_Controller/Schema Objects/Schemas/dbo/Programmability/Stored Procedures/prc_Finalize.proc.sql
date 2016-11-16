CREATE procedure [dbo].[prc_Finalize] (
    @pHeader xml(ETLController)
   ,@pHandle uniqueidentifier = null
   ,@pStatusID smallint
   
) as
begin
/******************************************************************************
** File:	[prc_Finalize].sql
** Name:	[dbo].[prc_Finalize]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Procedure/[prc_Finalize].sql:

** Desc:	clean up the execution tables
**          
**
** Params:
** Returns:
**
** Author:	andreys
** Date:	10/15/2011
** ****************************************************************************
** CHANGE HISTORY
** ****************************************************************************
** Date				Author	version	4	#bug			Description
** ----------------------------------------------------------------------------------------------------------

*/

set nocount on
declare @err                int
declare @proc               sysname
declare @msg                nvarchar(1000)
declare @debug              tinyint
declare @Rows               int

declare @ProcErr int
declare @ProcName sysname
declare @BatchID int
declare @RunID int
declare @Options int
declare @ProcessInfo xml (ETLController)

set @ProcName = object_name(@@PROCID);

set @err = 0;
begin try

exec dbo.prc_ReadHeader @pHeader,@BatchID out,null,null,@RunID out,@Options out;
set @debug = @Options & 1;

--Finalize All stuck RunIDs
--set @RunID = null;

declare @run table (RunID int primary key);
insert @run
select distinct RunID from dbo.ETLStepRun
 where BatchID = @BatchID AND (RunID = @RunID OR isnull(@RunID,0) = 0)
 union select RunID from dbo.ETLBatchRun
 where BatchID = @BatchID AND (RunID = @RunID OR (isnull(@RunID,0) = 0 and StatusID = 1));

select @RunID = MAX(RunID) from @run;

UPDATE s
  SET s.StatusID = CASE WHEN t.StatusID = 1 THEN @pStatusID ELSE t.StatusID END,s.StatusDT = t.StatusDT,s.Err = t.Err
 FROM dbo.ETLStep s
 JOIN  dbo.ETLStepRun t ON s.StepID = t.StepID AND s.BatchID = t.BatchID
 JOIN @run r on t.RunID = r.RunID
WHERE s.BatchID = @BatchID;

UPDATE b
 SET b.EndTime = getdate()
    ,b.StatusID = @pStatusID
    ,b.StatusDT = getdate()
    ,b.Err = case when @pStatusID = 2 then 0 else 50103 end
FROM dbo.ETLBatchRun b
JOIN @run r ON b.RunID = r.RunID
WHERE b.BatchID = @BatchID;

DELETE dbo.ETLStepRun
OUTPUT deleted.RunID,deleted.BatchID,deleted.StepID,deleted.StatusDT
 ,CASE WHEN deleted.StatusID = 1 THEN @pStatusID ELSE deleted.StatusID END
 ,deleted.SPID,deleted.StepOrder,deleted.IgnoreErr
 ,deleted.Err,deleted.StartTime,deleted.EndTime,deleted.SeqGroup,deleted.PriGroup,deleted.SvcName
 INTO dbo.ETLStepRunHistory
(RunID,BatchID,StepID,StatusDT,StatusID,SPID,StepOrder,IgnoreErr
,Err,StartTime,EndTime,SeqGroup,PriGroup,SvcName)
FROM dbo.ETLStepRun s
JOIN @run r ON s.RunID = r.RunID
WHERE s.BatchID = @BatchID;

set @Rows = @@ROWCOUNT;
IF (@Debug = 1)
BEGIN
  SET @msg = '   Moved ' + CAST(@Rows as nvarchar(30)) + ' rows in ETLStepRunHistory for BatchID=' + CAST(@BatchID as nvarchar(30));
  exec dbo.prc_CreateProcessInfo @ProcessInfo out,@pHeader,@msg;
  exec dbo.prc_Print @ProcessInfo,@pHandle;
END

UPDATE b
 SET b.EndTime = r.EndTime
    ,b.StatusDT = r.StatusDT
    ,b.StatusID = r.StatusID
   ,b.Err = r.Err
FROM dbo.ETLBatch b
JOIN dbo.ETLBatchRun r ON b.BatchID = r.BatchID AND r.RunID = @RunID 
WHERE b.BatchID = @BatchID;


end try
begin catch
   set @Proc = ERROR_PROCEDURE()
   set @Msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   raiserror ('ERROR: PROC %s, MSG: %s',11,11,@Proc,@Msg) 
end catch

return @err
end;
