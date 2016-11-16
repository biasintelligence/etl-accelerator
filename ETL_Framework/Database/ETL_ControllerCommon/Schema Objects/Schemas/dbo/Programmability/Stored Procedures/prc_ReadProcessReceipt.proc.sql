/*
declare @s int,@e int,@m nvarchar(max)
declare @pHeader xml
declare @pHeader1 xml
declare @pProcessReceipt xml
exec dbo.prc_CreateHeader @pHeader out,1,null,null,4,10
select @pHeader
exec dbo.prc_CreateProcessReceipt @pProcessReceipt out,@pHeader,2,5000,'xxx'
select @pProcessReceipt
exec dbo.prc_ReadProcessReceipt @pProcessReceipt,@pHeader1 out,@s out,@e out,@m out
select @s,@e,@m
select @pHeader1
*/
CREATE PROCEDURE [dbo].[prc_ReadProcessReceipt]
    @pProcessReceipt xml([ETLController])
   ,@pHeader xml([ETLController]) = null output
   ,@pStatusID int = null output
   ,@pErr int = null output
   ,@pErrMsg nvarchar(max) = null output
As
/******************************************************************
**D File:         prc_ReadProcessReceipt.SQL
**
**D Desc:         read Receipt object
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
SELECT @pHeader = @pProcessReceipt.query('declare namespace etl="ETLController.XSD";(/etl:ProcessReceipt/etl:Header)[1]')
SET @pStatusID = @pProcessReceipt.value('declare namespace etl="ETLController.XSD";(/etl:ProcessReceipt/etl:Status/@StatusID)[1]','int')
SET @pErr = @pProcessReceipt.value('declare namespace etl="ETLController.XSD";(/etl:ProcessReceipt/etl:Status/@Error)[1]','int')
SET @pErrMsg = @pProcessReceipt.value('declare namespace etl="ETLController.XSD";(/etl:ProcessReceipt/etl:Status/etl:msg)[1]','nvarchar(max)')
end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pHeader = null
   raiserror (@msg,11,11)
end catch

RETURN @Err