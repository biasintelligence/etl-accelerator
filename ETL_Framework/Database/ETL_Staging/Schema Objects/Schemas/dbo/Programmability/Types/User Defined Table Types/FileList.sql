CREATE TYPE [dbo].[FileList] AS TABLE
(
	[Name] nvarchar(100) not null,
	[Path] nvarchar(1000) not null,
	[Source] nvarchar(100) not null

)
