/*
declare @pHeader xml
declare @pProcessInfo xml
exec dbo.etl_CreateHeader @pHeader out,1,null,null,4,1
select @pHeader
exec dbo.etl_CreateProcessInfo @pProcessInfo out,@pHeader,'xxx'
select @pProcessInfo
*/
CREATE PROCEDURE [dbo].[prc_CreateProcessInfo]
    @pProcessInfo xml([ETLController]) output
   ,@pHeader xml([ETLController])
   ,@pMsg nvarchar(max)
   ,@pErr int = null
As
/******************************************************************
**D File:         etl_CreateProcessInfo.SQL
**
**D Desc:         return ProcessInfo object
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
DECLARE @Options int
DECLARE @debug tinyint
DECLARE @now nvarchar(30)
DECLARE @Prefix nvarchar(100)
DECLARE @BatchID int
DECLARE @StepID int

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0
SET @pErr = ISNULL(@pErr,0)

begin try

exec dbo.[prc_ReadHeader] @pHeader,@BatchID out,@StepID out,null,null,@Options out
set @debug = NULLIF(@Options & 1,0)

IF (@debug IS NOT NULL)
BEGIN
   --SET @Now = CONVERT(NVARCHAR(30),getdate(),121)
   SET @Prefix = CAST(isnull(@BatchID,0) as nvarchar(10)) + '.' + CAST(isnull(@StepID,0) as nvarchar(10))
               + ':' /*+ @@SERVERNAME + '.' + DB_NAME()*/ + 'SP=' + CAST(@@SPID AS nvarchar(10))
               + ':' + 'Er=' + CAST(@pErr AS nvarchar(10))
   --SET @pMsg =  'DEBUG(' + @@SERVERNAME + '.' + DB_NAME() + ' ID=' + CAST(@@SPID AS nvarchar(100)) + ':' + @Now + ') ' + isnull(@pMsg,'null')
   SET @pMsg =  'DEBUG(' + @Prefix + ') ' + isnull(@pMsg,'null')
   SET @pMsg =  isnull(@pMsg,'null')
END



SELECT @pProcessInfo = @pHeader.query('declare namespace etl="ETLController.XSD";
  <etl:ProcessInfo>
   {etl:Header}
   <etl:Message Error="{sql:variable("@pErr")}">{sql:variable("@pMsg")}</etl:Message>
  </etl:ProcessInfo>
')
 
 
 
end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pHeader = null
   raiserror (@msg,11,11)
end catch

RETURN @Err