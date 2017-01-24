SET NOCOUNT ON;
GO

DECLARE 
	@ErrorMessage VARCHAR(8000);


BEGIN TRY

	SET IDENTITY_INSERT dbo.ETLProcess ON;

	DECLARE @ETLProcess AS TABLE 
	(
		[ProcessID]		INT			NOT NULL,
		[Process]		[sysname]	NOT NULL,
		[Param]			[sysname]	NULL,
		[ScopeID]		TINYINT		NULL
	);

	INSERT INTO @ETLProcess([ProcessID], [Process], [Param], [ScopeID])
			SELECT 1 AS ProcessID, 'dbo.prc_ExecSql' AS Process, NULL AS [Param], 15 AS ScopeID
	UNION	SELECT 2 AS ProcessID, 'dbo.prc_ExecCmd' AS Process, NULL AS [Param], 15 AS ScopeID
	UNION	SELECT 3 AS ProcessID, 'dbo.prc_ExecSql' AS Process, '@pSQLAttribute = ''SQL_01''' AS [Param], 15 AS ScopeID
	UNION	SELECT 5 AS ProcessID, 'dbo.prc_ExecSql' AS Process, '@pSQLAttribute = ''SQL_ER''' AS [Param], 15 AS ScopeID
	UNION	SELECT 6 AS ProcessID, 'dbo.prc_ExecCmd' AS Process, '@pCMDAttribute = ''CMD_01''' AS [Param], 15 AS ScopeID
	UNION	SELECT 7 AS ProcessID, 'dbo.prc_CLRExecDE' AS Process, NULL AS [Param], 15 AS ScopeID
	UNION	SELECT 8 AS ProcessID, 'dbo.prc_CLRExecConsole' AS Process, NULL AS [Param], 15 AS ScopeID
	UNION	SELECT 11 AS ProcessID, 'dbo.prc_FileCheck' AS Process, NULL AS [Param], 12 AS ScopeID
	UNION	SELECT 12 AS ProcessID, 'dbo.prc_EventCheck' AS Process, NULL AS [Param], 12 AS ScopeID
	UNION	SELECT 13 AS ProcessID, 'dbo.prc_ExecSql' AS Process, '@pSQLAttribute = ''SQL_02''' AS [Param], 15 AS ScopeID;


	MERGE dbo.ETLProcess AS Dst 
	USING @ETLProcess AS Src
		ON Dst.ProcessID = Src.ProcessID
	WHEN MATCHED AND (Dst.[Process] <> Src.[Process] OR Dst.[Param] <> Src.[Param] OR Dst.[ScopeID] <> Src.[ScopeID]) THEN
		UPDATE SET
			Dst.[Process] = Src.[Process],
			Dst.[Param] = Src.[Param],
			Dst.[ScopeID] = Src.[ScopeID]
	WHEN NOT MATCHED BY TARGET THEN
		INSERT([ProcessID], [Process], [Param], [ScopeID]) 
		VALUES([ProcessID], [Process], [Param], [ScopeID]);
	
	RAISERROR('%d row(s) merged into dbo.ETLProcess', 10, 1, @@ROWCOUNT);

	SET IDENTITY_INSERT dbo.ETLProcess OFF;

END TRY
BEGIN CATCH

	SET @ErrorMessage = 'Error Occurred at Line:' + CAST(ERROR_LINE() AS VARCHAR) + 'with message: ' + ERROR_MESSAGE();

	RAISERROR('%s', 18, 127, @ErrorMessage);

END CATCH
	
