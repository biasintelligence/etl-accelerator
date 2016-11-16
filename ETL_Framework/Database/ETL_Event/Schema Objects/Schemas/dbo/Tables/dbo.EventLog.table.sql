CREATE TABLE [dbo].[EventLog] (
    [EventTypeID] UNIQUEIDENTIFIER NOT NULL,
    [EventID]     UNIQUEIDENTIFIER NOT NULL,
    [ReceiveDT]   DATETIME         NOT NULL,
    [PostDT]      DATETIME         NULL,
    [EventArgs]   XML              NULL
);

