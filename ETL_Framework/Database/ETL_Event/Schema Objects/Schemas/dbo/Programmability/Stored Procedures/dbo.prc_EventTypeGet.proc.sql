/*
--select * from eventtype
declare @args xml
exec [dbo].[prc_EventTypeGet] @EventArgsXSD = @args out,@EventType= 'TEST_EVENT_LOCAL'
select @args
*/
create procedure [dbo].[prc_EventTypeGet] (
	@EventArgsXSD xml = null out
	,@EventTypeID uniqueidentifier = null out
	,@EventType sysname
    ,@Options nvarchar(100) = null
    ) AS

begin
/******************************************************************************
** File:	[prc_EventTypeGet].sql
** Name:	[dbo].[prc_EventTypeGet]

** SD Location: /procedure/prc_EventTypeGet.sql:

** Desc:	returns EventType Args xsd
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
set nocount on

declare @trancount int;
declare @rows int;
declare @debug bit;
declare @msg nvarchar(1000);
declare @proc sysname;
declare @etid uniqueidentifier

set @proc = object_name(@@procid);
set @trancount = @@TRANCOUNT;
set @debug = case when CHARINDEX('debug',@Options) > 0 then 1 else 0 end;

begin try

   set @etid = [dbo].[fn_EventTypeCheck] (@EventType)      
   if (@etid is null)
      raiserror ('Requested EventType %s is not found',11,11,@EventType) 
   
   select @EventArgsXSD = xml_schema_namespace('dbo','EventArgsCollection',EventArgsSchema)
         ,@EventTypeID = EventTypeID
     from dbo.EventType
    where EventTypeID = @etid
    
end try
begin catch
   if @@TRANCOUNT > @trancount
      rollback tran
          
   set @msg = '%s failed with the message:' + ERROR_MESSAGE()
   raiserror (@msg,11,11,@proc)
end catch
end
;