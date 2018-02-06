CREATE FUNCTION [dbo].[fn_ParseAttributeName]
(
	@counterName varchar(100)
)
RETURNS @returntable TABLE
(
	partId int,
	partValue varchar(100)
)
AS
BEGIN
	declare @tbl table (id int identity(1,1),value varchar(100));
	insert @tbl (value)
	output inserted.id as partId
	,inserted.value as partValue
	into @returntable
	select top(3) value from string_split(@counterName,':');
	return;
END
