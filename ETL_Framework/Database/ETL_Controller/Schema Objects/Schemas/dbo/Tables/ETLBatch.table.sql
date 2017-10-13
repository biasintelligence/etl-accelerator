CREATE TABLE [dbo].[ETLBatch] (
    [BatchID]      INT           NOT NULL,
    [BatchName]    VARCHAR (30)  NOT NULL,
    [BatchDesc]    VARCHAR (500) NULL,
    [OnSuccessID]  INT           NULL,
    [OnFailureID]  INT           NULL,
    [IgnoreErr]    TINYINT       NULL,
    [RestartOnErr] TINYINT       NULL,
    [StatusDT]     DATETIME      NULL,
    [StatusID]     TINYINT       NULL,
    [Err]          INT           NULL,
    [EndTime]      DATETIME      NULL,
    PRIMARY KEY CLUSTERED ([BatchID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF),
    UNIQUE NONCLUSTERED ([BatchName] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF) ON [PRIMARY]
);

