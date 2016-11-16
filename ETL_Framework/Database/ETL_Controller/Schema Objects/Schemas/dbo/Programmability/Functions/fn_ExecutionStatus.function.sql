/*
select [dbo].[fn_ExecutionStatus] (20,null,null,null)
*/
create function [dbo].[fn_ExecutionStatus] (
    @pBatchID int
   ,@pStepID int
   ,@pRunID int
   ,@pConversation uniqueidentifier = NULL
)
returns bit
 as
begin
/******************************************************************************
** File:	[fn_ExecutionStatus].sql
** Name:	[dbo].[fn_ExecutionStatus]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema40/Function/[fn_ExecutionStatus].sql:

** Desc:	return  true if thread is running, false otherwise
**          
**
** Params:
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
   
   if not exists(select 1 from dbo.[ETLStepRun] with (nolock) where RunId = @pRunId
    and (BatchId = @pBatchId or isnull(@pBatchId,0) = 0)
    and (StepId = @pStepId or isnull(@pStepId,0) = 0))
      return 0
      
   if not exists (select 1 from sys.conversation_endpoints with (nolock)
    where (([conversation_id] = @pConversation AND  [state] = 'CO') or @pConversation is null))
      return 0
      
   return 1
end