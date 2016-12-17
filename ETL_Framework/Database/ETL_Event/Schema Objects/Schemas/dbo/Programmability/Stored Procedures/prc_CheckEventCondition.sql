CREATE PROCEDURE [dbo].[prc_CheckEventCondition]
	(@EventType sysname
	,@WatermarkEventType sysname
	,@Status bit = null output
    ,@Options nvarchar(100) = null
    ) AS

begin
/******************************************************************************
** File:	[prc_CheckEventCondition].sql
** Name:	[dbo].[prc_CheckEventCondition]


** Desc:	if Event.receiveDt > WatermarkEvent.receivDt then true
**          
**
** Params:
 EventType accepts EventType Name or GUID
 
** Returns:
** 0/1 success/failure

** Author:	andrey@biasintelligence.com
** Date:	2016/12/16
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
declare @wetid uniqueidentifier

set @proc = object_name(@@procid);
set @trancount = @@TRANCOUNT;
set @debug = case when CHARINDEX('debug',@Options) > 0 then 1 else 0 end;

begin try

   set @etid = [dbo].[fn_EventTypeCheck] (@EventType);  
   if (@etid is null)
      raiserror ('Requested EventType %s is not found',11,11,@EventType);

   set @wetid = [dbo].[fn_EventTypeCheck] (@WatermarkEventType);  
   if (@wetid is null)
      raiserror ('Requested WatermarkEventType %s is not found',11,11,@WatermarkEventType);

	declare @eReceiveDt datetime = (select top 1 receiveDt from dbo.[Event] where EventTypeID = @etId);        
	declare @wReceiveDt datetime = (select top 1 receiveDt from dbo.[Event] where EventTypeID = @wetId);   
	
	set @Status =
		case when @eReceiveDt is null then 0
			 when @wReceiveDt is null then 1
			 when @wReceiveDt < @eReceiveDt then 1
			 else 0
		 end;
end try
begin catch
   if @@TRANCOUNT > @trancount
      rollback tran
          
   set @msg = '%s failed with the message:' + ERROR_MESSAGE();
   raiserror (@msg,11,11,@proc);
end catch
end
;