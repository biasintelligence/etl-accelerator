create procedure [dbo].[prc_StagingTableUpload] (  
    @src      sysname  
   ,@dst      sysname
   ,@RunId    int = 0
   ,@options  varchar(255) = null  
) as  
begin  
/******************************************************************************  
** File: [prc_StagingTableUpload].sql  
** Name: [dbo].[prc_StagingTableUpload]  
  
** Desc: Delete and Insert data from src into dst table. dst must have primary key.   
**            
**  
** Params:  
** @src  -- fully qualified source table name. if database is not specified the db  
**               defaults to current database, if schema is not specified schema defaults to dbo  
** @dst  -- domain destination. must exists in current database. if schema is not specified schema defaults to dbo  
** @options   -- debug
                ,fullmerge --insert+update+softdelete
				,reload --truncate+insert
				,append --insert without check
				,checksum --update only changed records
				,appendnew --insert new only
** Returns:  
**  
** Author: andreys  
** Date: 11/05/2008  
** ****************************************************************************  
** CHANGE HISTORY  
** ****************************************************************************  
** Date        Author   version  #bug   Description
** ----------------------------------------------------------------------------------------------------------  
** 20011-12-29 andrey                  added type2 logic. Type2 columns are included columns in the UQ_SPK index
                                                         also RecordStartDate, RecordEndDate columns are required  
   2012-03-08       andreys            add extended property metadata
														table: hasMetadata = 1
														column: isSPK = ordinal(1,2,3) --source PK ordinal
															    isType2 = 1 -- type2 column
																isAudit = 1 -- audit column
																isAction = 1 -- action column
																isType2StartPeriod -- Type2 start period column
																isType2EndPeriod -- Type2 end period column
  2012-05-30		andrey				fix Soft Delete. After Soft Delete set ActionId to 1 - insert
  2013-05-13		andrey				check for type2 columns
  2013-05-21		andrey				move type2 filter from join to match clause
  2017-01-21		andrey				fix merge when table has only PK or SPK columns
  2017-03-13		andrey				comment audit calls + allow identity on pk
  2017-03-30		andrey				remove src/dst db if the same to support azure dbs 
  2019-01-08		andrey				add support for new types like geometry and colputed columns 
*/
--exec [prc_StagingTablePrepare] 'dbo.TestProperty','dbo.staging_TestProperty',0,'debug,rebuild,index'
  
   set nocount on  
  
   declare @err                int
   declare @tran               int
   declare @proc               sysname  
   declare @msg                nvarchar(1000)  
   declare @query              nvarchar(max)  
   declare @sql1               nvarchar(max)  
   declare @sql2               nvarchar(max)  
   declare @srcdb              sysname  
   declare @dstdb              sysname  
   declare @debug              bit
   declare @delete             bit
   declare @reload             bit
   declare @append             bit
   declare @appendnew      bit
   declare @check              bit
   declare @rows               int
   declare @is_tbl             tinyint
   declare @is_type2           bit

   declare @AuditCol           sysname
   declare @ActionCol          sysname
   declare @RecordStartCol     sysname
   declare @RecordEndCol       sysname

   set @proc = object_name(@@procid)  
   set @err = 0  
   set @tran = @@trancount
   set @debug = case when @options like '%debug%' then 1 else 0 end  
   set @delete = case when @options like '%fullmerge%' then 1 else 0 end
   set @reload = case when @options like '%reload%' then 1 else 0 end
   set @append = case when @options like '%append%' and @options not like '%appendnew%' then 1 else 0 end
   set @check = case when @options like '%checksum%' then 1 else 0 end
   set @appendnew = case when @options like '%appendnew%' then 1 else 0 end
                                                   
   set @RunId = ISNULL(@RunId,0)
   
-----------------------------------------------------------------------------------------------------
--metadata defaults
-----------------------------------------------------------------------------------------------------
   set @AuditCol = 'AuditId'
   set @ActionCol = 'ActionId'
   set @RecordStartCol = 'RecordStartDate'
   set @RecordEndCol = 'RecordEndDate'
-----------------------------------------------------------------------------------------------------  
--table name normalization  
-----------------------------------------------------------------------------------------------------  
   if (charindex('#',parsename(@src,1)) > 0)  
   begin  
      set @srcdb = quotename('tempdb') + '.';  
      set @src = quotename(parsename(@src,1))  
   end  
   else  
   begin  
      set @srcdb = isnull(parsename(@src,3),db_name())
      set @srcdb = case when @srcdb = db_name() then '' else quotename(@srcdb) + '.' end;

      set @src = @srcdb  
               + quotename(isnull(parsename(@src,2),'dbo')) + '.'
			   + quotename(parsename(@src,1));
   end  
  
   set @dstdb = isnull(parsename(@dst,3),db_name());
   set @dstdb = case when @dstdb = db_name() then '' else quotename(@dstdb) + '.' end;
   set @dst = @dstdb  
            + quotename(isnull(parsename(@dst,2),'dbo')) + '.'
			+ quotename(parsename(@dst,1))  
  
  
  create table #srccol  
  ([src_name] sysname  
  ,[type] smallint  
  ,[len]  smallint  
  ,[prec] tinyint  
  ,[scale] tinyint
  ,[spk] tinyint)  
  
  
  create table #dstcol  
  ([name] sysname  
  ,[src_name] sysname null  
  ,[type] smallint  
  ,[len]  smallint  
  ,[prec] tinyint  
  ,[scale] tinyint  
  ,[is_ident] bit  
  ,[is_null] bit  
  ,[is_guid] bit  
  ,[has_src] bit
  ,[is_add] bit
  ,[pk] tinyint 
  ,[spk] tinyint
  ,[is_type2] bit
  ,[use_hash] bit
  ,[is_computed] bit) 
  
  
  create table #dstprop
  ([colname] sysname
  ,[propname]  sysname
  ,[propvalue] nvarchar(30)
  )

begin try 
  
   set @is_tbl = 0;
   if (object_id(case when @srcdb = quotename('tempdb') + '.' then @srcdb + '.' else '' end + @src,'U') is not null)
      set @is_tbl = 1;
   else if (object_id(case when @srcdb = quotename('tempdb') + '.' then @srcdb + '.' else '' end + @src) is null)  
      raiserror('ERROR: unknown source table in %s',11,10,@src)  
     
   if (object_id(@dst) is null)  
   begin  
      raiserror('ERROR: unknown destination table in %s',11,10,@dst)  
   end  
  
-----------------------------------------------------------------------------------------------------  
--bring srcTable def over  
-----------------------------------------------------------------------------------------------------  
  
   set @query = '  
      insert #srccol  
      select s.[name],s.user_type_id,s.max_length,s.[precision],s.[scale],isnull(ispkc.[key_ordinal],0)  
        from ' + @srcdb + 'sys.columns s
    left join ' + @srcdb + 'sys.indexes ispk on ispk.[object_id] = s.[object_id] and  ispk.name like ''UQ_SPK_%''
    left join ' + @srcdb + 'sys.index_columns ispkc
           on ispk.[object_id] = ispkc.[object_id]
          and ispk.[index_id] = ispkc.[index_id]
          and s.[column_id] = ispkc.[column_id] 
       where s.[object_id] = object_id(''' + case when @srcdb = quotename('tempdb') + '.' then @srcdb + '.' else '' end + @src + ''')  
     '  
  
   --if (@debug = 1)  
   --begin  
   --   print @query  
   --end  
  
   exec(@query)  
  
   --if (@debug = 1)  
   --begin  
   --   select * from #srccol  
   --end  

-----------------------------------------------------------------------------------------------------  
--build column def set  
-----------------------------------------------------------------------------------------------------  
  set @query = '
declare @tbl sysname,@sch sysname;
set @tbl = parsename(@dst,1);
set @sch = parsename(@dst,2);
if exists(select 1 from <db>.sys.fn_listextendedproperty (NULL, ''schema'', @sch, ''table'', @tbl, default, default) where name = ''hasMetadata'' and value = 1)
begin
--use Extended Properties
   insert #dstprop
   select objname, name, cast(value as nvarchar(30)) from <db>.sys.fn_listextendedproperty (NULL, ''schema'', @sch, ''table'', @tbl, ''column'', default);

  insert #dstcol  
  select 
      [name]     = d.[name]
     ,[src_name] = s.[src_name]
     ,[type]     = d.user_type_id
     ,[len]      = d.max_length
     ,[prec]     = d.[precision]
     ,[scale]    = d.[scale]  
     ,[is_ident] = d.[is_identity]
     ,[is_null]  = d.[is_nullable]
     ,[is_guid]  = d.[is_rowguidcol]
     ,[has_src]  = case when s.[src_name] is null then 0 else 1 end
     ,[is_add]   = case when d.user_type_id in (48,52,56,59,60,62,106,108,122,127) then 1 else 0 end  
     ,isnull(ipkc.[key_ordinal],0)
     ,isnull(spk.[propvalue],0)
     ,isnull(t2.[propvalue],0)
     ,[use_hash]   = case when d.user_type_id in (129) then 1 else 0 end
	 ,[is_computed] = d.[is_computed]
    from <db>.sys.columns d  
    left join #srccol s on d.[name] = s.[src_name]  
    left join <db>.sys.indexes ipk on ipk.[object_id] = d.[object_id] and  ipk.[is_primary_key] = 1
    left join <db>.sys.index_columns ipkc
           on ipk.[object_id] = ipkc.[object_id]
          and ipk.[index_id] = ipkc.[index_id]
          and d.[column_id] = ipkc.[column_id]
    left join #dstprop spk on d.[name] = spk.[colname] and spk.[propname] = ''isSPK''
    left join #dstprop t2 on d.[name] = t2.[colname] and t2.[propname] = ''isType2''
    where d.[object_id] = object_id(@dst)

end
else
begin
--use defaults

  insert #dstcol  
  select 
      [name]     = d.[name]
     ,[src_name] = s.[src_name]
     ,[type]     = d.user_type_id
     ,[len]      = d.max_length
     ,[prec]     = d.[precision]
     ,[scale]    = d.[scale]  
     ,[is_ident] = d.[is_identity]
     ,[is_null]  = d.[is_nullable]
     ,[is_guid]  = d.[is_rowguidcol]
     ,[has_src]  = case when s.[src_name] is null then 0 else 1 end
     ,[is_add]   = case when d.user_type_id in (48,52,56,59,60,62,106,108,122,127) then 1 else 0 end  
     ,isnull(ipkc.[key_ordinal],0)
     ,isnull(ispkc.[key_ordinal],0)
     ,isnull(ispkc.[is_included_column],0)
     ,[use_hash]   = case when d.user_type_id in (129) then 1 else 0 end
	 ,[is_computed] = d.[is_computed]
    from <db>.sys.columns d  
    left join #srccol s on d.[name] = s.[src_name]  
    left join <db>.sys.indexes ipk on ipk.[object_id] = d.[object_id] and  ipk.[is_primary_key] = 1
    left join <db>.sys.index_columns ipkc
           on ipk.[object_id] = ipkc.[object_id]
          and ipk.[index_id] = ipkc.[index_id]
          and d.[column_id] = ipkc.[column_id]
    left join <db>.sys.indexes ispk on ispk.[object_id] = d.[object_id] and  ispk.name like ''UQ_SPK_%''
    left join <db>.sys.index_columns ispkc
           on ispk.[object_id] = ispkc.[object_id]
          and ispk.[index_id] = ispkc.[index_id]
          and d.[column_id] = ispkc.[column_id]
    where d.[object_id] = object_id(@dst)
end
'
  
   set @query = replace(@query,'<db>.',@dstdb)
   exec sp_executesql @query,N'@dst sysname',@dst = @dst

   --if (@debug = 1)
   --begin
   --   select * from #dstcol
   --end

   set @AuditCol = isnull((select top 1 [colname] from #dstprop where [propname] = 'isAudit' and [propvalue] = 1)
                            ,(select top 1 [name] from #dstcol where [name] = @AuditCol));
   set @ActionCol = isnull((select top 1 [colname] from #dstprop where [propname] = 'isAction' and [propvalue] = 1)
                            ,(select top 1 [name] from #dstcol where [name] = @ActionCol));
   set @RecordStartCol = isnull((select top 1 [colname] from #dstprop where [propname] = 'isType2StartPeriod' and [propvalue] = 1)
                            ,(select top 1 [name] from #dstcol where [name] = @RecordStartCol));
   set @RecordEndCol = isnull((select top 1 [colname] from #dstprop where [propname] = 'isType2EndPeriod' and [propvalue] = 1)
                            ,(select top 1 [name] from #dstcol where [name] = @RecordEndCol));

   set @is_type2 = 0;
   if (@RecordStartCol is not null and @RecordEndCol is not null)
   begin
      set @is_type2 = 1;
   end

   --remove dw specific columns
   delete #dstcol where [name] in (@AuditCol,@ActionCol,@RecordStartCol,@RecordEndCol)

if (@reload = 1 or @append = 1)
begin
-----------------------------------------------------------------------------------------------------  
--reload/append
-----------------------------------------------------------------------------------------------------  

   set @query =
 'declare @AuditId int,@Err int,@Rows int,@dt date;
set @dt = getdate();
begin try
if exists (select 1 from <src>)
begin
   --exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId out,@AuditMode = 1,@AuditObject = ''<dst>'',@Op = ''<op>'',@RunId = @RunID,@Options = @Options
'
+ case when @reload = 1 then ' truncate table <dst> ' else '' end
+ '  
  <IdentityOn>
   insert <dst>
   (<dstlist>)
   select <selectlist>
     from <src>

  set @rows = @@rowcount
  raiserror(''%d rows reloaded into <dst> table'',0,1,@rows) with nowait 
  <IdentityOff>

   --exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId,@AuditMode = 0,@RowCnt = @Rows,@Options = @Options
   --exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId,@AuditMode = 2,@Options = @Options

end  
end try  
begin catch
  <IdentityOff>
   declare @msg nvarchar(500)  
   set @msg = error_message()
   set @Err = error_number()
  
   --if (@AuditId is not null) 
   --   exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId,@AuditMode = 2,@Err = @Err,@Options = @Options
   raiserror (@msg,11,17) 
end catch  
     '  
end
else
begin

   --use spk as pk if provided
   if (not exists(select 1 from #dstcol where [spk] > 0))
   begin
      update #dstcol set [spk] = [pk]
   end

  
   if (not exists(select 1 from #dstcol where [spk] > 0))  
   begin  
      raiserror('ERROR: dst table %s must have spk in order to join to src table',11,10,@dst)  
   end  
   if (exists(select 1 from #dstcol where [spk] > 0 and [has_src] = 0))  
   begin  
      raiserror('ERROR: src table must have dst spk columns present',11,10,@dst)  
   end  
   if (exists(select 1 from #dstcol where [pk] = 0 and [is_ident] = 1))  
   begin  
      raiserror('ERROR: identity on non pk column is not supported',11,10,@dst)  
   end  

   if (@is_type2 = 0)
   begin
-----------------------------------------------------------------------------------------------------  
--type1 merge
-----------------------------------------------------------------------------------------------------  
   set @query =
 'declare @AuditId int,@Err int,@Rows int
begin try
if exists (select 1 from <src>)
begin
  --exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId out,@AuditMode = 1,@AuditObject = ''<dst>'',@Op = ''<op>'',@RunId = @RunID,@Options = @Options
  <performanceindex>

  <IdentityOn>
   merge <dst> as dst
   using (select <selectlist> from <src>) as src
         (<dstlist>)
      on (<joinlist>)
  --1  when matched <type1checksum> then 
  --1 update Set <updatelist>
       when not matched by target then 
      insert (<dstlist>)
      values (<dstlist>)
  --2  when not matched by source <delete>
  ;
--OUTPUT $action;

  set @rows = @@rowcount
  raiserror(''%d rows merged into <dst> table'',0,1,@rows) with nowait 
  <IdentityOff>

  --exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId,@AuditMode = 0,@RowCnt = @Rows,@Options = @Options
  --exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId,@AuditMode = 2,@Options = @Options

end  
end try  
begin catch  
  <IdentityOff>
   declare @msg nvarchar(500)  
   set @msg = error_message()
   set @Err = error_number()
  
   --if (@AuditId is not null) 
   --   exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId,@AuditMode = 2,@Err = @Err,@Options = @Options
   raiserror (@msg,11,17) 
end catch  
     '  
--<appendnew> option
--or dst only has pk/spk columns (no need for update)
   if (@appendnew = 0
   and exists (select 1 from #dstcol where spk = 0 and has_src = 1))
      set @query = replace(@query,'--1','')


--<fullmerge> option
--<delete>
   if (@delete = 1)
   begin
      set @query = replace(@query,'--2','');
	  if (@ActionCol is null)
         set @query = replace(@query,'<delete>','then delete'); --hard delete if action col is not defined
	  else
	  begin
	     set @sql1 = ' and dst.' +  @ActionCol + ' <> 3 then update set dst.' +  @ActionCol + ' = 3' + case when @AuditCol is not null then ',dst.' +  @AuditCol + ' = @AuditId' else '' end;
		 set @query = replace(@query,'<delete>',@sql1);
	  end
   end


   end
   else
   begin
-----------------------------------------------------------------------------------------------------  
--type2 merge
-----------------------------------------------------------------------------------------------------  
--if type2 columns are not defined set all none key columns to type2
   if (not exists(select 1 from #dstcol where [is_type2] = 1))  
   begin  
      update #dstcol set [is_type2] = 1 where [spk] = 0;
      SET @rows = @@ROWCOUNT;
      if (@rows = 0)
		raiserror('ERROR: no type2 columns found',11,10)  
   end

   set @query =
 'declare @AuditId int,@Err int,@Rows int,@tran int,@dt date;
 set @tran = @@TRANCOUNT;
 set @dt = getdate();
begin try
if exists (select 1 from <src>)
begin
  --exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId out,@AuditMode = 1,@AuditObject = ''<dst>'',@Op = ''<op>'',@RunId = @RunID,@Options = @Options
  <performanceindex>

   begin tran;
   merge <dst> as dst
   using (select <selectlist> from <src>) as src
         (<dstlist>)
      on (<joinlist>)
    when matched <type2checksum>
	and (dst.' + @RecordEndCol + ' is null and dst.' + @RecordStartCol + ' < @dt) then 
  update Set dst.' + @RecordEndCol + ' = @dt;

  set @rows = @@rowcount
  raiserror(''%d rows t2 closed recorded merged into <dst> table'',0,1,@rows) with nowait 

   merge <dst> as dst
   using (select <selectlist> from <src>) as src
         (<dstlist>)
      on (<joinlist>)
	 and (dst.' + @RecordEndCol + ' is null)
--1	when matched <type1checksum> then
--1  update Set <updatelist>
       when not matched by target then 
      insert (<dstlist>)
      values (<dstlist>);

  set @rows = @rows + @@rowcount
  raiserror(''%d rows t1 merged into <dst> table'',0,1,@rows) with nowait 

  commit tran;

  --exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId,@AuditMode = 0,@RowCnt = @Rows,@Options = @Options
  --exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId,@AuditMode = 2,@Options = @Options

end  
end try  
begin catch

   if  (@tran < @@TRANCOUNT) 
      rollback tran;

   declare @msg nvarchar(500)  
   set @msg = error_message()
   set @Err = error_number()
  
   --if (@AuditId is not null) 
   --   exec <dstdb>.dbo.prc_Audit @AuditId = @AuditId,@AuditMode = 2,@Err = @Err,@Options = @Options
   raiserror (@msg,11,17) 
end catch  
     '  

--dst only has pk/spk columns (no need for update)
   if exists (select 1 from #dstcol where spk = 0 and has_src = 1)
      set @query = replace(@query,'--1','')


--<type2checksum>
   set @sql1 = ''
   set @sql2 = ''
   select @sql1 = @sql1 +
				+ case when [use_hash] = 1 then ',hashbytes(''MD5'',cast(src.' + quotename([src_name]) + ' as varbinary(max)))' else ',src.' + quotename([src_name]) end 
       ,@sql2 = @sql2
				+ case when [use_hash] = 1 then ',hashbytes(''MD5'',cast(dst.' + quotename([src_name]) + ' as varbinary(max)))' else ',dst.' + quotename([src_name]) end 
    from  #dstcol where [has_src] = 1 and [is_type2] = 1 and [is_computed] = 0
      
   if (len(@sql1) > 0)
   begin
	   set @sql1 = right(@sql1,len(@sql1) -1)
	   set @sql2 = right(@sql2,len(@sql2) -1)
	   --set @sql1 = ' and binary_checksum(' + @sql1 + ') <> binary_checksum(' + @sql2 + ')'
	   set @sql1 = ' and not exists (select ' + @sql1 + ' intersect select ' + @sql2 + ')'
	   set @query = replace(@query,'<type2checksum>',@sql1)
   end

   end



--<performanceindex>
   set @sql1 = ''
   if (@is_tbl = 1 and not exists(select 1 from #srccol where [spk] > 0))
   begin
      select @sql1 = @sql1 + ',' + quotename([src_name])
        from  #dstcol where [spk] > 0 and [is_type2] = 0
        order by [spk]
      
	  select @sql1 = 'create unique clustered index [uq_spk_' + parsename(@src,1) + '] on <src>(' + right(@sql1,len(@sql1) -1) +')'
   end

   set @query = replace(@query,'<performanceindex>',@sql1)


--<type1checksum> option
   set @sql1 = ''
   set @sql2 = ''
   if (@check = 1)
   begin
   select @sql1 = @sql1 +
				+ case when [use_hash] = 1 then ',hashbytes(''MD5'',cast(src.' + quotename([src_name]) + ' as varbinary(max)))' else ',src.' + quotename([src_name]) end 
       ,@sql2 = @sql2
				+ case when [use_hash] = 1 then ',hashbytes(''MD5'',cast(dst.' + quotename([src_name]) + ' as varbinary(max)))' else ',dst.' + quotename([src_name]) end 
      from  #dstcol where [has_src] = 1 and [spk] = 0  and [is_computed] = 0 --and [is_type2] = 0
      
      if (len(@sql1) > 0)
	  begin
		  set @sql1 = right(@sql1,len(@sql1) -1)
		  set @sql2 = right(@sql2,len(@sql2) -1)
		  --set @sql1 = ' and (binary_checksum(' + @sql1 + ') <> binary_checksum(' + @sql2 + ')'
		  set @sql1 = ' and (not exists (select ' + @sql1 + ' intersect select ' + @sql2 + ')'
					+ case when @ActionCol is not null then ' or dst.' + @ActionCol + ' = 3' else '' end + ')'
      end          
   end
   set @query = replace(@query,'<type1checksum>',@sql1)

--<joinlist>
   set @sql1 = ''
   select @sql1 = @sql1 + ' and src.' + quotename([name]) + ' = dst.' + quotename([src_name])
     from  #dstcol where [spk] > 0 and [is_type2] = 0

   set @sql1 = right(@sql1,len(@sql1) -4)
   set @query = replace(@query,'<joinlist>',@sql1)


--<updatelist>
   set @sql1 = case when @AuditCol is not null then ',dst.' + @AuditCol + ' = @AuditId' else '' end
             + case when @ActionCol is not null then ',dst.' + @ActionCol + ' = case dst.' + @ActionCol + ' when 3 then 1 else 2 end' else '' end  --insert if deleted else update
   select @sql1 = @sql1 + ',dst.' + quotename([name]) + ' = src.' + quotename([src_name]) 
         ,@sql2 = @sql2 + ',' + quotename([name])  
   from  #dstcol where [has_src] = 1 and [spk] = 0  and [is_computed] = 0 --and [is_type2] = 0

   
   if (len(@sql1) > 0)
   begin
	   set @sql1 = right(@sql1,len(@sql1) -1)
	   set @query = replace(@query,'<updatelist>',@sql1)
   end
end

--<selectlist>
--<dstlist>
set @sql1 = case when @AuditCol is not null then ',@AuditId' else '' end
          + case when @ActionCol is not null then ',1' else '' end  --insert
          + case when @is_type2 = 1 then ',@dt,null' else '' end 
set @sql2 = case when @AuditCol is not null then ',' + @AuditCol else '' end
          + case when @ActionCol is not null then ',' + @ActionCol else '' end
          + case when @is_type2 = 1 then ',' + @RecordStartCol + ',' + @RecordEndCol else '' end
select @sql1 = @sql1 + ',' + quotename([src_name])  
      ,@sql2 = @sql2 + ',' + quotename([name])  
from  #dstcol where [has_src] = 1 and [is_computed] = 0

set @sql1 = right(@sql1, len(@sql1)-1) -- remove leading comma
set @sql2 = right(@sql2, len(@sql2)-1) -- remove leading comma  

set @query = replace(@query, '<selectlist>', @sql1)  
set @query = replace(@query, '<dstlist>', @sql2)  

--<Identity>
if exists(select 1 from  #dstcol where [is_ident] = 1 and [has_src] = 1) -- and [pk] = 0
begin
   set @query = replace(@query, '<IdentityOn>', 'set identity_insert <dst> on;')  
   set @query = replace(@query, '<IdentityOff>', 'set identity_insert <dst> off;')  
end
else
begin
   set @query = replace(@query, '<IdentityOn>', '')  
   set @query = replace(@query, '<IdentityOff>', '')  
end

--<src>  
--<dst>  
--<dstdb>  
set @query = replace(@query, '<dst>', @dst)  
set @query = replace(@query, '<src>', @src)  
set @query = replace(@query, '<dstdb>.', @dstdb)  
  
--<op>
set @sql1 = case when @reload = 1 then 'RELOAD'
               when @append = 1 then 'APPEND'
			   when @is_type2 = 1 then 'MERGE_T2'
               else 'MERGE_T1'
			 end
set @query = replace(@query, '<op>', @sql1)  

if (@debug = 1)  
begin  
  print @query  
end                 

exec sp_executesql @query,N'@RunID int,@Options varchar(255)',@RunID = @RunID,@Options = @Options

end try  
begin catch  
   if @@trancount > @tran  
      rollback tran  
  
   set @Proc = ERROR_PROCEDURE()  
   set @Msg = ERROR_MESSAGE()  
   set @Err = ERROR_NUMBER()  
   raiserror ('ERROR: PROC %s, MSG: %s',11,17,@Proc,@Msg)   
end catch  
  
   return @err  
end
;