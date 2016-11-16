
CREATE FUNCTION [dbo].[fn_GetBase64String]
(
	@StringToEncode nvarchar(max)
) RETURNS nvarchar(max)
AS
BEGIN
/******************************************************************
**	D File:         fn_GetBase64String.SQL
**
**	D Desc:         Encode string as base 64 to pass to DeltaExtractor
**
**	D Auth:         vensri
**	D Date:         12/18/2007
**
**	Param:			@StringToEncode - The string to encode
**  Returns:		nvarchar(max) the base 64 encoded string
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
******************************************************************/
	DECLARE @xmlDoc xml

	SELECT @xmlDoc = (select cast( @StringToEncode as varbinary(max))
	FOR XML RAW ('row'), elements, BINARY BASE64, type)

	return (select @xmlDoc.value('(/row)[1]','nvarchar(max)'))

END