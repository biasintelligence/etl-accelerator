/*
exec [dbo].[prc_EventLogPurge] @EventType = 'TEST_EVENT_LOCAL',@Retention = -1
*/
create procedure [dbo].[prc_EventLogPurge] (
	 @EventType sysname = null
	,@Retention int = null
    ,@Options nvarchar(100) = null
    ) AS

begin
/******************************************************************************
** File:	[prc_EventLogPurge].sql
** Name:	[dbo].[prc_EventLogPurge]

** SD Location: /procedure/prc_EventLogPurge.sql:

** Desc:	purge all event history based on retention policy
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

set @Retention = nullif(@Retention,0)

begin try

   if (@EventType is not null)
   begin
      set @etid = [dbo].[fn_EventTypeCheck] (@EventType)      
      if (@etid is null)
         return
   end 

   delete l
     from dbo.EventLog l
     join dbo.EventType t on l.EventTypeID = t.EventTypeID
    where  (t.EventTypeID = @etid or @etid is null)
      and  l.ReceiveDT < dateadd(day,-ISNULL(@Retention,t.[LogRetention]),getdate()) 
      
   set @rows = @@ROWCOUNT
   raiserror ('Purge %d records from dbo.EventLog',0,1,@rows)
   
   delete l
     from dbo.[Event] l
     join dbo.EventType t on l.EventTypeID = t.EventTypeID
    where  (t.EventTypeID = @etid or @etid is null)
      and  l.ReceiveDT < dateadd(day,-ISNULL(@Retention,t.[LogRetention]),getdate()) 
      
   set @rows = @@ROWCOUNT
   raiserror ('Purge %d records from dbo.Event',0,1,@rows)
    
end try
begin catch
   if @@TRANCOUNT > @trancount
      rollback tran
          
   set @msg = '%s failed with the message:' + ERROR_MESSAGE()
   raiserror (@msg,11,11,@proc)
end catch
end
;