--exec [prc_StagingTablePrepare] 'dbo.TestProperty','dbo.staging_TestProperty',0,'debug,rebuild,index'
create procedure [dbo].[prc_StagingTablePrepare] (
    @src           sysname
   ,@dst           sysname = null output
   ,@runid         int = null
   ,@options       varchar(255) = null
) as
begin
/******************************************************************************
** File:	[prc_StagingTablePrepare].sql
** Name:	[dbo].[prc_StagingTablePrepare]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Procedure/[prc_StagingTablePrepare].sql:

** Desc:	prepare staging table. Create one or tructate if present. Proc uses @src table as template
**          
**
** Params:
** @src       -- table or view name to use as template
** @dst       -- returns delta table name
** @options   -- supported: debug,rebuild,index,uniqueindex
** Returns:
**
** Author:	andreys
** Date:	12/5/2008
** ****************************************************************************
** CHANGE HISTORY
** ****************************************************************************
** Date				Author	version	4	#bug			Description
** ----------------------------------------------------------------------------------------------------------
   2012-03-08       andreys                             add extended property metadata
														table: hasMetadata = 1
														column: isSPK = ordinal(1,2,3) --source PK ordinal
															    isType2 = 1 -- type2 column
																isAudit = 1 -- audit column
																isType2StartPeriod -- Type2 start period column
																isType2EndPeriod -- Type2 end period column
																isAction = 1 -- action column

*/

   set nocount on

   declare @err                int
   declare @tran               int
   declare @proc               sysname
   declare @msg                nvarchar(1000)
   declare @query              nvarchar(max)
   declare @sql1               nvarchar(max)
   declare @debug              tinyint
   declare @rebuild            tinyint
   declare @needindex          tinyint
   declare @quotename          tinyint
   declare @srcDB              sysname
   declare @srcSchema          sysname

   declare @AuditCol           sysname
   declare @ActionCol          sysname
   declare @RecordStartCol     sysname
   declare @RecordEndCol       sysname

   set @err = 0
   set @tran = @@trancount
   set @debug = case when @options like '%debug%' then 1 else 0 end
   set @rebuild = case when @options like '%rebuild%' then 1 else 0 end
   set @needindex = case 
                      when @options like '%uniqueindex%' then 1
                      when @options like '%index%' then 2
                      else 0
                    end
   set @quotename = case when @options like '%quotename%' then 1 else 0 end

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
   set @srcDB = quotename(isnull(parsename(@src,3),DB_NAME()))
   set @srcSchema = quotename(isnull(parsename(@src,2),'dbo'))
   set @src = @srcDB+ '.' + @srcSchema+ '.' + quotename(parsename(@src,1))
   if (@quotename = 0)
   	   set @dst = isnull(parsename(isnull(@dst,@src),2),'dbo')
				+ '.' + isnull(parsename(@dst,1),'staging_' + parsename(@src,1))
	else
      set @dst = quotename(isnull(parsename(isnull(@dst,@src),2),'dbo'))
            + '.' + quotename(isnull(parsename(@dst,1),'staging_' + parsename(@src,1)))

  create table #srccol
  ([name] sysname primary key
  ,[type] smallint
  ,[len]  smallint
  ,[prec] tinyint
  ,[scale] tinyint
  ,[is_ident] bit
  ,[is_null] bit
  ,[is_guid] bit
  ,[colid] smallint
  ,[pk] tinyint
  ,[spk] tinyint
  ,[is_type2] bit
  )

  create table #srcprop
  ([colname] sysname
  ,[propname]  sysname
  ,[propvalue] nvarchar(30)
  )

begin try

   if (object_id(@src) is null)
   begin
      raiserror('ERROR: unknown object in %s',11,10,@src)
   end


-----------------------------------------------------------------------------------------------------
--build column def set
-----------------------------------------------------------------------------------------------------
   set @query = '
declare @tbl sysname,@sch sysname;
set @tbl = parsename(@src,1);
set @sch = parsename(@src,2);
if exists(select 1 from <db>.sys.fn_listextendedproperty (NULL, ''schema'', @sch, ''table'', @tbl, default, default) where name = ''hasMetadata'' and value = 1)
begin
--use Extended Properties
   insert #srcprop
   select objname, name, cast(value as nvarchar(30)) from <db>.sys.fn_listextendedproperty (NULL, ''schema'', @sch, ''table'', @tbl, ''column'', default);

   insert #srccol
   select d.[name],d.system_type_id,d.max_length,d.[precision],d.[scale]
       ,d.[is_identity],d.[is_nullable],d.[is_rowguidcol],d.[column_id]
       ,isnull(ipkc.[key_ordinal],0),isnull(spk.[propvalue],0),isnull(t2.[propvalue],0)
    from <db>.sys.columns d
    left join <db>.sys.indexes ipk on ipk.[object_id] = d.[object_id] and  ipk.[is_primary_key] = 1
    left join <db>.sys.index_columns ipkc
           on ipk.[object_id] = ipkc.[object_id]
          and ipk.[index_id] = ipkc.[index_id]
          and d.[column_id] = ipkc.[column_id]
    left join #srcprop spk on d.[name] = spk.[colname] and spk.[propname] = ''isSPK''
    left join #srcprop t2 on d.[name] = t2.[colname] and t2.[propname] = ''isType2''
    where d.[object_id] = object_id(@src)
end
else
begin
--use defaults
   insert #srccol
   select d.[name],d.system_type_id,d.max_length,d.[precision],d.[scale]
       ,d.[is_identity],d.[is_nullable],d.[is_rowguidcol],d.[column_id]
       ,isnull(ipkc.[key_ordinal],0),isnull(ispkc.[key_ordinal],0),isnull(ispkc.[is_included_column],0)
    from <db>.sys.columns d
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
    where d.[object_id] = object_id(@src)
end
'
    
   set @query = replace(@query,'<db>',@srcDB)
   --print @query
   exec sp_executesql @query,N'@src sysname',@src = @src

   --if (@debug = 1)
   --begin
   --   select * from #srccol
   --end

  set @AuditCol = isnull((select top 1 [colname] from #srcprop where [propname] = 'isAudit' and [propvalue] = 1)
                            ,(select top 1 [name] from #srccol where [name] = @AuditCol));
   set @ActionCol = isnull((select top 1 [colname] from #srcprop where [propname] = 'isAction' and [propvalue] = 1)
                            ,(select top 1 [name] from #srccol where [name] = @ActionCol));
   set @RecordStartCol = isnull((select top 1 [colname] from #srcprop where [propname] = 'isType2StartPeriod' and [propvalue] = 1)
                            ,(select top 1 [name] from #srccol where [name] = @RecordStartCol));
   set @RecordEndCol = isnull((select top 1 [colname] from #srcprop where [propname] = 'isType2EndPeriod' and [propvalue] = 1)
                            ,(select top 1 [name] from #srccol where [name] = @RecordEndCol));

   --remove dw specific columns
   delete #srccol where [name] in (@AuditCol,@ActionCol,@RecordStartCol,@RecordEndCol)
   --remove identity column
   delete #srccol where [is_ident] = 1

   --use spk as pk if provided
   if (exists(select 1 from #srccol where [spk] > 0))
   begin
      delete #srccol where [pk] > 0 and  [spk] = 0
      update #srccol set [pk] = [spk]
   end
   
   --remove pk requirement for staging
   if not exists(select 1 from #srccol where pk > 0)
   begin
      if (@needindex <> 0)
	  begin
         raiserror('WARNING: index can not be created. spk is required on %s',0,1,@src);
         set @needindex = 0;
	  end
      --raiserror('ERROR: pk is required in %s',11,10,@src)
   end
   if not exists(select 1 from #srccol)
   begin
      raiserror('ERROR: no source columns are found in %s',11,10,@src)
   end

-----------------------------------------------------------------------------------------------------
--check delta
-----------------------------------------------------------------------------------------------------
   if (@rebuild = 0)
   begin
      if object_id(@dst) is null
      begin
         set @rebuild = 1
      end
      else
      begin
         if exists(
            select 1 from (select d.[name] from sys.columns d where d.[object_id] = object_id(@dst)) d
              full join (select s.[name] from #srccol s) s
                on s.[name] = d.[name]
             where (s.[name] is null or d.name is null))
         begin
            exec ('drop table ' + @dst)
            set @rebuild = 1
         end
      end
   end
   else if object_id(@dst) is not null
   begin
      exec ('drop table ' + @dst)
   end


-----------------------------------------------------------------------------------------------------
--truncate or table create DDL
-----------------------------------------------------------------------------------------------------
   if (@rebuild = 0)
   begin
      set @query = 'truncate table ' + @dst + ';'
      if (@needIndex = 0 and exists(select 1 from sys.indexes where object_id = object_id(@dst) and name like 'uq_spk_%'))
         set @query = @query + 'drop index [uq_spk_' + parsename(@dst,1) + '] on ' + @dst + ';'
   end
   else
   begin
      set @query = 'create table ' + @dst + '(<columnlist>)'

      --<columnlist>
      set @sql1 = ''
      select @sql1 = @sql1 + ',' + quotename([name]) + ' ' + type_name([type])
                + case when [type] in (62) then '(' + cast(prec as nvarchar(10)) + ')'    
                       when [type] in (106,108) then '(' + cast(prec as nvarchar(10)) + ',' + cast(scale as nvarchar(10)) + ')'
                       when [type] in (165,167,173,175) then case when [len] = -1 then '(max)' else '(' + cast([len] as nvarchar(10)) + ')' end
                       when [type] in (231,239) then case when [len] = -1 then '(max)' else '(' + cast([len]/2 as nvarchar(10)) + ')' end
                       else '' 
                  end
                --+ case when ([pk] = 0) then ' null' else ' not null' end --+ char(13) + char(10)
                + case when ([is_null] = 0) then ' not null' else ' null' end --+ char(13) + char(10)
        from  #srccol
        order by [colid]

      set @sql1 = right(@sql1,len(@sql1) -1)
      set @query = replace(@query,'<columnlist>',@sql1)

   end

   if (@debug = 1)
   begin
      print @query
   end
   exec(@query)

 --create index for pre sorted input
   if (@rebuild = 1 and @needIndex > 0)
   begin
      set @query = 'create ' + case @needIndex when 1 then 'unique' else '' end + ' clustered index uq_spk_<tblname> on '
                 + @dst + '(<keycolumnlist>)'

      --<keycolumnlist>
      set @sql1 = ''
      select @sql1 = @sql1 + ',' + quotename([name])
        from  #srccol where [pk] > 0 and [is_type2] = 0
        order by [pk]

      set @sql1 = right(@sql1,len(@sql1) -1)
      set @query = replace(@query,'<keycolumnlist>',@sql1)
      set @query = replace(@query,'<tblname>',parsename(@dst,1))

      if (@debug = 1)
      begin
         print @query
      end
      exec(@query)
   end


end try
begin catch
   if @@trancount > @tran
      rollback tran

   set @dst = null
   set @Proc = ERROR_PROCEDURE()
   set @Msg = ERROR_MESSAGE()
   set @Err = ERROR_NUMBER()
   raiserror ('ERROR: PROC %s, MSG: %s',11,17,@Proc,@Msg) 
end catch

   return @err
end
go
