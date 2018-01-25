/*
select * from ETLStepAttribute
select [dbo].fn_AttributeGet (-10,1,null,'StepName')
*/
create function [dbo].[fn_ETLAttributeGet] (
    @BatchID int
   ,@StepID int
   ,@ConstID int
   ,@AttributeName nvarchar(100)
)
returns nvarchar(4000)
 as
begin
/******************************************************************************
** File:	[fn_AttributeGet].sql
** Name:	[dbo].[fn_AttributeGet]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema/Function/[fn_AttributeGet].sql:

** Desc:	return  user defined attribute value for batch/step/const combination
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
   declare @v nvarchar(4000)

   if (@ConstId is not null and @StepID is null)
      if (@AttributeName = 'ConstOrder')
         select @v = ConstOrder from dbo.[ETLBatchConstraint] where BatchID = @BatchID and ConstId = @ConstID;
      else if (@AttributeName = 'WaitPeriod')
         select @v = WaitPeriod from dbo.[ETLBatchConstraint] where BatchID = @BatchID and ConstId = @ConstID;
      else 
         select @v = AttributeValue
           from dbo.[ETLBatchConstraintAttribute]
          where BatchID = @BatchID and ConstId = @ConstID and AttributeName = @AttributeName;
   else if (@ConstId is not null and @StepID is not null)
      if (@AttributeName = 'ConstOrder')
         select @v = ConstOrder from dbo.[ETLStepConstraint] where BatchID = @BatchID and StepID = @StepID and ConstId = @ConstID;
      else if (@AttributeName = 'WaitPeriod')
         select @v = WaitPeriod from dbo.[ETLStepConstraint] where BatchID = @BatchID and StepID = @StepID and ConstId = @ConstID;
      else 
         select @v = AttributeValue
           from dbo.[ETLStepConstraintAttribute]
          where BatchID = @BatchID and StepID = @StepID and ConstId = @ConstID and AttributeName = @AttributeName
   else if (@StepID is not null)
       if (@AttributeName = 'StepName')
         select @v = StepName from dbo.[ETLStep] where BatchID = @BatchID and StepID = @StepID;
       else if (@AttributeName = 'StepDesc')
         select @v = StepDesc from dbo.[ETLStep] where BatchID = @BatchID and StepID = @StepID;
       else if (@AttributeName = 'IgnoreErr')
         select @v = IgnoreErr from dbo.[ETLStep] where BatchID = @BatchID and StepID = @StepID;
       else if (@AttributeName = 'StepOrder')
         select @v = StepOrder from dbo.[ETLStep] where BatchID = @BatchID and StepID = @StepID;
       else
         select @v = AttributeValue
           from dbo.[ETLStepAttribute]
          where BatchID = @BatchID and StepID = @StepID and AttributeName = @AttributeName
   else 
       if (@AttributeName = 'BatchName')
         select @v = BatchName from dbo.[ETLBatch] where BatchID = @BatchID;
       else if (@AttributeName = 'BatchDesc')
         select @v = BatchDesc from dbo.[ETLBatch] where BatchID = @BatchID;
       else if (@AttributeName = 'IgnoreErr')
         select @v = IgnoreErr from dbo.[ETLBatch] where BatchID = @BatchID;
       else if (@AttributeName = 'RestartOnErr')
         select @v = RestartOnErr from dbo.[ETLBatch] where BatchID = @BatchID;
       else
         select @v = AttributeValue
           from dbo.[ETLBatchAttribute]
          where BatchID = @BatchID and AttributeName = @AttributeName

   return (@v)
end