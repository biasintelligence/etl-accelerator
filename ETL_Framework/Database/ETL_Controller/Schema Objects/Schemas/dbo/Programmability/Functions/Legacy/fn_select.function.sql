--select dbo.fn_select (1,'GeoData',null)
create function dbo.fn_select (@DsvID int,@tablename sysname,@list varchar(4000))
   returns varchar(max)
as
begin
     --dsv support
     --return select statement based on dsv definition of the table 
     --@tablename - tablename to generate select for excluding SA system columns 
     --@list      - static col list to add to the select statement


     declare @proc sysname
	 declare @sql varchar(max)
	 declare @columnlist varchar(max)
     declare @xname sysname
     set @columnlist = ''
     set @proc = object_name(@@procid)

     set @xname = isnull(parsename(@tablename,2) + '_','') + parsename(@tablename,1)
     select @sql = oValue from dbo.[fn_ETLObjectProperty](@DsvID,@xname)
     where oName = 'adCenter.DataQuery'
     if (@sql is null)
     begin
	    select @columnlist = @columnlist + ',' + quotename(cname)  
	      from dbo.[fn_ETLColumn](@DsvID,@xname) 
	     where cname not like 'SA[_]%' 
	     order by cName

        if len(@columnlist) > 0
        begin
	       set @columnlist = right(@columnlist, len(@columnlist)-1) -- remove leading comma  
           if (@list is not null)
           begin
              set @columnlist = @columnlist
                + case when (left(@list,1) = ',') then '' else ',' end
                + @list
           end
	       set @sql = 'select <columnlist> from <tablename>'
	       set @sql = replace(@sql, '<tablename>', @tablename)
	       set @sql = replace(@sql, '<columnlist>', @columnlist)
        end
     end
	 return @sql
end