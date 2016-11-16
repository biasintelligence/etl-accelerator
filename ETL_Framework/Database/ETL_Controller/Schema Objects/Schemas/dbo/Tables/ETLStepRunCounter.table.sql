CREATE TABLE [dbo].[ETLStepRunCounter] (
    [BatchID]      INT            NOT NULL,
    [StepID]       INT            NOT NULL,
    [RunID]        INT            NOT NULL,
    [CounterName]  VARCHAR (100)  NOT NULL,
    [CounterValue] VARCHAR (1000) NULL,
    [CreatedDTim]  SMALLDATETIME  DEFAULT (getdate()) NOT NULL,
    [ModifiedDTim] SMALLDATETIME  DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([RunID] ASC, [CounterName] ASC, [BatchID] ASC, [StepID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);

