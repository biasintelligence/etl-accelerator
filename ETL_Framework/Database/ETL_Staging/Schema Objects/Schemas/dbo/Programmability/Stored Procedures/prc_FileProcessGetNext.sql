CREATE PROCEDURE dbo.prc_FileProcessGetNext
	 @SourceName nvarchar(100) = null
	,@Count int = 1
	,@processId int = 0
	,@options nvarchar(100) = null
AS
/*
exec dbo.prc_FileProcessGetNext 'testSource',5,0
*/
	SET NOCOUNT ON;
	--SET XACT_ABORT ON;
	DECLARE	 @msg nvarchar(max)
			,@debug bit
			,@tran int
			,@err int
			,@proc sysname = object_name(@@procid)
			,@rows int;

	SET @err = 0;
	SET @tran = @@TRANCOUNT;
			 
	BEGIN TRY

	SET @debug = CASE WHEN charindex('debug', @options) > 0 THEN 1 ELSE 0 END;


	UPDATE dst
	 SET ProgressStatusId = 1 --started
		,dst.processId = isnull(@processId,dst.processId)
		,dst.changeDt = getDate()
		,dst.changeBy = suser_sname()
		OUTPUT deleted.fileId,deleted.[fileName],deleted.[fullName],deleted.sourceName,inserted.processId
	FROM dbo.FileProcessProgress dst
	JOIN (SELECT TOP (@count) fp.fileId FROM dbo.FileProcessProgress fp WITH (TABLOCK)
	WHERE fp.ProgressStatusId = 0 AND fp.SourceName = ISNULL(@sourceName,fp.sourceName)
	ORDER BY fp.[Priority] desc, fp.CreateDt asc) lst
	ON dst.fileId = lst.fileId;


	RETURN 0;

	END TRY
	BEGIN CATCH
	   IF @@TRANCOUNT > @tran
	      ROLLBACK TRAN;
	      
	   SET @msg = ERROR_MESSAGE();
	   SET @Err = ERROR_NUMBER();

	   THROW @Err, @msg, 1;
	END CATCH

RETURN @err;
go
