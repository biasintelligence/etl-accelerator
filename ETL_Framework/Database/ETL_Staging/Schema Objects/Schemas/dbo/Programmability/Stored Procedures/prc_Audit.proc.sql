
/*
declare @Audit_Key int
exec dbo.[prc_Audit] @AuditMode = 1,@AuditId = @Audit_Key out,@AuditObject='Test',@Options='debug'
--select @Audit_Key
exec dbo.[prc_Audit] @AuditMode = 0,@AuditId = @Audit_Key,@Op='1',@RowCnt=100,@Options='debug'
exec dbo.[prc_Audit] @AuditMode = 2,@AuditId = @Audit_Key,@Op='1',@RowCnt=100,@Options='debug'
select * from Audit
*/
CREATE PROCEDURE dbo.prc_Audit
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
/******************************************************************************
** File: prc_Audit.proc.sql
** Name: dbo.prc_Audit
**
** Desc: Set process run record; generates unique audit key.
**          
** Params: 
**    AudutMode: {0=Update audit; 1=Start audit; 2=Close audit}
**
** Returns:
**    Return code: {0=Success; Error code otherwise}.
*******************************************************************************
**       CHANGE HISTORY
*******************************************************************************
** Date        Author    Description
** ----------  --------  ------------------------------------------------------
** 04/20/2009  andrey@biasintelligence.com  Created procedure.
******************************************************************************/

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

GO
