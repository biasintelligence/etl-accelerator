/*
select [dbo].[fn_EventTypeCheck] ('ssss')
*/
create function [dbo].[fn_EventTypeCheck] (@EventType sysname)
returns uniqueidentifier
 AS

begin
/******************************************************************************
** File:	[fn_EventTypeCheck].sql
** Name:	[dbo].[fn_EventTypeCheck]

** SD Location: /function/fn_EventTypeCheck.sql:

** Desc:	check the Event Type
**          
**
** Params:
 EventType accepts EventType Name or GUID
 
** Returns:
** 0/1 success/failure

** Author:	andrey@biasintelligence.com
** Date:	2010/03/12
** ****************************************************************************
** CHANGE HISTORY
** ****************************************************************************
** Date				Author	version	4	#bug			Description
** ----------------------------------------------------------------------------------------------------------

*/
declare @ETID uniqueidentifier

if (@EventType like '________-____-____-____-____________')
begin
  select @ETID = EventTypeID
    from dbo.EventType
   where EventTypeID = CAST(@EventType as uniqueidentifier)

end
else
begin
  select @ETID = EventTypeID
    from dbo.EventType
   where EventTypeName = @EventType
end
  
return @ETID    
end
;