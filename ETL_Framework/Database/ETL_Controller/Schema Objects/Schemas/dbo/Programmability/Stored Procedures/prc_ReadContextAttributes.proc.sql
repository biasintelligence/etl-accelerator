/*
declare @pHeader xml
declare @pContext xml
declare @pProcessRequest xml
declare @pAttributes xml
exec dbo.prc_CreateHeader @pHeader out,301,2,null,4,15
exec dbo.prc_CreateContext @pContext out,@pHeader
select @pcontext
exec dbo.prc_CreateProcessRequest @pProcessRequest out,@pHeader,@pContext
select @pProcessRequest
exec dbo.prc_ReadContextAttributes @pProcessRequest,@pAttributes out
select @pAttributes
*/
CREATE PROCEDURE [dbo].[prc_ReadContextAttributes]
    @pProcessRequest xml([ETLController])
   ,@pAttributes xml([ETLController]) = null output
As
/******************************************************************
**D File:         prc_ReadContextAttributes.SQL
**
**D Desc:         create Attributes object for a Context
**
**D Auth:         andreys
**D Date:         10/27/2007
**
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
**  05/21/2008       andreys            add etl namespace to system attributes
******************************************************************/
SET NOCOUNT ON
DECLARE @Err INT
DECLARE @ProcErr INT
DECLARE @Cnt INT
DECLARE @ProcName sysname
DECLARE @msg nvarchar(max)

DECLARE @BatchID int
DECLARE @StepID int
DECLARE @ConstID int
DECLARE @RunID int
DECLARE @Options int
DECLARE @debug tinyint
DECLARE @Handle uniqueidentifier

declare @Name nvarchar(100)
declare @Value1 nvarchar(max)
declare @Value2 nvarchar(max)
declare @nValue nvarchar(max)
declare @id int

DECLARE @Header xml(ETLController)
DECLARE @Context xml(ETLController)
DECLARE @ProcessInfo xml(ETLController)

SET @ProcName = OBJECT_NAME(@@PROCID)
SET @Err = 0
SET @ProcErr = 0

declare @attr table(id int identity(1,1),AttributeName nvarchar(100),AttributeValue nvarchar(max) null,CompleteFlag tinyint null)
declare @ab table (aid int,bid int,isexec tinyint)

begin try
exec @ProcErr = dbo.[prc_ReadProcessRequest] @pProcessRequest,@Header out,@Context out,@Handle out
exec @ProcErr = dbo.[prc_ReadHeader] @Header,@BatchID out,@StepID out,@ConstID out,@RunID out,@Options out

set @debug = nullif(@Options & 1,0)
IF (@debug IS NOT NULL)
BEGIN
   SET @msg = 'BEGIN Procedure ' + @ProcName
                           + ' with @BatchID=' + CAST(@BatchID AS nvarchar(30))
                           + ISNULL( ', @StepID=' +CAST(@StepID AS nvarchar(30)),'')
                           + ISNULL( ', @ConstID=' +CAST(@ConstID AS nvarchar(30)),'')

   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END

if (@ConstID is not null and @StepID is null)
begin
   ;with xmlnamespaces('ETLController.XSD' as etl)
   insert @attr (AttributeName,AttributeValue)
   select cba.ba.value('@Name[1]','nvarchar(100)'),cba.ba.value('(.)[1]','nvarchar(max)')
     from @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]
/etl:Constraints/etl:Constraint[@ConstID=(sql:variable("@ConstID"))]/etl:Attributes') cb(b)
   cross apply cb.b.nodes('./etl:Attribute') cba(ba)

--system batch constraint attributes
   ;with xmlnamespaces('ETLController.XSD' as etl)
   insert @attr (AttributeName,AttributeValue)
   select cba.ba.value('concat("etl:",local-name(.))','nvarchar(100)'),cba.ba.value('string(.)','nvarchar(max)')
     from @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]
/etl:Constraints/etl:Constraint[@ConstID=(sql:variable("@ConstID"))]') cb(b)
   cross apply cb.b.nodes('./@*') cba(ba)
   left join @attr a on a.AttributeName = cba.ba.value('concat("etl:",local-name(.))','nvarchar(100)')
   where a.AttributeName is null

end
else if (@ConstID is not null)
begin
   ;with xmlnamespaces('ETLController.XSD' as etl)
   insert @attr (AttributeName,AttributeValue)
   select cba.ba.value('@Name[1]','nvarchar(100)'),cba.ba.value('(.)[1]','nvarchar(max)')
     from @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]
/etl:Steps/etl:Step[@StepID=(sql:variable("@StepID"))]
/etl:Constraints/etl:Constraint[@ConstID=(sql:variable("@ConstID"))]/etl:Attributes') cb(b)
   cross apply cb.b.nodes('./etl:Attribute') cba(ba)

--system step constraint attributes
   ;with xmlnamespaces('ETLController.XSD' as etl)
   insert @attr (AttributeName,AttributeValue)
   select cba.ba.value('concat("etl:",local-name(.))','nvarchar(100)'),cba.ba.value('string(.)','nvarchar(max)')
     from @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]
/etl:Steps/etl:Step[@StepID=(sql:variable("@StepID"))]
/etl:Constraints/etl:Constraint[@ConstID=(sql:variable("@ConstID"))]') cb(b)
   cross apply cb.b.nodes('./@*') cba(ba)
   left join @attr a on a.AttributeName = cba.ba.value('concat("etl:",local-name(.))','nvarchar(100)')
   where a.AttributeName is null
end

if(@StepID is not null)
begin
   ;with xmlnamespaces('ETLController.XSD' as etl)
   insert @attr (AttributeName,AttributeValue)
   select cba.ba.value('@Name[1]','nvarchar(100)'),cba.ba.value('(.)[1]','nvarchar(max)')
     from @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]
/etl:Steps/etl:Step[@StepID=(sql:variable("@StepID"))]/etl:Attributes') cb(b)
   cross apply cb.b.nodes('./etl:Attribute') cba(ba)
   left join @attr a on a.AttributeName = cba.ba.value('@Name[1]','nvarchar(100)')
   where a.AttributeName is null

--system step attributes
   ;with xmlnamespaces('ETLController.XSD' as etl)
   insert @attr (AttributeName,AttributeValue)
   select cba.ba.value('concat("etl:",local-name(.))','nvarchar(100)'),cba.ba.value('string(.)','nvarchar(max)')
     from @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]
/etl:Steps/etl:Step[@StepID=(sql:variable("@StepID"))]') cb(b)
   cross apply cb.b.nodes('./@*') cba(ba)
   left join @attr a on a.AttributeName = cba.ba.value('concat("etl:",local-name(.))','nvarchar(100)')
   where a.AttributeName is null

end

;with xmlnamespaces('ETLController.XSD' as etl)
insert @attr (AttributeName,AttributeValue)
select cba.ba.value('@Name[1]','nvarchar(100)'),cba.ba.value('(.)[1]','nvarchar(max)')
  from @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]/etl:Attributes') cb(b)
cross apply cb.b.nodes('./etl:Attribute') cba(ba)
left join @attr a on a.AttributeName = cba.ba.value('@Name[1]','nvarchar(100)')
 where a.AttributeName is null

--system batch attributes
;with xmlnamespaces('ETLController.XSD' as etl)
insert @attr (AttributeName,AttributeValue)
select cba.ba.value('concat("etl:",local-name(.))','nvarchar(100)'),cba.ba.value('string(.)','nvarchar(max)')
  from @Context.nodes('/etl:Context[@BatchID=(sql:variable("@BatchID"))]') cb(b)
cross apply cb.b.nodes('./@*') cba(ba)
left join @attr a on a.AttributeName = cba.ba.value('concat("etl:",local-name(.))','nvarchar(100)')
where a.AttributeName is null

declare @inputoptions nvarchar(1000)
set @inputoptions = case when @options & 1 = 1 then 'debug' else '' end
+ case when @options & 2 = 2 then ',forcestart' else '' end

--legacy system attributes
insert @attr (AttributeName,AttributeValue)
select '@BatchID',cast(@BatchID as nvarchar(100))
union select '@StepID',cast(@StepID as nvarchar(100))
union select '@ConstID',cast(@ConstID as nvarchar(100))
union select '@RunID',cast(@RunID as nvarchar(100))
union select '@Options',cast(@inputoptions as nvarchar(100))
union select '@Handle',cast(@handle as nvarchar(100))
union select 'etl:RunID',cast(@RunID as nvarchar(100))


-------------------------------------------------------------------
--relationship table
-------------------------------------------------------------------
insert @ab
select a.id,b.id,0
  from @attr a
  join @attr b on charindex('<' + a.AttributeName + '>',b.AttributeValue) > 0
union all select a.id,b.id,1
  from @attr a
  join @attr b on charindex('<' + a.AttributeName + '*>',b.AttributeValue) > 0

-------------------------------------------------------------------
--replace dynamic parameters if any
-------------------------------------------------------------------
WHILE (1=1)
BEGIN
    SET @id = null
    SELECT top(1) @id = t.id
                 ,@Name = t.AttributeName
                 ,@Value1 = t.AttributeValue
      FROM @attr t
      WHERE t.CompleteFlag IS NULL
        AND NOT EXISTS(SELECT 1 FROM @ab t1
                         JOIN @attr t2 ON t1.aid = t2.id WHERE  t.id = t1.bid AND t2.CompleteFlag IS NULL) --no parents
        AND EXISTS(SELECT 1 FROM @ab t2 WHERE  t.id = t2.aid) --have children
      order by t.id

    IF (@id is null)
       BREAK


    update a set a.AttributeValue = REPLACE(a.AttributeValue,'<' + @Name + '>',isnull(@Value1,''))
      from @attr a
      join @ab ab on a.id = ab.bid and ab.aid = @id and ab.isexec = 0


   --execute value to get the value
   IF (@Value1 is not null
       AND EXISTS(SELECT 1 FROM @ab ab WHERE ab.aid = @id and ab.isexec = 1))
   BEGIN
      SET @nValue = 'select top 1 @value = (' + @Value1 + ')'

	  IF (@debug IS NOT NULL)
	  BEGIN
		  SET @msg = 'Evaluate attribute: ' + @Name + ' = ' + @Value1
		  exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
		  exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
	  END

      SET @Value2 = null
      EXEC sp_executesql @nValue,N'@value varchar(max) output',@value = @value2 out

      update a set a.AttributeValue = REPLACE(a.AttributeValue,'<' + @Name + '*>',isnull(@Value2,''))
        from @attr a
        join @ab ab on a.id = ab.bid and ab.aid = @id and ab.isexec = 1
   END


   update @attr set CompleteFlag = 1 where id = @id
END


;with xmlnamespaces('ETLController.XSD' as etl)
select @pAttributes = 
 (select a.AttributeName as '@Name',a.AttributeValue as '*' from @attr a
     for xml path('etl:Attribute'),root('etl:Attributes'),type)

if @pAttributes is null
   set @pAttributes = '<etl:Attributes xmlns:etl="ETLController.XSD" />'

IF (@debug IS NOT NULL)
BEGIN
   SET @msg = 'END Procedure ' + @ProcName
   exec @ProcErr = dbo.[prc_CreateProcessInfo] @ProcessInfo out,@Header,@msg
   exec @ProcErr = dbo.[prc_Print] @ProcessInfo,@handle
END

end try
begin catch
   set @msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   set @pAttributes = null
   raiserror (@msg,11,11)
end catch

RETURN @Err