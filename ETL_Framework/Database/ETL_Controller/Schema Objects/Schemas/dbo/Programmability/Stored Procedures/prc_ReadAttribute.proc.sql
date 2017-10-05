/*
declare @value nvarchar(max)
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @pAttributes xml
exec dbo.prc_CreateHeader @pHeader out,101,1,1,4,1
exec dbo.prc_CreateContext @pContext out,@pHeader
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext
--select @pProcessRequest
exec dbo.prc_ReadContextAttributes @pProcessRequest,@pAttributes out
select @pAttributes
exec dbo.prc_ReadAttribute @pAttributes,'SQL',@value out
select @value
*/
CREATE PROCEDURE [dbo].[prc_ReadAttribute]
    @pAttributes xml([ETLController])
   ,@pName nvarchar(100) = null output
   ,@pValue nvarchar(max) = null output
As
/******************************************************************
**D File:         prc_ReadAttribute.SQL
**
**D Desc:         read Attribute
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
  set @pValue = @pAttributes.value('declare namespace etl="ETLController.XSD";
   (/etl:Attributes/etl:Attribute[@Name=(sql:variable("@pName"))])[1]','nvarchar(max)')
end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pValue = null
   raiserror (@msg,11,11)
end catch

RETURN @Err