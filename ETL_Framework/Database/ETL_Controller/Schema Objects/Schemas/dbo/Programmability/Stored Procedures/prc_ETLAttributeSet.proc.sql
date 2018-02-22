
/*
exec [dbo].[prc_ETLAttributeSet] -10,-5,null,'RestartOnErr','1'
select * from ETLBatch
*/
create procedure [dbo].[prc_ETLAttributeSet] (
    @BatchID int
   ,@StepID int = null
   ,@ConstID int = null
   ,@AttributeName nvarchar(100)
   ,@AttributeValue nvarchar(4000)
)
 as
begin
/******************************************************************************
** File:	[prc_ETLAttributeSet].sql
** Name:	[dbo].[prc_ETLAttributeSet]

** SD Location: VSS/Development/SubjectAreas/BI/Database/Schema40/Procedure/[prc_ETLAttributeSet].sql:

** Desc:	set  user defined attribute value for batch/step/const combination
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
** 2012-01-09       andreys                             validate inpits
** 2018-02-22       andrey                              etl: prefix
*/
set nocount on
declare @msg nvarchar(max)
declare @Err int
declare @proc sysname

set @Err = 0
set @proc = OBJECT_NAME(@@procid)

declare @shortName nvarchar(100);
declare @fullName nvarchar(100);
declare @sys nvarchar(4) = 'etl:'

begin try

   if (not exists(select 1 from dbo.ETLBatch where BatchID = @BatchID))
      raiserror('Invalid Parameter b=%d',11,11,@BatchID);


   if (left(@attributeName,4) = @sys)
      set @shortName = right(@attributeName,len(@attributeName) - 4);
   else
      set @shortName = @attributeName;

	set @fullName = @sys + @shortName;

   if (@ConstId is not null and @StepID is null)
   begin
      if (not exists(select 1 from dbo.ETLBatchConstraint where BatchID = @BatchID and ConstId = @ConstId))
         raiserror('Invalid Parameter bc=%d.%d',11,11,@BatchID,@ConstID);
  --columns
      if (@shortName = 'ConstOrder')
         update dbo.[ETLBatchConstraint]
            set ConstOrder = @AttributeValue
          where BatchID = @BatchID and ConstId = @ConstID
      else if (@shortName = 'WaitPeriod')
         update dbo.[ETLBatchConstraint]
            set ConstOrder = @AttributeValue
          where BatchID = @BatchID and ConstId = @ConstID
   --attributes 
      else if exists(select 1 from dbo.[ETLBatchConstraintAttribute]
                 where BatchID = @BatchID and ConstId = @ConstID and AttributeName in (@shortName,@fullName))
         if (@AttributeValue is null)
            delete dbo.[ETLBatchConstraintAttribute]
             where BatchID = @BatchID and ConstId = @ConstID and AttributeName in (@shortName,@fullName)
         else
            update dbo.[ETLBatchConstraintAttribute]
               set AttributeValue = @AttributeValue
             where BatchID = @BatchID and ConstId = @ConstID and AttributeName in (@shortName,@fullName)
      else if (@AttributeValue is not null)
         insert dbo.[ETLBatchConstraintAttribute] (BatchID,ConstID,AttributeName,AttributeValue)
         values (@BatchID,@ConstID,@AttributeName,@AttributeValue)
   end
   else if (@ConstId is not null and @StepID is not null)
   begin
      if (not exists(select 1 from dbo.ETLStepConstraint where BatchID = @BatchID and StepID = @StepID and ConstID = @ConstID))
         raiserror('Invalid Parameter  for the bsc= %d.%d.%d',11,11,@BatchID,@StepID,@ConstID);
   --columns
      if (@shortName = 'ConstOrder')
         update dbo.[ETLStepConstraint]
            set ConstOrder = @AttributeValue
          where BatchID = @BatchID and StepID = @StepID and ConstId = @ConstID
      else if (@shortName = 'WaitPeriod')
         update dbo.[ETLStepConstraint]
            set ConstOrder = @AttributeValue
          where BatchID = @BatchID and StepID = @StepID and ConstId = @ConstID
   --attributes 
     else if exists(select 1 from dbo.[ETLStepConstraintAttribute]
                 where BatchID = @BatchID and StepID = @StepID and ConstId = @ConstID and AttributeName in (@shortName,@fullName))
         if (@AttributeValue is null)
            delete dbo.[ETLStepConstraintAttribute]
             where BatchID = @BatchID and StepID = @StepID  and ConstId = @ConstID and AttributeName in (@shortName,@fullName)
         else
            update dbo.[ETLStepConstraintAttribute]
               set AttributeValue = @AttributeValue
             where BatchID = @BatchID and StepID = @StepID  and ConstId = @ConstID and AttributeName in (@shortName,@fullName)
      else if (@AttributeValue is not null)
         insert dbo.[ETLStepConstraintAttribute] (BatchID,StepID,ConstID,AttributeName,AttributeValue)
         values (@BatchID,@StepID,@ConstID,@AttributeName,@AttributeValue)
   end
   else if (@StepID is not null)
   begin
      if (not exists(select 1 from dbo.ETLStep where BatchID = @BatchID and StepID = @StepID))
         raiserror('Invalid Parameter  for the bs= %d.%d',11,11,@BatchID,@StepID);
    --columns
      if (@shortName = 'StepName')
         update dbo.[ETLStep]
            set StepName = @AttributeValue
          where BatchID = @BatchID and StepID = @StepID
      else if (@shortName = 'StepDesc')
         update dbo.[ETLStep]
            set StepDesc = @AttributeValue
          where BatchID = @BatchID and StepID = @StepID
      else if (@shortName = 'IgnoreErr')
         update dbo.[ETLStep]
            set IgnoreErr = @AttributeValue
          where BatchID = @BatchID and StepID = @StepID
      else if (@shortName = 'StepOrder')
         update dbo.[ETLStep]
            set StepOrder = @AttributeValue
          where BatchID = @BatchID and StepID = @StepID
   --attributes 
      else if exists(select 1 from dbo.[ETLStepAttribute]
                 where BatchID = @BatchID and StepId = @StepID and AttributeName in (@shortName,@fullName))
         if (@AttributeValue is null)
            delete dbo.[ETLStepAttribute]
             where BatchID = @BatchID and StepID = @StepID and AttributeName in (@shortName,@fullName)
         else
            update dbo.[ETLStepAttribute]
               set AttributeValue = @AttributeValue
             where BatchID = @BatchID and StepId = @StepID and AttributeName in (@shortName,@fullName)
      else if (@AttributeValue is not null)
         insert dbo.[ETLStepAttribute] (BatchID,StepID,AttributeName,AttributeValue)
         values (@BatchID,@StepID,@AttributeName,@AttributeValue)
   end
   else
   begin
    --columns
      if (@shortName = 'BatchName')
         update dbo.[ETLBatch]
            set BatchName = @AttributeValue
          where BatchID = @BatchID
      else if (@shortName = 'BatchDesc')
         update dbo.[ETLBatch]
            set BatchDesc = @AttributeValue
          where BatchID = @BatchID
      else if (@shortName = 'IgnoreErr')
         update dbo.[ETLBatch]
            set IgnoreErr = @AttributeValue
          where BatchID = @BatchID
      else if (@shortName = 'RestartOnErr')
         update dbo.[ETLBatch]
            set RestartOnErr = @AttributeValue
          where BatchID = @BatchID
   --attributes 
      else if exists(select 1 from dbo.[ETLBatchAttribute]
                 where BatchID = @BatchID and AttributeName  in (@shortName,@fullName))
         if (@AttributeValue is null)
            delete dbo.[ETLBatchAttribute]
             where BatchID = @BatchID and AttributeName in (@shortName,@fullName)
         else
            update dbo.[ETLBatchAttribute]
               set AttributeValue = @AttributeValue
             where BatchID = @BatchID and AttributeName in (@shortName,@fullName)
      else if (@AttributeValue is not null)
         insert dbo.[ETLBatchAttribute] (BatchID,AttributeName,AttributeValue)
         values (@BatchID,@AttributeName,@AttributeValue)
   end

end try
begin catch	
	if @@trancount > 0 rollback tran
	
   set @Proc = ERROR_PROCEDURE()
   set @Msg = ERROR_MESSAGE()
   raiserror ('ERROR: PROC %s, MSG: %s',11,17,@Proc,@Msg) 
   set @Err = ERROR_NUMBER()
end catch
   return 0
end