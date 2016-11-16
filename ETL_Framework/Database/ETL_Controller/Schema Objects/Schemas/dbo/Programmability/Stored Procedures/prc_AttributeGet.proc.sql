/*
declare @pHeader xml
declare @pValue nvarchar(1000)
exec dbo.prc_CreateHeader @pHeader out,-20,1,null,4,1
--select @pHeader
exec dbo.prc_AttributeGet @pValue out,@pHeader,'test'
select @pValue

*/
create procedure [dbo].[prc_AttributeGet] (
    @pValue nvarchar(max) output
   ,@pHeader xml([ETLController])
   ,@pName varchar(100) = null
) as
begin
/******************************************************************************
** File:	[prc_CounterGet].sql
** Name:	[dbo].[prc_CounterGet]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Procedure/[prc_CounterGet].sql:

** Desc:	return Counter value to the client
**          
**
** Params:
** Returns:
**
** Author:	andreys
** Date:	10/30/2007
** ****************************************************************************
** CHANGE HISTORY
** ****************************************************************************
** Date				Author	version	4	#bug			Description
** ----------------------------------------------------------------------------------------------------------

*/

set nocount on
declare @err                int
declare @proc               sysname
declare @msg                nvarchar(1000)
declare @debug              tinyint
declare @Options            int
declare @query              nvarchar(max)

declare @BatchID int
declare @StepID int
declare @RunID int
declare @ConstID int
declare @ProcErr int
declare @ProcName sysname

set @err = 0
set @proc = object_name(@@procid)
begin try

exec @ProcErr = dbo.[prc_ReadHeader] @pHeader,@BatchID out,@StepID out,@ConstID out,null,null,null
set @pValue = dbo.[fn_AttributeGet] (@BatchID,@StepID,@ConstID,@pName)

end try
begin catch
   set @Proc = ERROR_PROCEDURE()
   set @pValue = null
   set @Msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   raiserror ('ERROR: PROC %s, MSG: %s',11,11,@Proc,@Msg) 
end catch

return @err
end