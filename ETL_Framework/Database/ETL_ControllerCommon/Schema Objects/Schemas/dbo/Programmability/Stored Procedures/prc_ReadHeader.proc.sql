/*
declare @pHeader xml
declare @b int,@s int,@c int,@o int,@r int,@sc int
exec dbo.prc_CreateHeader @pHeader out,1,2,null,4,10,5
select @pHeader
exec dbo.prc_ReadHeader @pHeader,@b out,@s out,@c out,@r out,@o out,@sc out
select @b,@s,@c,@r,@o,@sc
*/
CREATE PROCEDURE [dbo].[prc_ReadHeader]
    @pHeader xml([ETLController])
   ,@pBatchID int = null output
   ,@pStepID int = null output
   ,@pConstID int = null output
   ,@pRunID int = null output
   ,@pOptions int = null output
   ,@pScope int = null output
As
/******************************************************************
**D File:         prc_ReadHeader.SQL
**
**D Desc:         read Header object
**
**D Auth:         andreys
**D Date:         10/27/2007
**
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ProcErr INT
DECLARE @Cnt INT
DECLARE @ProcName sysname
DECLARE @msg nvarchar(max)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0

begin try

;with xmlnamespaces('ETLController.XSD' as etl)
select @pBatchID = h.d.value('(./@BatchID)[1]','int')
      ,@pStepID = h.d.value('(./@StepID)[1]','int')
      ,@pConstID = h.d.value('(./@ConstID)[1]','int')
      ,@pRunID = h.d.value('(./@RunID)[1]','int')
      ,@pOptions = h.d.value('(./@Options)[1]','int')
      ,@pScope = h.d.value('(./@Scope)[1]','int')
  from @pHeader.nodes('(/etl:Header)') h(d)

end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pHeader = null
   raiserror (@msg,11,11)
end catch

RETURN @Err