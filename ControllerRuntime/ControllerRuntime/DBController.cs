/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     02/20/2015
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
*******************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Collections;
using System.Data;
using System.Globalization;

using System.Data.SqlTypes;

namespace ControllerRuntime
{

    /// <summary>
    /// WfStatus to the backend execution status converter. 
    /// </summary>
    public class DBStatus
    {
        public static short WfValue(WfStatus status)
        {
            short value = 0;
            switch (status)
            {
                case WfStatus.Unknown:
                    value = 0;
                    break;
                case WfStatus.Running:
                case WfStatus.Suspended:
                    value = 1;
                    break;
                case WfStatus.Succeeded:
                    value = 2;
                    break;
                case WfStatus.Failed:
                    value = 4;
                    break;
                default:
                    value = 0;
                    break;
            }

            return value;
        }
        public static WfStatus DbValue(short status)
        {
            WfStatus value = WfStatus.Unknown;
            switch (status)
            {
                case 0:
                    value = WfStatus.Unknown;
                    break;
                case 1:
                    value = WfStatus.Running;
                    break;
                case 2:
                    value = WfStatus.Succeeded;
                    break;
                case 3:
                case 4:
                    value = WfStatus.Failed;
                    break;
                default:
                    value = WfStatus.Unknown;
                    break;
            }

            return value;
        }
    }
    /// <summary>
    /// backend communication manager
    /// </summary>
    public class DBController
    {

        private const string WORKFLOW_METADATA_QUERY = @"

declare @batchName varchar(100) = '{0}';
declare @wfid int;
declare @pHeader xml;
declare @pContext xml;
select top 1 @wfid = batchid from ETLBatch where BatchName = @batchName;
if (@wfid is null)
    raiserror ('Workflow %s is not found',11,11,@batchName);

exec dbo.prc_CreateHeader @pHeader out,@wfid,null,null,0,{1},15;
exec dbo.prc_CreateContext @pContext out,@pHeader;
select cast(@pContext as nvarchar(max));";

        //successfull steps from the previous unsuccessful run
        private const string WORKFLOW_STATUS_QUERY = @"
declare @batchName varchar(100) = '{0}';
declare @BatchId int;
declare @LastStatusID tinyint;
select top 1 @BatchId = BatchId from dbo.ETLBatch where BatchName = @batchName;
set @LastStatusID = isnull((select top 1 StatusID from dbo.ETLBatchRun
where RunID = (select MAX(RunID) from dbo.ETLBatchRun where BatchID = @BatchID)),2);

select s.StepId,s.StatusId
from dbo.ETLStep s
where s.BatchId = @BatchId
and (@LastStatusID <> 2
and isnull(s.StatusID,0) = 2);
";
        private const string WORKFLOW_INITIALIZE_QUERY = @"
declare @processorName varchar(100) = '{0}';
declare @batchName varchar(100) = '{1}';
declare @options int = {2} + 2 * {3};
declare @forcestart bit = {3};

declare @BatchId int;
declare @LastStatusID tinyint;
declare @LastRunID int;
declare @RunId int;
declare @BatchHeader xml;
begin try
	select top 1 @BatchId = BatchId from dbo.ETLBatch where BatchName = @batchName;

--cleanup part	
    SELECT @LastStatusID = StatusID
		  ,@LastRunID    = RunID
	  FROM dbo.ETLBatchRun
	 WHERE RunID = (SELECT MAX(RunID) FROM dbo.ETLStepRun
					 WHERE BatchID = @BatchID)

    --abort if running and forcestart is not set
    if(@forcestart = 0 and @LastStatusID = 1)
        raiserror ('Workflow %s is already running, Use forestart option to cleanup',11,11,@batchName);

	if (@LastRunId is not null)
	begin
		exec dbo.prc_CreateHeader @BatchHeader out,@BatchID,null,null,@LastRunID,@options,1--batch
		exec dbo.prc_Finalize @BatchHeader,null,4;
	end

--initialization part
    SELECT @LastStatusID = StatusID
		  ,@LastRunID    = RunID
	  FROM dbo.ETLBatchRun
	 WHERE RunID = (SELECT MAX(RunID) FROM dbo.ETLBatchRun
					 WHERE BatchID = @BatchID)


	INSERT dbo.ETLBatchRun
	(BatchID,StatusDT,StatusID,Err,StartTime,EndTime)
	SELECT @BatchID,getdate(),1,0,getdate(),cast(null as datetime)
	SELECT @RunID = SCOPE_IDENTITY()

	INSERT dbo.ETLStepRun
	(RunID,BatchID,StepID
	,StatusDT,StatusID
	,SPID,StepOrder,IgnoreErr
	,Err,StartTime,EndTime,SeqGroup,PriGroup,SvcName)

SELECT @RunID,s.BatchID,s.StepID
      ,getdate(),0
      ,null,s.StepOrder,ISNULL(NULLIF(b.IgnoreErr,0),NULLIF(s.IgnoreErr,0))
      ,null,null,null
      ,(SELECT TOP 1 sg.AttributeValue FROM dbo.ETLStepAttribute sg WHERE s.BatchID = sg.BatchID AND s.StepID = sg.StepID AND sg.AttributeName IN ('SEQGROUP','etl:SeqGroup))
      ,isnull((SELECT TOP 1 pg.AttributeValue FROM dbo.ETLStepAttribute pg WHERE s.BatchID = pg.BatchID AND s.StepID = pg.StepID AND pg.AttributeName IN ('PRIGROUP','etl:PriGroup)),'zzz')
      ,@processorName
  FROM dbo.ETLStep s
  JOIN dbo.ETLBatch b ON s.BatchID = b.BatchID
  LEFT JOIN dbo.ETLStepAttribute rs ON s.BatchID = rs.BatchID AND s.StepID = rs.StepID
   AND rs.AttributeName = 'RESTART'
 WHERE (s.BatchID = @BatchID
   and not exists (SELECT 1 dbo.ETLStepAttribute a WHERE s.BatchID = a.BatchID AND s.StepID = a.StepID AND a.AttributeName IN ('DISABLED','etl:Disabled') AND a.AttributeValue = '1')  --enebled steps only
   AND (ISNULL(@LastStatusID,0) = 2 --succeeded batches
   --always restartable steps
    OR ((ISNULL(b.RestartOnErr,0) = 1)
    or (exists (SELECT 1 dbo.ETLStepAttribute a WHERE rs ON s.BatchID = rs.BatchID AND s.StepID = rs.StepID AND rs.AttributeName IN ('RESTART','etl:Restart') AND rs.AttributeValue = '1'))
    OR (ISNULL(s.StatusID,0) <> 2) --never executed or failed steps
       ));

	if (@@ROWCOUNT = 0)
	begin
		exec dbo.prc_CreateHeader @BatchHeader out,@BatchID,null,null,@RunID,@options,1--batch
		exec dbo.prc_Finalize @BatchHeader,null,4;
	end

    select @runId,0
    union all
    select @runId,stepId
	  from dbo.ETLStepRun
	  where BatchId = @BatchId and runId = @runId;
end try
begin catch
	declare @msg nvarchar(1000);
	declare @ProcessInfo xml;
	SET @msg = 'Failed to initialize new run for the Workflow ' + @batchName + ': ' + error_message();
	exec dbo.prc_CreateProcessInfo @ProcessInfo out,@BatchHeader,@msg;
	exec dbo.prc_Print @ProcessInfo;
	raiserror (@msg,11,11);
end catch	  
";
        private const string WORKFLOW_FINALIZE_QUERY = @"
declare @BatchId int = {0};
declare @RunId int = {1};
declare @StatusId smallint = {2};
declare @HistRet int = {3};
declare @options int = {4};

declare @BatchHeader xml;
declare @CleanupRunID int;

exec dbo.prc_CreateHeader @BatchHeader out,@BatchID,null,null,@RunId,@options,1--batch
exec dbo.prc_Finalize @BatchHeader,null,@StatusID;

--processing history clean up
SELECT @CleanupRunID = max(RunID)
	FROM dbo.ETLBatchRun
WHERE BatchID = @BatchID and StatusDT <= dateadd(dd,-@HistRet,getdate());

IF (@CleanupRunID IS NOT NULL)
BEGIN
	DELETE dbo.ETLStepRunHistoryLog
	FROM dbo.ETLStepRunHistoryLog h
	JOIN dbo.ETLBatchRun b on h.RunID = b.RunID AND b.BatchID = @BatchID
	WHERE  h.RunID <= @CleanupRunID;

	DELETE dbo.ETLStepRunHistory WHERE RunID <= @CleanupRunID and BatchID = @BatchID;
	DELETE dbo.ETLStepRunCounter WHERE RunID <= @CleanupRunID and BatchID = @BatchID;
	--DELETE dbo.ETLStepRun WHERE RunID <= @CleanupRunID and BatchID = @BatchID;
	DELETE dbo.ETLBatchRun WHERE RunID <= @CleanupRunID and BatchID = @BatchID;
END
";

        private const string WORKFLOW_ATTRIBUTE_QUERY = @"
declare @pHeader xml;
declare @pContext xml;
declare @pProcessRequest xml;
declare @pAttributes xml;
exec dbo.prc_CreateHeader @pHeader out,{0},{1},{2},{3},{4},15
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext
exec dbo.prc_ReadContextAttributes @pProcessRequest,@pAttributes out
select cast(@pAttributes as nvarchar(max));
";

        private const string WORKFLOW_STEP_STATUS_SET_QUERY = @"
declare @BatchId int = {0};
declare @StepId int = {1};
declare @RunId int = {2}
declare @StatusId smallint = {3};
declare @Err int = {4};
UPDATE s
    SET s.StatusID = @StatusId
,s.StartTime = case when @StatusId = 1 then getdate() else s.StartTime end
,s.EndTime = case when @StatusId in (0,1) then null else getdate() end
,s.Err = @Err
FROM dbo.ETLStepRun s
WHERE s.RunID = @RunID AND s.StepID = @StepID AND s.BatchID = @BatchID
";
        private const string WORKFLOW_LOOP_STEPS_RESET_QUERY = @"
declare @BatchId int = {0};
declare @RunId int = {1}
declare @LoopGroup nvarchar(30) = '{2}'

UPDATE s
    SET s.StatusID = 0,s.EndTime = null,s.Err = 0
FROM dbo.ETLStepRun s
LEFT JOIN dbo.ETLStepRunCounter rc ON s.BatchId = rc.BatchId and s.RunId = rc.RunId
AND (s.StepId = rc.StepId OR rc.StepId = 0) and rc.CounterName = 'ExitEvent'
WHERE s.RunID = @RunID AND s.BatchID = @BatchID and s.StatusId in (2,3,4) and rc.BatchId is null
AND EXISTS (SELECT 1 FROM dbo.ETLStepAttribute sa WHERE s.BatchId = sa.BatchId and s.StepId = sa.StepId
    and sa.AttributeName IN ('LOOPGROUP','etl:LoopGroup') and sa.AttributeValue = @LoopGroup)

;

if (@@ROWCOUNT > 0)
BEGIN
    declare @value int = 0;
    set @LoopGroup = 'Loop_' + @LoopGroup;
    set @value = 1 + isnull(dbo.fn_ETLCounterGet (@BatchId,0,@RunId,@LoopGroup),0);
    exec dbo.prc_ETLCounterSet @BatchId,0,@RunId,@LoopGroup,@value;
END";

        private const string WORKFLOW_LOOP_BREAK_QUERY = @"
declare @BatchId int = {0};
declare @RunId int = {1}
declare @LoopGroup nvarchar(30) = '{2}'
declare @CounterValue nvarchar(30);
declare @CounterName nvarchar(30) = 'BreakEvent_' + @LoopGroup;

--check for loop break event
set @CounterValue = dbo.fn_ETLCounterGet (@BatchId,0,@RunId,@counterName);

if (@CounterValue is not null)
BEGIN
    UPDATE s
        SET s.StatusID = 2,s.EndTime = getdate() ,s.Err = 0
    FROM dbo.ETLStepRun s
    WHERE s.RunID = @RunID AND s.BatchID = @BatchID and s.StatusId = 0
    AND EXISTS (SELECT 1 FROM dbo.ETLStepAttribute sa WHERE s.BatchId = sa.BatchId and s.StepId = sa.StepId
        and sa.AttributeName IN ('LOOPGROUP','etl:LoopGroup') and sa.AttributeValue = @LoopGroup)
;
    SELECT cast(2 as smallint) as StatusId;
END
ELSE
    SELECT cast(1 as smallint) as StatusId; 
";
        private const string WORKFLOW_EXIT_EVENT_QUERY = @"
declare @BatchId int = {0};
declare @StepId int = {1}
declare @RunId int = {2}
declare @StatusId smallint;

--check for exit event
set @StatusId = isnull(dbo.fn_ETLCounterGet (@BatchId,@stepId,@RunId,'ExitEvent'),0);
IF (@StatusId not in (0,1))
BEGIN
    UPDATE s
        SET s.StatusID = @StatusId,s.EndTime = getdate() ,s.Err = 0
    FROM dbo.ETLStepRun s
    WHERE s.RunID = @RunID AND s.BatchID = @BatchID and s.StatusId = 0
    AND (@StepId = 0 OR s.StepId = @StepId)  ;
    SELECT @StatusId as StatusId;
END
ELSE
    SELECT cast(1 as smallint) as StatusId;
";



        private string _connection_string;
        private bool _debug = false;
        private bool _verbose = false;

        protected DBController(string connectionString, bool debug, bool verbose)
        {
            _connection_string = connectionString;
            _debug = debug;
            _verbose = verbose;
        }

        public static DBController Create(string connectionString, bool debug, bool verbose)
        {
            return new DBController(connectionString, debug, verbose);
        }

        public static DBController Create(string connectionString)
        {
            return new DBController(connectionString, false, false);
        }

        /// <summary>
        /// Reads the xml Workflow definition from DB 
        /// </summary>
        /// <param name="WorkflowName"></param>
        /// <returns></returns>
        public Workflow WorkflowMetadataGet(string WorkflowName)
        {

            Workflow wf = new Workflow();
            string xml_string;
            using (SqlConnection cn = new SqlConnection())
            {
                cn.ConnectionString = _connection_string;
                try
                {
                    cn.Open();
                    string cmd_text = String.Format(WORKFLOW_METADATA_QUERY,
                        WorkflowName,
                        ((_debug) ? 1 : 0));
                    using (SqlCommand cmd = new SqlCommand(cmd_text, cn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.CommandType = CommandType.Text;
                        var xml = cmd.ExecuteScalar();
                        //xml_string = "<?xml version=\"1.0\"?>" + xml.ToString();
                        xml_string = xml.ToString();
                    }

                    wf = Workflow.DeSerializefromXml(xml_string);
                }
                catch (Exception ex)
                {
                    throw (ex);
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();
                }
            }

            return wf;
        }
        /// <summary>
        /// Initialize new Workflow Instance
        /// forcestart finctionality is not supported
        /// make sure another instance of the same workflow is not running
        /// before starting new instance 
        /// </summary>
        /// <param name="Processor"></param>
        /// <param name="wf"></param>
        /// <returns></returns>
        public bool WorkflowInitialize(string Processor, Workflow wf, bool forcestart)
        {
            if (wf == null || String.IsNullOrEmpty(wf.WorkflowName))
            {
                throw new ArgumentException("Workflow object can not be Initialized");
            }

            using (SqlConnection cn = new SqlConnection())
            {
                cn.ConnectionString = _connection_string;
                try
                {
                    cn.Open();
                    string cmd_text = String.Format(WORKFLOW_INITIALIZE_QUERY,
                        Processor,
                        wf.WorkflowName,
                        ((_debug) ? 1 : 0),
                        ((forcestart) ? 1 : 0));
                    using (SqlCommand cmd = new SqlCommand(cmd_text, cn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.CommandType = CommandType.Text;
                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            int runid = reader.GetInt32(0);
                            int stepid = reader.GetInt32(1);

                            wf.RunId = runid;
                            if (wf.WorkflowSteps != null && stepid > 0)
                            {
                                foreach (WorkflowStep step in wf.WorkflowSteps)
                                {
                                    if (step.StepId == stepid)
                                    {
                                        step.RunId = runid;
                                        step.WorkflowId = wf.WorkflowId;
                                        step.IsSetToRun = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    throw (ex);
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();
                }
            }

            return true;
        }
        /// <summary>
        /// Finilizes the Workflow completion with backend.
        /// Perform Workflow History Cleanup based on retention policy.
        /// </summary>
        /// <param name="wf"></param>
        /// <param name="Result"></param>
        /// <returns></returns>
        public bool WorkflowFinalize(Workflow wf, WfResult Result)
        {
            if (wf == null || wf.RunId == 0)
            {
                throw new InvalidDataException("Workflow object can not be finalized");
            }


            short status_id = DBStatus.WfValue(Result.StatusCode);
            string cmd_text = String.Format(WORKFLOW_FINALIZE_QUERY,
                wf.WorkflowId,
                wf.RunId,
                status_id,
                wf.HistoryRetention,
                ((_debug) ? 1 : 0));
            ExecuteNonQuery(cmd_text);

            return true;
        }
        /// <summary>
        /// Reports execution results to the backend
        /// </summary>
        /// <param name="wfs"></param>
        /// <param name="Result"></param>
        /// <returns></returns>
        public bool WorkflowStepStatusSet(WorkflowStep wfs, WfResult Result)
        {
            if (wfs == null || wfs.RunId == 0)
            {
                throw new InvalidDataException("WorkflowStep object is not in the correct state");
            }


            short status_id = DBStatus.WfValue(Result.StatusCode);
            string cmd_text = String.Format(WORKFLOW_STEP_STATUS_SET_QUERY,
                wfs.WorkflowId,
                wfs.StepId,
                wfs.RunId,
                status_id,
                Result.ErrorCode);
            ExecuteNonQuery(cmd_text);

            return true;
        }

        /// <summary>
        /// Reset all LoopGroup Step statuses to NotStarted to reinitinialize the steps for the next cycle
        /// </summary>
        /// <param name="WfId"></param>
        /// <param name="RunId"></param>
        /// <param name="LoopGroup"></param>
        public void WorkflowLoopStepsReset(int WfId, int RunId, string LoopGroup)
        {

            string cmd_text = String.Format(WORKFLOW_LOOP_STEPS_RESET_QUERY,
                WfId,
                RunId,
                LoopGroup);
            ExecuteNonQuery(cmd_text);
        }

        /// <summary>
        /// Finilize all Loop Steps on Loop Break Event 
        /// </summary>
        /// <param name="WfId"></param>
        /// <param name="RunId"></param>
        /// <param name="LoopGroup"></param>
        public WfStatus WorkflowLoopBreak(int WfId, int RunId, string LoopGroup)
        {

            string cmd_text = String.Format(WORKFLOW_LOOP_BREAK_QUERY,
                WfId,
                RunId,
                LoopGroup);
            var ret = ExecuteScalar(cmd_text);
            if (ret == null)
                return WfStatus.Failed;

            return DBStatus.DbValue((short)ret);
        }

        /// <summary>
        /// Cancel the Batch(StepId = 0)/Step on the ExitEvent 
        /// </summary>
        /// <param name="WfId"></param>
        /// <param name="StepId"></param>
        /// <param name="RunId"></param>
        /// <param name="LoopGroup"></param>
        public WfResult WorkflowExitEventCheck(int WfId, int StepId, int RunId)
        {

            string cmd_text = String.Format(WORKFLOW_EXIT_EVENT_QUERY,
                WfId,
                StepId,
                RunId);
            var ret = ExecuteScalar(cmd_text);
            if (ret == null)
                return WfResult.Unknown;

            WfResult result;
            switch (DBStatus.DbValue((short)ret))
            {
                case WfStatus.Unknown:
                case WfStatus.Running:
                    result = WfResult.Started;
                    break;
                case WfStatus.Failed:
                    result = WfResult.Create(WfStatus.Failed, "Canceled", 0);
                    break;
                case WfStatus.Succeeded:
                    result = WfResult.Create(WfStatus.Succeeded, "Canceled", 0);
                    break;
                default:
                    result = WfResult.Started;
                    break;
            }

            return result;
        }


        /// <summary>
        /// Returns resolved Attribute Collection set based on the request scope. 
        /// </summary>
        /// <param name="WfId"></param>
        /// <param name="StepId"></param>
        /// <param name="ConstId"></param>
        /// <param name="RunId"></param>
        /// <returns></returns>
        public WorkflowAttribute[] WorkflowAttributeCollectionGet(int WfId, int StepId, int ConstId, int RunId)
        {

            WorkflowAttributeCollection attributes = new WorkflowAttributeCollection();
            string xml_string;
            using (SqlConnection cn = new SqlConnection())
            {
                cn.ConnectionString = _connection_string;
                try
                {
                    cn.Open();

                    string cmd_text = String.Format(WORKFLOW_ATTRIBUTE_QUERY
                        , WfId
                        , (StepId == 0) ? "null" : StepId.ToString()
                        , (ConstId == 0) ? "null" : ConstId.ToString()
                        , RunId,
                        ((_debug) ? 1 : 0));
                    using (SqlCommand cmd = new SqlCommand(cmd_text, cn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.CommandType = CommandType.Text;
                        var xml = cmd.ExecuteScalar();
                        xml_string = xml.ToString();
                    }

                    attributes = WorkflowAttributeCollection.DeSerializefromXml(xml_string);
                }
                catch (Exception ex)
                {
                    throw (ex);
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();
                }

            }

            return attributes.Attributes;
        }

        /// <summary>
        /// Provide unified Logger Interface to all modules
        /// </summary>
        /// <param name="WfId"></param>
        /// <param name="StepId"></param>
        /// <param name="ConstId"></param>
        /// <param name="RunId"></param>
        /// <returns></returns>
        public IWorkflowLogger GetLogger(int WfId, int StepId, int ConstId, int RunId)
        {
            return new WorkflowControllerLogger(WfId, StepId, ConstId, RunId, _connection_string, _debug, _verbose);
        }

        private void ExecuteNonQuery(string cmd_text)
        {
            using (SqlConnection cn = new SqlConnection())
            {
                cn.ConnectionString = _connection_string;
                try
                {
                    cn.Open();

                    using (SqlCommand cmd = new SqlCommand(cmd_text, cn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw (ex);
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();
                }

            }

        }

        private object ExecuteScalar(string cmd_text)
        {
            using (SqlConnection cn = new SqlConnection())
            {
                cn.ConnectionString = _connection_string;
                try
                {
                    cn.Open();

                    using (SqlCommand cmd = new SqlCommand(cmd_text, cn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.CommandType = CommandType.Text;
                        return cmd.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    throw (ex);
                }
                finally
                {
                    if (cn.State != ConnectionState.Closed)
                        cn.Close();
                }

            }

        }

    }
}
