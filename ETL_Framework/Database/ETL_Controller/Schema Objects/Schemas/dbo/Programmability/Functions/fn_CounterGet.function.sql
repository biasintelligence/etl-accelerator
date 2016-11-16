/*
select * from ETLStepRunCounter
select [dbo].[fn_CounterGet] (20,0,0,'ExProcessID')
*/
create function [dbo].[fn_CounterGet] (
    @BatchID int
   ,@StepID int
   ,@RunID int
   ,@CounterName nvarchar(100)
)
returns nvarchar(1000)
 as
begin
/******************************************************************************
** File:	[fn_CounterGet].sql
** Name:	[dbo].[fn_CounterGet]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Function/fn_CounterGet.sql:

** Desc:	return  user defined counter value for batch/step/run combination
**          
**
** Params:
** @Request      --xml containing @RunID/StepID/BatchID
** Returns:
**
** Author:	andreys
** Date:	08/01/2007
** ****************************************************************************
** CHANGE HISTORY
** ****************************************************************************
** Date				Author	version	4	#bug			Description
** ----------------------------------------------------------------------------------------------------------

*/

   return (select top(1) CounterValue
             from dbo.[ETLStepRunCounter]
            where BatchID = @BatchID and StepID = @StepID and RunID = @RunID and CounterName = @CounterName)
end