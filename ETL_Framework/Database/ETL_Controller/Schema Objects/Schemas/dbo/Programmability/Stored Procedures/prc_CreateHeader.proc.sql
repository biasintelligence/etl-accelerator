/*
declare @pHeader xml
exec dbo.prc_CreateHeader @pHeader out,1,null,null,4,10
select @pHeader
*/
CREATE PROCEDURE [dbo].[prc_CreateHeader]
    @pHeader xml([ETLController]) output
   ,@pBatchID int
   ,@pStepID int = null
   ,@pConstID int = null
   ,@pRunID int
   ,@pOptions int = null
   ,@pScope int = null
As
/******************************************************************
**D File:         prc_CreateHeader.SQL
**
**D Desc:         return Header object
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
SET @pHeader =
  '<etl:Header xmlns:etl="ETLController.XSD"'
+ ' BatchID="' + CAST(@pBatchID AS nvarchar(30)) + '"'
+ CASE WHEN @pStepID IS NOT NULL THEN ' StepID="' + CAST(@pStepID AS nvarchar(30)) + '"' ELSE '' END
+ CASE WHEN @pConstID IS NOT NULL THEN N' ConstID="' + CAST(@pConstID AS nvarchar(30)) + '"' ELSE '' END
+ ' RunID="' + CAST(@pRunID AS nvarchar(30)) + '"'
+ CASE WHEN @pOptions IS NOT NULL THEN N' Options="' + CAST(@pOptions AS nvarchar(30)) + '"' ELSE '' END
+ CASE WHEN @pScope IS NOT NULL THEN N' Scope="' + CAST(@pScope AS nvarchar(30)) + '"' ELSE '' END
+ ' />'
end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pHeader = null
   raiserror (@msg,11,11)
end catch

RETURN @Err