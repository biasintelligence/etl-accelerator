﻿CREATE TABLE [dbo].[ETLStepConstraintAttribute] (
    [BatchID]        INT            NOT NULL,
    [StepID]         INT            NOT NULL,
    [ConstID]        INT            NOT NULL,
    [AttributeName]  VARCHAR (100)  NOT NULL,
    [AttributeValue] VARCHAR (8000) NULL,
    PRIMARY KEY CLUSTERED ([BatchID] ASC, [StepID] ASC, [ConstID] ASC, [AttributeName] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF)
);

