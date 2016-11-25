CREATE TABLE dbo.FileProcessProgress
([FileId] int identity(1,1) primary key
,[FileName] nvarchar(100) not null
,[FullName] nvarchar(1000) not null
,[SourceName] nvarchar(100) not null
,[ProgressStatusId] tinyint not null
,[ProcessId] int null
,[Priority] int not null default 0
,[CreateDt] datetime not null default getdate()
,[ChangeDt] datetime not null default getdate()
,[ChangeBy] nvarchar(30) not null default suser_sname()
,unique (SourceName,[FileName])
);