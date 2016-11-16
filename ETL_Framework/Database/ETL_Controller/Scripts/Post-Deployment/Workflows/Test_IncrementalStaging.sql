set nocount on
If not exists (select name from sys.databases where name = N'Dest_DB')
	create database [Dest_DB]
go

use Dest_DB
go
If  exists (select * FROM sys.objects where object_id = OBJECT_ID(N'dbo.Audit') AND type in (N'U'))
	drop table dbo.Audit;
CREATE TABLE [dbo].[Audit](
	 [AuditId] [int] IDENTITY(1,1) NOT NULL,
	 [AuditObject] [sysname] NULL,
	 [SourceObject] [nvarchar](1000) NULL,
	 [StartDT] [datetime] NULL,
	 [EndDT] [datetime] NULL,
	 [Op] [nvarchar](10) NULL,
	 [Err] [int] NULL,
	 [RowCnt] [int] NULL,
	 [RunId] [int] NULL,
	 CONSTRAINT [AuditPK] PRIMARY KEY CLUSTERED ([AuditId] ASC));
go

create PROCEDURE [dbo].[prc_Audit]
      @AuditId INT = NULL OUT
    , @AuditMode TINYINT = 0
    , @AuditObject SYSNAME = NULL
    , @SourceObject NVARCHAR(1000) = NULL
    , @Op NVARCHAR(10) = NULL
    , @Err INT = NULL
    , @RowCnt INT = NULL
    , @RunId INT = NULL
    , @Options NVARCHAR(100) = NULL
AS

-- Reduce processing overhead.
SET NOCOUNT ON;

-- Standard variable declarations.
DECLARE
      @InitialTransactionCount INT
    , @RowCount INT
    , @IsDebug BIT
    , @ReturnCode INT
    , @Message NVARCHAR(1000)
    , @ProcedureName SYSNAME
    ;
    
-- Set variable defaults.
SELECT
      @ProcedureName = OBJECT_NAME(@@PROCID)
    , @InitialTransactionCount = @@TRANCOUNT
    , @IsDebug = CASE WHEN CHARINDEX('debug',@Options) > 0 THEN 1 ELSE 0 END
    , @ReturnCode = 0 -- Default return code.
    ;

BEGIN TRY

-- STEP 1: Check for begin audit mode.
IF @AuditMode = 1  -- Begin mode.

   BEGIN -- NEW AUDIT
   
        -- STEP 2: Validate mode.
        IF @AuditId IS NOT NULL
            RAISERROR('Warning: AuditId cant be reused in BEGIN Audit mode . Key=%d', 1, 0, @AuditId) WITH NOWAIT;
   
        -- STEP 3: Insert beginning of audit.
        INSERT dbo.Audit (
                AuditObject
              , SourceObject
              , StartDT
              , Op
              , Err
              , RowCnt
              , RunId
              )
            VALUES (
                  @AuditObject
                , @SourceObject
                , GETDATE() -- Start date time.
                , @Op
                , ISNULL(@Err,0) -- Error code.
                , @RowCnt
                , @RunId
                )
                ;
                
        -- STEP 3: Retrieve auto-generated audit key value.
        SELECT @AuditId = SCOPE_IDENTITY();
      
        -- STEP 4: For debug mode, report audit is created.
        IF @IsDebug = 1
            RAISERROR('Audit record %d created.', 0, 1, @AuditId) WITH NOWAIT;
         
   END; -- NEW AUDIT
   
ELSE

    BEGIN -- EXISTING AUDIT
    
        -- STEP 5: Validate audit exists.
        IF NOT EXISTS (SELECT 1 FROM dbo.Audit WHERE AuditId = @AuditId)
            RAISERROR('Specified audit record %d does not exist.', 11, 11, @AuditId) WITH NOWAIT;
            
        -- STEP 6: Check for update audit mode.
        IF @AuditMode = 0 -- Update mode.

            BEGIN -- UPDATE AUDIT MODE
         
                -- STEP 7: Validate audit is not already closed.
                IF EXISTS (SELECT 1 FROM dbo.Audit WHERE AuditId = @AuditId AND EndDT IS NOT NULL)
                    RAISERROR('Cannot change audit record %d because it is already closed.', 11, 11, @AuditId) WITH NOWAIT;
            
                -- STEP 8: Update audit.
                UPDATE A SET
                      A.AuditObject = ISNULL(@AuditObject, A.AuditObject)
                    , A.SourceObject = ISNULL(@SourceObject, A.SourceObject)
                    , A.Op = ISNULL(@Op, A.Op)
                    , A.Err = ISNULL(@Err, A.Err)
                    , A.RowCnt = ISNULL(@RowCnt, A.RowCnt)
                    , A.RunId = ISNULL(@RunId, A.RunId)
                    FROM dbo.Audit A
                    WHERE A.AuditId = @AuditId
                ;

                -- STEP 9: For debug mode, report audit is updated.
                IF @IsDebug = 1
                    RAISERROR('Audit record %d is updated.', 0, 1, @AuditId) WITH NOWAIT;

            END; -- UPDATE AUDIT MODE

        -- STEP 10: Check for close audit mode.    
        ELSE IF @AuditMode = 2

           BEGIN -- CLOSE AUDIT MODE
           
                -- STEP 11: Update audit mode to closed state.
                UPDATE A SET
                      A.EndDT = GETDATE()
                    , A.Err = ISNULL(@Err, Err)
                    FROM dbo.Audit A
                    WHERE A.AuditId = @AuditId
                ;

                -- STEP 12: For debug mode, report audit is closed.
                IF @IsDebug = 1 
                    RAISERROR('Audit record %d closed.', 0, 1, @AuditId) WITH NOWAIT;
                 
           END; -- CLOSE AUDIT MODE
           
        -- STEP 13: Handle unknown audit mode conditions.
        ELSE

            BEGIN -- UNKNOWN MODE CONDITION
                
                -- STEP 14: Handle unknown mode condition.
                RAISERROR('Invalid audit mode specified for an existing audit record. Mode=%d', 11, 11, @AuditMode) WITH NOWAIT;
              
            END; -- UNKNOWN MODE CONDITION
            
    END; -- EXISTING AUDIT
    
END TRY
BEGIN CATCH

    -- Rollback uncommitted changes.
    IF @@TRANCOUNT > @InitialTransactionCount ROLLBACK TRAN;

    SELECT @ReturnCode = ERROR_NUMBER();
        
    -- Record an error condition.  
    UPDATE A SET 
          A.Err = ERROR_NUMBER()
        , A.EndDT = GETDATE()  -- Error implicitly closes an audit cycle.
        FROM dbo.Audit A
        WHERE A.AuditId = @AuditId
    ;
    
    SELECT @Message = 'Audit %d: failed with the message:' + ERROR_MESSAGE();
    RAISERROR(@Message, 11, 11, @AuditId) WITH NOWAIT;
    
END CATCH

RETURN @ReturnCode
go

If  exists (select * FROM sys.objects where object_id = OBJECT_ID(N'dbo.Dest_Tbl') AND type in (N'U'))
	drop table dbo.Dest_Tbl;
create table dbo.Dest_Tbl(
		PurchaseOrderId int NOT NULL,
		TotalDue varchar(50) NULL,
		AuditId int NULL,
		ActionId int NULL,
	 CONSTRAINT PK_Dest_Tbl PRIMARY KEY CLUSTERED (PurchaseOrderId ASC));

use ETL_Controller
go
If  exists (select * FROM sys.objects where object_id = OBJECT_ID(N'dbo.Src_Tbl') AND type in (N'U'))
	drop table Src_Tbl;
create table dbo.Src_Tbl(
		PurchaseOrderId int IDENTITY(1,1) NOT NULL,
		TotalDue varchar(50) NULL,
		OperationId int NULL,
	 CONSTRAINT PK_Src_Tbl PRIMARY KEY CLUSTERED (PurchaseOrderId ASC));

	 
declare @BatchName varchar(30),
		@ControlValue varchar(30),
		@RowCount int
set @BatchName = 'IncrementalStaging'

-- insert 2 rows into the source table
insert Src_Tbl
select 20,4
union 
select 11,4

-- make sure that the ControlValue is initialized to zero as it would be if the IncrementalStaging had not yet run
update a
set AttributeValue = 0
from ETLBatch b
join ETLStepAttribute a
on	b.BatchID = a.BatchID
and	b.BatchName = @BatchName
and AttributeName  = 'ControlValue'

-- Call IncrementalStaging batch for first time
exec dbo.prc_Execute @BatchName,'debug;forcestart'

-- Verify that the ControlValue recorded in the ETLStepRunCounter table for the step = the maximum control value in the source table = 4
select	@ControlValue = c.CounterValue 
from	ETLStepRunCounter c
join	ETLBatch b
on		c.BatchID = b.BatchID
and		c.CounterName = 'ControlValue'
and		c.StepID = 2
and		c.RunID = (
	select	MAX(RunID)
	from	ETLBatchRun r
	join	ETLBatch b
	on		c.BatchID = b.BatchID
	and		b.BatchName = @BatchName)

if @ControlValue <> 4
   RAISERROR ('ControlValue should = 4 but it = %s',11,11,@ControlValue)

-- Verify that the RowsExtracted recorded in the ETLStepRunCounter table for the step = the number of rows inserted in the source table = 2
select	@ControlValue = c.CounterValue 
from	ETLStepRunCounter c
join	ETLBatch b
on		c.BatchID = b.BatchID
and		c.CounterName = 'RowsExtracted'
and		c.StepID = 2
and		c.RunID = (
	select	MAX(RunID)
	from	ETLBatchRun r
	join	ETLBatch b
	on		c.BatchID = b.BatchID
	and		b.BatchName = @BatchName)

if @ControlValue <> 2
   RAISERROR ('RowsExtracted should = 2 but it = %s',11,11,@ControlValue)

insert Src_Tbl
select 32,6

update Src_Tbl
set TotalDue = 43, OperationId = 6
where PurchaseOrderId = 1

exec dbo.prc_Execute @BatchName,'debug;forcestart'

-- Verify that the ControlValue recorded in the ETLStepRunCounter table for the step = the maximum control value in the source table = 6
select	@ControlValue = c.CounterValue 
from	ETLStepRunCounter c
join	ETLBatch b
on		c.BatchID = b.BatchID
and		c.CounterName = 'ControlValue'
and		c.StepID = 2
and		c.RunID = (
	select	MAX(RunID)
	from	ETLBatchRun r
	join	ETLBatch b
	on		c.BatchID = b.BatchID
	and		b.BatchName = @BatchName)

if @ControlValue <> 6
   RAISERROR ('ControlValue should = 6 but it = %s',11,11,@ControlValue)

select	@ControlValue = c.CounterValue 
from	ETLStepRunCounter c
join	ETLBatch b
on		c.BatchID = b.BatchID
and		c.CounterName = 'RowsExtracted'
and		c.StepID = 2
and		c.RunID = (
	select	MAX(RunID)
	from	ETLBatchRun r
	join	ETLBatch b
	on		c.BatchID = b.BatchID
	and		b.BatchName = @BatchName)

-- Verify that the RowsExtracted recorded in the ETLStepRunCounter table for the step = the number of rows inserted and updated in the source table = 2
if @ControlValue <> 2
   RAISERROR ('RowsExtracted should = 2 but it = %s',11,11,@ControlValue)


select @RowCount = COUNT(*) 
from Dest_DB.dbo.Dest_Tbl d
join Dest_DB.dbo.Audit a
on d.AuditId = a.AuditId
where	(PurchaseOrderId = 1 and TotalDue = 43 and d.AuditId = 2 and ActionId = 2) 
or		(PurchaseOrderId = 2 and TotalDue = 11 and d.AuditId = 1 and ActionId = 1) 
or		(PurchaseOrderId = 3 and TotalDue = 32 and d.AuditId = 2 and ActionId = 1)

if @RowCount <> 3
   RAISERROR ('Dest_DB.dbo.Dest_Tbl should have 3 rows but it has %i rows',11,11,@RowCount)

If  exists (select * FROM sys.objects where object_id = OBJECT_ID(N'dbo.Src_Tbl') AND type in (N'U'))
	drop table Src_Tbl;

If exists (select name from sys.databases where name = N'Dest_DB')
	drop database [Dest_DB]