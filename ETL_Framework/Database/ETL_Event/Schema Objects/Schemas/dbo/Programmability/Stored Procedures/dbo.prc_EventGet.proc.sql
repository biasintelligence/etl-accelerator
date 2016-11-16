/*
declare @args xml
exec [dbo].[prc_EventGet] @EventArgs = @args out,@EventType = 'ssss'
select @args
*/
create procedure [dbo].[prc_EventGet] (
     @EventID uniqueidentifier = null out
	,@EventReceived datetime = null out
	,@EventPosted datetime = null out
	,@EventArgs xml = null out
	,@EventType sysname
    ,@Options nvarchar(100) = null
    ) AS

begin
/******************************************************************************
** File:	[prc_EventGet].sql
** Name:	[dbo].[prc_EventGet]

** SD Location: /procedure/prc_EventGet.sql:

** Desc:	returns latest Event data for a type
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
   
   select @EventID = EventID
         ,@EventReceived = ReceiveDT
         ,@EventPosted = PostDT
         ,@EventArgs = [EventArgs]
     from dbo.[Event]
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