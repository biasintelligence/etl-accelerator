/*
declare @value nvarchar(max)
declare @RunID int
declare @pHeader xml
declare @pCounters xml
exec dbo.prc_CreateHeader @pHeader out,1,1,1,4,1
exec dbo.prc_CreateCounters @pCounters out,@pHeader
select @pCounters
exec dbo.prc_ReadCounter @pCounters,'test',@value out,@RunID out
select @value,@RunID
*/
CREATE PROCEDURE [dbo].[prc_ReadCounter]
    @pCounters xml([ETLController])
   ,@pName nvarchar(100) = null output
   ,@pValue nvarchar(max) = null output
   ,@pRunID int = null output
As
/******************************************************************
**D File:         prc_ReadCounter.SQL
**
**D Desc:         read Counter
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
  select @pValue = c.a.value('(.)[1]','nvarchar(max)')
        ,@pRunID = c.a.value('(./@RunID)[1]','int')
  from @pCounters.nodes('/etl:Counters/etl:Counter[@Name=(sql:variable("@pName"))]') c(a)
end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pValue = null
   raiserror (@msg,11,11)
end catch

RETURN @Err