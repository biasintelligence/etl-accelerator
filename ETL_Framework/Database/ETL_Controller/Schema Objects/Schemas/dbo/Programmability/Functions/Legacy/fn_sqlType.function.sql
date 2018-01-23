--select dbo.fn_sqlType ('xs:unsignedByte')
create function [dbo].[fn_sqlType] (@xtype sysname)
   returns sysname
as
begin
-- dsv support
-- take .net type and return sql type instead
-- if not mapped original .net type is returned

   declare @type sysname
   declare @r sysname
   
   --remove namespace
   set @type = right(@xtype,len(@xtype)-charindex(':',@xtype))
   select top(1) @r = t.sType
     from (select 'int' as xtype, 'int' as stype
 union all select 'string','nvarchar'
 union all select 'dateTime','datetime'
 union all select 'long','bigint'
 union all select 'boolean','bit'
 union all select 'unsignedByte','tinyint'
 union all select 'Byte','tinyint'
 union all select 'short','smallint'
 union all select 'decimal','decimal'
 union all select 'double','float'
     ) t where t.xType = @type
   set @r = isnull(@r,@xtype)
   return (@r)
           
end