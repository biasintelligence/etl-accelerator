/*
declare @uid uniqueidentifier
declare @uid1 uniqueidentifier
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
set @uid = newid()
exec dbo.prc_CreateHeader @pHeader out,-10,null,null,4,10
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext,@uid
select @pProcessRequest
exec dbo.prc_ReadProcessRequest @pProcessRequest,@pHeader out,@pContext out,@uid1 out
select @pHeader
select @pContext
select @uid1
*/
CREATE PROCEDURE [dbo].[prc_ReadProcessRequest]
    @pProcessRequest xml([ETLController])
   ,@pHeader xml([ETLController]) = null output
   ,@pContext xml([ETLController]) = null output
   ,@pConversation uniqueidentifier = null output
   ,@pConversationGrp uniqueidentifier = null output
As
/******************************************************************
**D File:         prc_ReadProcessRequest.SQL
**
**D Desc:         read Request object
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
declare @nHandle nvarchar(36)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0

begin try
SELECT @pHeader = @pProcessRequest.query('declare namespace etl="ETLController.XSD";(/etl:ProcessRequest/etl:Header)[1]')
SELECT @pContext = @pProcessRequest.query('declare namespace etl="ETLController.XSD";(/etl:ProcessRequest/etl:Context)[1]')
SET @nHandle = @pProcessRequest.value('declare namespace etl="ETLController.XSD";(/etl:ProcessRequest/etl:SrcConversation)[1]','nvarchar(36)')
if len(@nHandle) = 36
   set @pConversation = cast(@nHandle as uniqueidentifier)
SET @nHandle = @pProcessRequest.value('declare namespace etl="ETLController.XSD";(/etl:ProcessRequest/etl:SrcConversationGrp)[1]','nvarchar(36)')
if len(@nHandle) = 36
   set @pConversationGrp = cast(@nHandle as uniqueidentifier)

end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pHeader = null
   raiserror (@msg,11,11)
end catch

RETURN @Err