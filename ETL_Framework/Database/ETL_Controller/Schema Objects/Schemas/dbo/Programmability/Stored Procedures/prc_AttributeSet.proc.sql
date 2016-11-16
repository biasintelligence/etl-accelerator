/*
declare @pHeader xml
declare @pValue nvarchar(1000)
exec dbo.prc_CreateHeader @pHeader out,20,1,null,4,1
--select @pHeader
exec [dbo].[prc_AttributeSet] @pHeader,'TEST',null
exec dbo.prc_AttributeGet @pValue out,@pHeader,'TEST'
select @pValue
*/
create procedure [dbo].[prc_AttributeSet] (
        @pHeader xml([ETLController])
       ,@pName nvarchar(100)
       ,@pValue nvarchar(4000)
)
 as
begin
/******************************************************************************
** File:	[prc_AttributeSet].sql
** Name:	[dbo].[prc_AttributeSet]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Function/prc_AttributeSet.sql:

** Desc:	set  user defined attribute value for batch/step/const combination
**          
**
** Params:
** Returns:
**
** Author:	andreys
** Date:	08/01/2007
** ****************************************************************************
** CHANGE HISTORY
** ****************************************************************************
** Date				Author	version	4	#bug			Description
** ----------------------------------------------------------------------------------------------------------

*/
set nocount on
declare @retcode int
declare @msg nvarchar(max)
declare @err int
declare @proc sysname

declare @BatchID int
declare @StepID int
declare @RunID int
declare @ConstID int
declare @ProcErr int
declare @ProcName sysname

set @retcode = 0
set @err = 0
set @proc = object_name(@@procid)
begin try
   exec @ProcErr = dbo.[prc_ReadHeader] @pHeader,@BatchID out,@StepID out,@ConstID out,null,null,null;
   exec @ProcErr = dbo.prc_ETLAttributeSet @BatchID,@StepID,@ConstID,@pName,@pValue;
end try
begin catch
   set @retcode = ERROR_NUMBER()
   set @msg = ERROR_MESSAGE()
   raiserror (@msg,11,11)
end catch
   return (@retcode)
end