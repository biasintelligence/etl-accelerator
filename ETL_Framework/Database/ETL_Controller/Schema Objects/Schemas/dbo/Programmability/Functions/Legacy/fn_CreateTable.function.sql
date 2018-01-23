--select dbo.fn_CreateTable (2,'dbo.OrderItem')
--select * from dbo.fn_ETLColumn(2,'dbo_AdType')
create function [dbo].[fn_CreateTable] (@DsvID int,@tablename sysname)
   returns varchar(max)
as
begin
     --dsv support
     --return table create script based on dsv definition
     --@dsvid - dsv version
     --@tablename - tablename to generate create for

     declare @proc sysname
	 declare @sql varchar(max)
	 declare @columnlist varchar(max)
     declare @xname sysname
     set @columnlist = ''
     set @proc = object_name(@@procid)

     set @xname = isnull(parsename(@tablename,2),'dbo') + '_' + parsename(@tablename,1)
	 select @columnlist = @columnlist + ',' + quotename(cname) + ' ' + dbo.fn_SqlType(ctype)
                        + case when cLen is not null then '(' + cast(cLen as nvarchar(10)) + ')' else '' end
                        + ' ' + ' null'
	   from dbo.[fn_ETLColumn](@DsvID,@xname) 

     if len(@columnlist) > 0
     begin
	    set @columnlist = right(@columnlist, len(@columnlist)-1) -- remove leading comma  
	    set @sql = 'create table <tablename> (<columnlist>)'
	    set @sql = replace(@sql, '<tablename>', @tablename)
	    set @sql = replace(@sql, '<columnlist>', @columnlist)
     end
	 return @sql
end