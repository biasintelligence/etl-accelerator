/*
declare @args xml
exec [dbo].[prc_EventPost] @EventType = 'ssss'
select @args
*/
create procedure [dbo].[prc_EventPost] (
	 @EventType sysname
	,@EventPosted datetime = null
	,@EventArgs xml(EventArgsCollection) = null
    ,@Options nvarchar(100) = null
    ) AS

begin
/******************************************************************************
** File:	[prc_EventPost].sql
** Name:	[dbo].[prc_EventPost]

** SD Location: /procedure/prc_EventPost.sql:

** Desc:	post an event
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
declare @EventArgsExpected sysname
declare @EventArgsReceived sysname

set @proc = object_name(@@procid);
set @trancount = @@TRANCOUNT;
set @debug = case when CHARINDEX('debug',@Options) > 0 then 1 else 0 end;

begin try

   set @etid = [dbo].[fn_EventTypeCheck] (@EventType)      
   if (@etid is null)
      raiserror ('Requested EventType %s is not found',11,11,@EventType)
         
   if (@EventArgs is not null)
   begin
      select @EventArgsExpected = EventArgsSchema
        from dbo.EventType
       where EventTypeID = @etid
         
      set @EventArgsReceived = @EventArgs.value('namespace-uri(./*[1])','sysname')
      if (@EventArgsExpected <> @EventArgsReceived)
         raiserror ('Invalid EventArgs type is received = %s. Expected %s',11,11,@EventArgsReceived,@EventArgsExpected)
   end
   
   begin tran
   
   insert dbo.EventLog
   (EventTypeID,EventID,ReceiveDT,PostDT,EventArgs)
   select EventTypeID,EventID,ReceiveDT,PostDT,EventArgs
     from dbo.[Event]
    where EventTypeID = @etid;
    
    merge dbo.[Event] as dst
    using (select @etid,newid(),getdate(),@EventPosted,@EventArgs) as src
    (EventTypeID,EventID,ReceiveDT,PostDT,EventArgs)
    on dst.EventTypeID = src.EventTypeID
    when matched then
    update
       set dst.EventID = src.EventID
          ,dst.ReceiveDT = src.ReceiveDT
          ,dst.PostDT = src.PostDT
          ,dst.EventArgs = src.EventArgs
     when not matched by target then
     insert (EventTypeID,EventID,ReceiveDT,PostDT,EventArgs)
     values (src.EventTypeID,src.EventID,src.ReceiveDT,src.PostDT,src.EventArgs);
   
   commit tran  
    
end try
begin catch
   if @@TRANCOUNT > @trancount
      rollback tran
          
   set @msg = '%s failed with the message:' + ERROR_MESSAGE()
   raiserror (@msg,11,11,@proc)
end catch
end
;