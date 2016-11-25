CREATE PROCEDURE dbo.prc_FileProcessRegisterNew
	 @list [dbo].[FileList] readonly
	,@processId int = 0
	,@priority int = 0
	,@options nvarchar(100) = null
AS
/*
declare @list [dbo].[FileList]
insert @list
values ('testFile1.json','c:\files\testFile1.json','testSource')
,('testFile2.json','c:\files\testFile1.json','testSource')
exec dbo.prc_FileProcessRegisterNew @list,1,1

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
	IF NOT EXISTS (SELECT 1 FROM @list)
		RETURN 0;


	DECLARE @StatusId tinyint = 0; -- new records

	MERGE dbo.FileProcessProgress dst
	USING (SELECT [Name], [Path], [Source] FROM @list) src ([FileName], [FullName], [SourceName])
	ON src.[FileName] = dst.[FileName] AND src.[SourceName] = dst.[SourceName]
	WHEN NOT MATCHED BY TARGET THEN
	INSERT ([FileName], [FullName], [SourceName],[ProgressStatusId],[ProcessId],[Priority])
	VALUES ([FileName], [FullName], [SourceName],@statusId,@processId,@priority)
	;

	END TRY
	BEGIN CATCH
	   IF @@TRANCOUNT > @tran
	      ROLLBACK TRAN;
	      
	   SET @msg = ERROR_MESSAGE();
	   SET @Err = ERROR_NUMBER();

	   THROW @Err, @msg, 1;
	END CATCH

RETURN @err;
