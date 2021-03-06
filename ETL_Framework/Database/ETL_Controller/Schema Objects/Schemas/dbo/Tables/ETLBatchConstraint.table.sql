﻿CREATE TABLE [dbo].[ETLBatchConstraint] (
    [ConstID]    INT          NOT NULL,
    [BatchID]    INT          NOT NULL,
    [ProcessID]  INT          NOT NULL,
    [ConstOrder] VARCHAR (10) NULL,
    [WaitPeriod] INT          NULL,
    PRIMARY KEY CLUSTERED ([BatchID] ASC, [ConstID] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);

