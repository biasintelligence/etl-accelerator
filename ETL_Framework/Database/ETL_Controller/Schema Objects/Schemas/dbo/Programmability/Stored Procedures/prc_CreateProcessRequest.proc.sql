/*
declare @uid uniqueidentifier
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
set @uid = newid()
exec dbo.prc_CreateHeader @pHeader out,-10,null,null,4,10
--exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext,@uid
select @pHeader
select @pContext
select @pProcessRequest
*/
CREATE PROCEDURE [dbo].[prc_CreateProcessRequest]
    @pProcessRequest xml([ETLController]) output
   ,@pHeader xml([ETLController])
   ,@pContext xml([ETLController])
   ,@pConversation uniqueidentifier = null
   ,@pConversationGrp uniqueidentifier = null
As
/******************************************************************
**D File:         prc_CreateProcessRequest.SQL
**
**D Desc:         return ProcessRequest object
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
DECLARE @x xml
DECLARE @nHandle nvarchar(36)
DECLARE @nHandleGrp nvarchar(36)
DECLARE @pr nvarchar(max)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0

set @nHandle= isnull(cast(@pConversation as nvarchar(36)),'')
set @nHandleGrp= isnull(cast(@pConversationGrp as nvarchar(36)),'')

begin try
set @pr = '
  <etl:ProcessRequest xmlns:etl="ETLController.XSD">
   <etl:Header/>
   <etl:SrcConversation>' + @nHandle + '</etl:SrcConversation>
   <etl:SrcConversationGrp>' + @nHandleGrp + '</etl:SrcConversationGrp>
   <etl:DstConversation/>
   <etl:DstConversationGrp/>
   <etl:Context/>
   </etl:ProcessRequest>
'
set @pr = replace(@pr,'<etl:Header/>',cast(@pHeader as nvarchar(max)))

set @pr = replace(@pr,'<etl:Context/>',isnull(cast(@pContext as nvarchar(max)),''))

set @pProcessRequest = @pr

end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pHeader = null
   raiserror (@msg,11,11)
end catch

RETURN @Err