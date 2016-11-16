/*
declare @pHeader xml
declare @pProcessReceipt xml
exec dbo.prc_CreateHeader @pHeader out,1,null,null,4,10
select @pHeader
exec dbo.prc_CreateProcessReceipt @pProcessReceipt out,@pHeader,2,5000,null
select @pProcessReceipt
*/
CREATE PROCEDURE [dbo].[prc_CreateProcessReceipt]
    @pProcessReceipt xml([ETLController]) output
   ,@pHeader xml([ETLController])
   ,@pStatusID int
   ,@pErr int
   ,@pErrMsg nvarchar(max) = null
As
/******************************************************************
**D File:         prc_CreateProcessReceipt.SQL
**
**D Desc:         return ProcessReceipt object
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
SELECT @pProcessReceipt = @pHeader.query('declare namespace etl="ETLController.XSD";
  <etl:ProcessReceipt>
   {etl:Header}
   <etl:Status StatusID="{sql:variable("@pStatusID")}" Error="{sql:variable("@pErr")}">
    <etl:msg>{sql:variable("@pErrMsg")}</etl:msg>
   </etl:Status>
  </etl:ProcessReceipt>
')
end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pHeader = null
   raiserror (@msg,11,11)
end catch

RETURN @Err