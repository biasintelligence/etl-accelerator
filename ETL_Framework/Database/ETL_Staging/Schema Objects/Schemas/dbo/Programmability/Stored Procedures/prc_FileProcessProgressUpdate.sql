CREATE PROCEDURE dbo.prc_FileProcessProgressUpdate
	 @fileId int = null
	,@FileName nvarchar(100) = null
	,@SourceName nvarchar(100) = null
	,@ProgressStatusId tinyint = null
	,@ProgressStatusName nvarchar(100) = null
	,@processId int = 0
	,@priority int = 0
	,@options nvarchar(100) = null
AS
/*
exec dbo.prc_FileProcessProgressUpdate @fileId = 3, @progressStatusId = 0,@priority = 2
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

	IF (@fileId IS NULL AND (@FileName IS NULL OR @SourceName IS NULL))
	BEGIN
		SET @msg = 'Invalid parameters. @fileId or @fileName and @sourceName are required';   
		THROW 50001, @msg, 1;
	END

	DECLARE @fId int = (SELECT fileId FROM dbo.FileProcessProgress
	 WHERE fileId = ISNULL(@fileId,fileId)
	 AND [fileName] = ISNULL(@fileName,[fileName])
	 AND [sourceName] = ISNULL(@sourceName,[sourceName]));

	IF (@fId IS NULL)
	BEGIN
		SET @msg = 'Invalid parameters. @fileId = ' + ISNULL(CAST(@fileId AS nvarchar(30)),'null')
		+ ' or @fileName = ' + ISNULL(@fileName,'null')
		+ ' or @sourceName = ' + ISNULL(@sourceName,'null') ;

		THROW 50002, @msg, 1;
	END


	IF (@ProgressStatusId IS NULL AND @ProgressStatusName IS NULL)
	BEGIN
		SET @msg = 'Invalid parameters. @ProgressStatusName or ProgressStatusName are required';   
		THROW 50001, @msg, 1;
	END

	DECLARE @statusId tinyint = (SELECT progressStatusId FROM dbo.ProgressStatus
	WHERE progressStatusId = ISNULL(@progressStatusId,progressStatusId)
	AND progressStatusName = ISNULL(@progressStatusName,progressStatusName));

	IF (@statusId IS NULL)
	BEGIN
		SET @msg = 'Invalid parameters. @progressStatusId = ' + ISNULL(CAST(@progressStatusId AS nvarchar(30)),'null')
		+ ' or @progressStatusName = ' + ISNULL(@progressStatusName,'null');

		THROW 50002, @msg, 1;
	END

	UPDATE dbo.FileProcessProgress
	 SET ProgressStatusId = isnull(@statusId,ProgressStatusId)
		,processId = isnull(@processId,processId)
		,[priority] = isnull(@priority,[priority])
		,changeDt = getDate()
		,changeBy = suser_sname()
	WHERE fileId = @fId;

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
