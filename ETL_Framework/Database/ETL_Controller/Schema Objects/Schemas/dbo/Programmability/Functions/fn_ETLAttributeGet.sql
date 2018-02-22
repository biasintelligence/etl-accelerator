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
** 2018-02-22       andrey                              etl: prefix

*/
	declare @v nvarchar(4000)
	declare @shortName nvarchar(100);
	declare @fullName nvarchar(100);
	declare @sys nvarchar(4) = 'etl:'

   if (left(@attributeName,4) = @sys)
      set @shortName = right(@attributeName,len(@attributeName) - 4);
   else
      set @shortName = @attributeName;

	set @fullName = @sys + @shortName;


   if (@ConstId is not null and @StepID is null)
      if (@shortName = 'ConstOrder')
         select top 1 @v = ConstOrder from dbo.[ETLBatchConstraint] where BatchID = @BatchID and ConstId = @ConstID;
      else if (@shortName = 'WaitPeriod')
         select top 1 @v = WaitPeriod from dbo.[ETLBatchConstraint] where BatchID = @BatchID and ConstId = @ConstID;
      else 
         select top 1 @v = AttributeValue
           from dbo.[ETLBatchConstraintAttribute]
          where BatchID = @BatchID and ConstId = @ConstID and AttributeName in (@shortName,@fullName);
   else if (@ConstId is not null and @StepID is not null)
      if (@shortName = 'ConstOrder')
         select top 1 @v = ConstOrder from dbo.[ETLStepConstraint] where BatchID = @BatchID and StepID = @StepID and ConstId = @ConstID;
      else if (@shortName = 'WaitPeriod')
         select top 1 @v = WaitPeriod from dbo.[ETLStepConstraint] where BatchID = @BatchID and StepID = @StepID and ConstId = @ConstID;
      else 
         select top 1 @v = AttributeValue
           from dbo.[ETLStepConstraintAttribute]
          where BatchID = @BatchID and StepID = @StepID and ConstId = @ConstID and AttributeName  in (@shortName,@fullName)
   else if (@StepID is not null)
       if (@shortName = 'StepName')
         select top 1 @v = StepName from dbo.[ETLStep] where BatchID = @BatchID and StepID = @StepID;
       else if (@shortName = 'StepDesc')
         select top 1 @v = StepDesc from dbo.[ETLStep] where BatchID = @BatchID and StepID = @StepID;
       else if (@shortName = 'IgnoreErr')
         select top 1 @v = IgnoreErr from dbo.[ETLStep] where BatchID = @BatchID and StepID = @StepID;
       else if (@shortName = 'StepOrder')
         select top 1 @v = StepOrder from dbo.[ETLStep] where BatchID = @BatchID and StepID = @StepID;
       else
         select top 1 @v = AttributeValue
           from dbo.[ETLStepAttribute]
          where BatchID = @BatchID and StepID = @StepID and AttributeName  in (@shortName,@fullName)
   else 
       if (@shortName = 'BatchName')
         select top 1 @v = BatchName from dbo.[ETLBatch] where BatchID = @BatchID;
       else if (@shortName = 'BatchDesc')
         select top 1 @v = BatchDesc from dbo.[ETLBatch] where BatchID = @BatchID;
       else if (@shortName = 'IgnoreErr')
         select top 1 @v = IgnoreErr from dbo.[ETLBatch] where BatchID = @BatchID;
       else if (@shortName = 'RestartOnErr')
         select top 1 @v = RestartOnErr from dbo.[ETLBatch] where BatchID = @BatchID;
       else
         select top 1 @v = AttributeValue
           from dbo.[ETLBatchAttribute]
          where BatchID = @BatchID and AttributeName  in (@shortName,@fullName)

   return (@v)
end