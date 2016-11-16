CREATE TABLE [dbo].[Event] (
    [EventTypeID] UNIQUEIDENTIFIER NOT NULL,
    [EventID]     UNIQUEIDENTIFIER NOT NULL,
    [ReceiveDT]   DATETIME         DEFAULT (getdate()) NOT NULL,
    [PostDT]      DATETIME         NULL,
    [EventArgs]   XML              NULL
);

