/*
select * from [fn_filename]
select [dbo].[fn_filename] ('<date>','<hour>','test.txt')
*/
create function [dbo].[fn_filename] (
    @P1 nvarchar(25)
   ,@P2 nvarchar(10)
   ,@P3 nvarchar(100)
)
returns nvarchar(200)
 as
begin
/******************************************************************************
** File:	[fn_filename].sql
** Name:	[dbo].[fn_filename]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Function/[fn_filename].sql:

** Desc:	return  filename in format <@p1>_<@p2>_<@p3>
**          
**
** Params:
** @p1 and @p2 -if <date> then 'yyyymmdd' if <hour> then 'hh00'
** @P1      optional. 
** @P2      optional.
** @P3      required. filename.ext (flagfile.txt)
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

   declare @f nvarchar(200)
   declare @d datetime
   set @d = getdate()
   set @f = case when @p1 is null then ''
                 when @p1 = '<date>' then convert(nvarchar(8),@d,112)
                 when @p1 = '<hour>' then right('00' + cast(datepart(hh,@d) as nvarchar(2)),2) + '00'
                 else @p1
             end
   set @f = @f
          + case when len(@f) > 0 and @p2 is not null then '_' else '' end
          + case when @p2 is null then ''
                 when @p2 = '<date>' then convert(nvarchar(8),@d,112)
                 when @p2 = '<hour>' then right('00' + cast(datepart(hh,@d) as nvarchar(2)),2) + '00'
                 else @p2
             end
   set @f = @f
          + case when len(@f) > 0 and @p3 is not null then '_' else '' end
          + @p3

   return (@f)
end