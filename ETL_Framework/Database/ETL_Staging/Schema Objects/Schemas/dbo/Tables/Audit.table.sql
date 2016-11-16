CREATE TABLE [dbo].[Audit] (
    [AuditId]      INT             IDENTITY (1, 1) NOT NULL,
    [AuditObject]  [sysname]       NULL,
    [SourceObject] NVARCHAR (1000) NULL,
    [StartDT]      DATETIME        NULL,
    [EndDT]        DATETIME        NULL,
    [Op]           NVARCHAR (10)   NULL,
    [Err]          INT             NULL,
    [RowCnt]       INT             NULL,
    [RunId]        INT             NULL
);

