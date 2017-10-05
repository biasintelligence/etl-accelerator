/*
select * from [dbo].[fn_ETLObjectProperty] (2,'dbo_vKeywordOrder')
select * from [dbo].[fn_ETLObjectProperty] (3,'LogFileMR')
*/
create function [dbo].[fn_ETLObjectProperty] (@DsvID int,@ObjName sysname)
returns @t table(oName sysname,oValue nvarchar(max))
 as
begin
/******************************************************************************
** File:	[fn_ETLObjectProperty].sql
** Name:	[dbo].[fn_ETLObjectProperty]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Function/[fn_ETLObjectProperty].sql:

** Desc:	return  etl object properties from dsv
**          
**
** Params:
** @DsvID -- dsv id
** @ObjName -- object name
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
cross apply xt.tbl.nodes('./@*') xp(prop)
where DsvID = @DsvID

return
end