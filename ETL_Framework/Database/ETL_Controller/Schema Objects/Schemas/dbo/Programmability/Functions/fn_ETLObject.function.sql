/*
select * from [fn_ETLObject]
select * from [dbo].[fn_ETLObject] (2)
select * from [dbo].[fn_ETLObject] (3)

*/
create function [dbo].[fn_ETLObject] (@DsvID int)
returns @t table(oName sysname,tSchema sysname null,tName sysname null,oType sysname null,isFile bit null)
 as
begin
/******************************************************************************
** File:	[fn_ETLObject].sql
** Name:	[dbo].[fn_ETLObject]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Function/[fn_ETLObject].sql:

** Desc:	return  etl object definition from dsv
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
      xt.tbl.value('./@name','sysname') as [oName]
     ,xt.tbl.value('./@msprop:DbSchemaName','sysname') as [tSchema]
     ,xt.tbl.value('./@msprop:DbTableName','sysname')  as [tName]
     ,xt.tbl.value('./@msprop:TableType','sysname') as [oType]
     ,case when xt.tbl.value('./@msprop:IsFileTable','sysname') = 'true' then 1 else 0 end as [isFile]
from dbo.dsv t
cross apply t.dsv.nodes('(/DataSourceView/Schema/xs:schema/xs:element[@msdata:IsDataSet="true"])
/xs:complexType/xs:choice/xs:element') xt(tbl)
where DsvID = @DsvID

return
end