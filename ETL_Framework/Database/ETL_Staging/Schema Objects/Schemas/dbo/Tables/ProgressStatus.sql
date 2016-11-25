CREATE TABLE dbo.ProgressStatus
(ProgressStatusId tinyint not null primary key
,ProgressStatusName nvarchar(30) not null unique
,[CreateDt] datetime not null default getdate()
,[ChangeDt] datetime not null default getdate()
,[ChangeBy] nvarchar(30) not null default suser_sname()
);