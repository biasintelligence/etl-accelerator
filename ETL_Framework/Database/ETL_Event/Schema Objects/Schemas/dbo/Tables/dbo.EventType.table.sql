CREATE TABLE [dbo].[EventType] (
    [EventTypeID]     UNIQUEIDENTIFIER NOT NULL,
    [EventTypeName]   [sysname]        NOT NULL,
    [EventArgsSchema] [sysname]        NULL,
    [SourceName]      [sysname]        NULL,
    [LogRetention]    INT              NULL,
    [CreateDT]        DATETIME         NOT NULL,
    [ModifyDT]        DATETIME         NOT NULL
);

