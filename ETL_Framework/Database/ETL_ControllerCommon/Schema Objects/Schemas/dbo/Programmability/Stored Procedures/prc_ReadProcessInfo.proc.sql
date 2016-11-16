/*
declare @m nvarchar(max)
declare @e int
declare @pHeader xml
declare @pHeader1 xml
declare @pProcessInfo xml
exec dbo.prc_CreateHeader @pHeader out,1,null,null,4,10
select @pHeader
exec dbo.prc_CreateProcessInfo @pProcessInfo out,@pHeader,'xxx',10
select @pProcessInfo
exec dbo.prc_ReadProcessInfo @pProcessInfo,@pHeader1 out,@m out,@e out
select @e,@m
select @pHeader1
*/
CREATE PROCEDURE [dbo].[prc_ReadProcessInfo]
    @pProcessInfo xml([ETLController])
   ,@pHeader xml([ETLController]) = null output
   ,@pMsg nvarchar(max) = null output
   ,@pErr int = null output
As
/******************************************************************
**D File:         prc_ReadProcessInfo.SQL
**
**D Desc:         read Info object
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
SELECT @pHeader = @pProcessInfo.query('declare namespace etl="ETLController.XSD";(/etl:ProcessInfo/etl:Header)[1]')
 ;WITH XMLNAMESPACES ('ETLController.XSD' as etl)
SELECT @pMsg = p.m.value('string(.)[1]','nvarchar(max)')
      ,@pErr = p.m.value('(./@Error)[1]','nvarchar(max)')
  FROM @pProcessInfo.nodes('/etl:ProcessInfo/etl:Message') as p(m)
end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pHeader = null
   raiserror (@msg,11,11)
end catch

RETURN @Err