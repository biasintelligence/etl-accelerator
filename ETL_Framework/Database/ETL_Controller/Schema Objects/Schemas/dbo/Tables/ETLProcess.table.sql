CREATE TABLE [dbo].[ETLProcess] (
    [ProcessID] INT       IDENTITY (1, 1) NOT NULL,
    [Process]   [sysname] NOT NULL,
    [Param]     [nvarchar](2048) NULL,
    [ScopeID]   TINYINT   NULL,
    PRIMARY KEY CLUSTERED ([ProcessID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);

