CREATE FUNCTION [dbo].[fn_ParseAttribute]
(
	@AttributeValue nvarchar(max)
)
RETURNS @returntable TABLE
(
	attributeName varchar(100)
)
AS
BEGIN

	insert @returntable
	select left(t.value, t.endChar -1)
	from 
	(
	--look for space,/n/r,/t (whitespaces) inside the attributeName
	select value,charindex('>',value) as endChar,patindex('%[ 
	]%',value) as whiteChar
	 from string_split(@AttributeValue,'<')
	 ) t
	 where t.endChar > 1
	 and (t.endChar < whiteChar or whiteChar = 0)
 
	RETURN
END
