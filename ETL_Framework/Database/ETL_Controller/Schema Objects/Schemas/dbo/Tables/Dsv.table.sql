CREATE TABLE [dbo].[Dsv] (
    [DsvID]   INT           IDENTITY (1, 1) NOT NULL,
    [DsvName] NVARCHAR (30) NOT NULL,
    [DsvType] TINYINT       NOT NULL,
    [FromDT]  DATETIME      DEFAULT (getdate()) NOT NULL,
    [ToDT]    DATETIME      DEFAULT ('9999-12-31') NOT NULL,
    [Dsv]     XML           NOT NULL
);

