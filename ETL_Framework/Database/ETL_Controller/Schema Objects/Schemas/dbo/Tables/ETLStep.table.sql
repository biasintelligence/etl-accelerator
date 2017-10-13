CREATE TABLE [dbo].[ETLStep] (
    [StepID]      INT           NOT NULL,
    [BatchID]     INT           NOT NULL,
    [StepName]    VARCHAR (100) NOT NULL,
    [StepDesc]    VARCHAR (500) NULL,
    [StepProcID]  INT           NOT NULL,
    [OnSuccessID] INT           NULL,
    [OnFailureID] INT           NULL,
    [IgnoreErr]   TINYINT       NULL,
    [StepOrder]   VARCHAR (10)  NULL,
    [StatusDT]    DATETIME      NULL,
    [StatusID]    TINYINT       NULL,
    [Err]         INT           NULL,
    PRIMARY KEY CLUSTERED ([BatchID] ASC, [StepID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF),
    UNIQUE NONCLUSTERED ([StepName] ASC, [BatchID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF) ON [PRIMARY]
);

