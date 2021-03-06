﻿CREATE TABLE [dbo].[ETLBatchRun] (
    [RunID]      INT           IDENTITY (1, 1) NOT NULL,
    [BatchID]    INT           NOT NULL,
    [StatusDT]   DATETIME      NULL,
    [StatusID]   TINYINT       NULL,
    [Err]        INT           NULL,
    [StartTime]  DATETIME      NULL,
    [EndTime]    DATETIME      NULL,
    [ModifiedBY] NVARCHAR (100) DEFAULT (suser_sname()) NOT NULL,
    PRIMARY KEY CLUSTERED ([RunID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF),
    UNIQUE NONCLUSTERED ([BatchID] ASC, [RunID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF) ON [PRIMARY]
);

