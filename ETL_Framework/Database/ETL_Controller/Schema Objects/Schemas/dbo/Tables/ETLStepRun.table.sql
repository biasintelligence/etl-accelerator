CREATE TABLE [dbo].[ETLStepRun] (
    [RunID]     INT          NOT NULL,
    [BatchID]   INT          NOT NULL,
    [StepID]    INT          NOT NULL,
    [StatusDT]  DATETIME     NULL,
    [StatusID]  TINYINT      NULL,
    [spid]      INT          NULL,
    [StepOrder] VARCHAR (10) NULL,
    [IgnoreErr] TINYINT      NULL,
    [Err]       INT          NULL,
    [StartTime] DATETIME     NULL,
    [EndTime]   DATETIME     NULL,
    [SeqGroup]  VARCHAR (10) NULL,
    [PriGroup]  VARCHAR (10) NULL,
    [SvcName]   [sysname]    NULL,
    PRIMARY KEY CLUSTERED ([RunID] ASC, [StepID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF),
    UNIQUE NONCLUSTERED ([BatchID] ASC, [StepID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF) ON [PRIMARY]
);

