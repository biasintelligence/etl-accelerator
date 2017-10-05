
create function [dbo].[fn_ETLColumn] (@DsvID int,@ObjName sysname)
returns @t table(tSchema sysname null,tName sysname null,cName sysname,cType sysname,cLen int,iSys tinyint,iPK tinyint,iSPK tinyint)
as begin

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
select o.tSchema,o.tName,o.cName,o.cType,o.cLen,o.iSys
      ,case when p.cName is null then 0 else 1 end as iPK
      ,0 as iSPK
from ( 
select
      xc.col.value('./@name','sysname') as [cName]
     ,isnull(xc.col.value('./@type','sysname'),xy.typ.value('./@base','sysname')) as [cType]
     ,xy.typ.value('./xs:maxLength[1]/@value','int') as [cLen]
     ,xc.col.value('contains(substring(./@name,1,3),"SA_")','bit') as [iSys]
     ,xt.tbl.value('./@name','sysname') as [xName]
     ,xt.tbl.value('./@msprop:DbSchemaName','sysname') as [tSchema]
     ,xt.tbl.value('./@msprop:DbTableName','sysname') as [tName]
from dbo.dsv t
cross apply t.dsv.nodes('(/DataSourceView/Schema/xs:schema/xs:element[@msdata:IsDataSet="true"]
/xs:complexType/xs:choice/xs:element[@name=(sql:variable("@ObjName"))])') xt(tbl)
cross apply xt.tbl.nodes('(./xs:complexType/xs:sequence/xs:element)') xc(col)
outer apply xc.col.nodes('(./xs:simpleType/xs:restriction)') xy(typ)
where DsvID = @DsvID) o
left join ( 
select
      xpk.col.value('./@xpath','sysname') as [cName]
     ,xu.pk.value('./xs:selector[1]/@xpath','sysname') as [xPath]
from dbo.dsv t
cross apply t.dsv.nodes('(/DataSourceView/Schema/xs:schema/xs:element[@msdata:IsDataSet="true"]
/xs:unique[@msdata:PrimaryKey="true"])') xu(pk)
cross apply xu.pk.nodes('(./xs:field)') xpk(col)
where DsvID = @DsvID
  and xu.pk.exist('./xs:selector[@xpath=concat(".//",(sql:variable("@ObjName")))]') = 1
) p on o.cName = p.cName

select top (1)
       @tname = tname
      ,@sname = tschema
  from @t

;with xmlnamespaces
 ('http://www.w3.org/2001/XMLSchema' as xsd
 ,'http://www.w3.org/2001/XMLSchema-instance' as xsi
 ,default 'http://schemas.microsoft.com/analysisservices/2003/engine'
 ,'http://www.w3.org/2001/XMLSchema' as xs
 ,'urn:schemas-microsoft-com:xml-msdata' as msdata
 ,'urn:schemas-microsoft-com:xml-msprop' as msprop
)
update t
set t.iSPK = 1
from @t t
join ( 
select
      xpk.col.value('./@xpath','sysname') as [cName]
     ,xu.pk.value('./xs:selector[1]/@xpath','sysname') as [xPath]
from dbo.dsv t
cross apply t.dsv.nodes('(/DataSourceView/Schema/xs:schema/xs:element[@msdata:IsDataSet="true"]
/xs:unique[@name=concat("UQ_SPK_",(sql:variable("@tname")))])') xu(pk)
cross apply xu.pk.nodes('(./xs:field)') xpk(col)
where DsvID = @DsvID
  and xu.pk.exist('./xs:selector[@xpath=concat(".//",(sql:variable("@ObjName")))]') = 1
) sp on t.cName = sp.cName


return
end