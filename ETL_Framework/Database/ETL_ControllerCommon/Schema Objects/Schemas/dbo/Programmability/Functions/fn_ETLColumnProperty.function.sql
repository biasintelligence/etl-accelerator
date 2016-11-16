/*
select * from [dbo].[fn_ETLColumn] (2,'dbo_AdType')
select * from [dbo].[fn_ETLColumnProperty] (2,'dbo_AdType','AdTypeId')
select * from [dbo].[fn_ETLColumnProperty] (3,'LastHourMB','ABTestName')
*/
create function [dbo].[fn_ETLColumnProperty] (@DsvID int,@ObjName sysname,@ColName sysname)
returns @t table(cName sysname,cValue nvarchar(max))
 as
begin
/******************************************************************************
** File:	[fn_ETLColumnProperty].sql
** Name:	[dbo].[fn_ETLColumnProperty]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Function/[fn_ETLColumnProperty].sql:

** Desc:	return  etl object column definition from dsv
**          
**
** Params:
** @DsvID -- dsv id
** @ObjName -- object name
** @ColName -- object name
** Returns:
**
** Author:	andreys
** Date:	10/11/2007
** ****************************************************************************
** CHANGE HISTORY
** ****************************************************************************
** Date				Author	version	4	#bug			Description
** ----------------------------------------------------------------------------------------------------------

*/

declare @tname sysname
declare @sname sysname

;with xmlnamespaces
 ('http://www.w3.org/2001/XMLSchema' as xsd
 ,'http://www.w3.org/2001/XMLSchema-instance' as xsi
 ,default 'http://schemas.microsoft.com/analysisservices/2003/engine'
 ,'http://www.w3.org/2001/XMLSchema' as xs
 ,'urn:schemas-microsoft-com:xml-msdata' as msdata
 ,'urn:schemas-microsoft-com:xml-msprop' as msprop
)
insert @t
select
      xp.prop.value('local-name(.)','sysname') as [oName]
     ,xp.prop.value('data(.)','nvarchar(max)') as [oValue]
from dbo.dsv t
cross apply t.dsv.nodes('(/DataSourceView/Schema/xs:schema/xs:element[@msdata:IsDataSet="true"]
/xs:complexType/xs:choice/xs:element[@name=(sql:variable("@ObjName"))])') xt(tbl)
cross apply xt.tbl.nodes('(./xs:complexType/xs:sequence/xs:element[@name=(sql:variable("@ColName"))])') xc(col)
cross apply xc.col.nodes('./@*') xp(prop)
where @DsvID = @DsvID

return
end