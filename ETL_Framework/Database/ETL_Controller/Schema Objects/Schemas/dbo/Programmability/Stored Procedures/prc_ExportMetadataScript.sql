create procedure [dbo].[prc_ExportMetadataScript]
 @BatchName nvarchar(100) = null
,@BatchId int = null
,@Script nvarchar(max) = null out
as
/******************************************************************************
** File:	[prc_ExportMetadataScript].sql
** Name:	[dbo].[prc_ExportMetadataScript]

** Desc:	export workflow metadata to sql script
**          
**
** Params:
** Returns:
**
** Author:	andrey
** Date:	11/27/2016
** ****************************************************************************
** CHANGE HISTORY
** ****************************************************************************
** Date				Author	version	4	#bug			Description
** ----------------------------------------------------------------------------------------------------------
declare @script nvarchar(max);
exec prc_ExportMetadataScript @batchId = 100,@script = @script out
print substring (@Script,1,4000)
print substring (@Script,4001,8000)
print substring (@Script,8001,12000)
print substring (@Script,12001,16000)

*/
set nocount on;

set @script = '';
declare @sql nvarchar(max);

begin try

declare @msg nvarchar(2000);
if (@BatchName is null and @BatchId is null)
begin
	set @msg = 'Invalid input parameters. BatchId or BatchName are required';
	throw 50001,@msg,1; 
end


declare @bid int, @batchDesc nvarchar(1000);
select top 1
	 @bid = batchId
 from dbo.ETLBatch
where batchId = isnull(@batchId,batchId)
  and batchName = isnull(@batchName,batchName);

if (@bid is null)
begin
	set @msg = 'Invalid input parameters. BatchId: ' + isnull(cast(@batchId as nvarchar(30)),'null')
			 + ' or BatchName:' + isnull(@BatchName,'null');
	throw 50002,@msg,1; 
end

select top 1
	 @batchId = batchId
	,@BatchName = batchName
	,@BatchDesc = batchDesc
from dbo.ETLBatch
where batchId = @bid;


set @Script = '
---------------------------------------------------------
--' + @batchDesc + '
---------------------------------------------------------
set quoted_identIfier on;
set nocount on;
Declare @Batchid int,@BatchName nvarchar(100) ;
set @BatchID = ' + cast(@batchId as nvarchar(30)) + ';
set @BatchName = ''' + @batchName + ''';

print '' Compiling '' + @BatchName;

begin try
-------------------------------------------------------
--remove Workflow metadata
-------------------------------------------------------
exec dbo.prc_RemoveContext @BatchName;
'

set @sql = (select
	     + '''' + isnull(batchDesc,@batchName) + ''','
		 + isnull(cast(OnSuccessID as nvarchar(10)),'null') + ','
		 + isnull(cast(OnFailureID as nvarchar(10)),'null') + ','
		 + isnull(cast(IgnoreErr as nvarchar(10)),'0') + ','
		 + isnull(cast(RestartOnErr as nvarchar(10)),'0')
from dbo.ETLBatch where batchId = @batchId);

set @Script += '

-------------------------------------------------------
--create workflow record 
-------------------------------------------------------
--set identity_insert dbo.ETLBatch on;
insert dbo.ETLBatch
(BatchID,BatchName,BatchDesc,OnSuccessID,OnFailureID,IgnoreErr,RestartOnErr)
values (@BatchID,@BatchName,' + @sql + ');
--set identity_insert dbo.ETLBatch off;
'
--system attributes
set @sql = '';
select @sql += ',(@batchId,''' + ba.attributeName + ''',''' + replace(ba.attributeValue,'''','''''') + ''')
'
from dbo.ETLBatchAttribute ba
where ba.batchId = @batchId
and  ba.AttributeName in ('HISTRET','MAXTHREAD','PING','TIMEOUT','LIFETIME','RETRY','DELAY');

if (len(@sql) > 0)
set @Script += '
-------------------------------------------------------
--Define workflow level system attributes
--those attributes can be referenced in wf body like <etl:MaxThread> etc.
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
values
 ' + right(@sql,len(@sql) - 1) + ';';


--user attributes
set @sql = '';
select @sql += ',(@batchId,''' + ba.attributeName + ''',''' + replace(ba.attributeValue,'''','''''') + ''')
'
from dbo.ETLBatchAttribute ba
where ba.batchId = @batchId
and  ba.AttributeName not in ('HISTRET','MAXTHREAD','PING','TIMEOUT','LIFETIME','RETRY','DELAY');

if (len(@sql) > 0)
set @Script += '
-------------------------------------------------------
--Define workflow level user attributes
-- use systemparameters to store global configuration parameters
-- select * from systemparameters
-------------------------------------------------------
insert dbo.ETLBatchAttribute
(BatchID,AttributeName,AttributeValue)
values
 ' + right(@sql,len(@sql) - 1) + ';';


if exists (select 1 from dbo.ETLBatchConstraint where batchId = @batchId)
begin

set @Script += '
-------------------------------------------------------
--create batch level constraints
-------------------------------------------------------

'
set @sql = '';
select @sql += ',(@batchId,'
		 + cast(constId as nvarchar(10)) + ','
		 + isnull(cast(ProcessId as nvarchar(10)),'null') + ','
		 + isnull('''' + ConstOrder + '''','null') + ','
		 + isnull(cast(WaitPeriod as nvarchar(10)),'0') + ')'
from dbo.ETLBatchConstraint where batchId = @batchId
order by constId;


if (len(@sql) > 0)
set @Script += '
--set identity_insert dbo.ETLBatchConstraint on
insert dbo.ETLBatchConstraint
(BatchID,ConstID,ProcessID,ConstOrder,WaitPeriod)
values
' + right(@sql,len(@sql) - 1) + ';
--set identity_insert dbo.ETLBatchConstraint off;
';

--system attributes
set @sql = '';
select @sql += ',(@batchId,' + cast(bca.ConstId as nvarchar(10)) + ',''' + bca.attributeName + ''',''' + replace(bca.attributeValue,'''','''''') + ''')
'
from dbo.ETLBatchConstraintAttribute bca
where bca.batchId = @batchId
and  bca.AttributeName in ('DISABLED','PING')
order by bca.constId;

if (len(@sql) > 0)
begin
set @Script += '
-------------------------------------------------------
--Define workflow constraint level system attributes
-------------------------------------------------------
insert dbo.ETLBatchConstraintAttribute
(BatchID,ConstId,AttributeName,AttributeValue)
values
 ' + right(@sql,len(@sql) - 1) + ';';

--user attributes
set @sql = '';
select @sql += ',(@batchId,' + cast(bca.ConstId as nvarchar(10)) + ',''' + bca.attributeName + ''',''' + replace(bca.attributeValue,'''','''''') + ''')
'
from dbo.ETLBatchConstraintAttribute bca
where bca.batchId = @batchId
and  bca.AttributeName not in ('DISABLED','PING')
order by bca.constId;


set @Script += '

-------------------------------------------------------
--Define workflow constraint level user attributes
-------------------------------------------------------
insert dbo.ETLBatchConstraintAttribute
(BatchID,ConstId,AttributeName,AttributeValue)
values
 ' + right(@sql,len(@sql) - 1) + ';';

end
end

--steps
set @Script += '

-------------------------------------------------------
--create workflow steps
-------------------------------------------------------
declare @stepId int;
';

declare step_cur cursor local fast_forward
for select StepId from dbo.ETLStep
where BatchId = @BatchId;

declare @stepId int = 0;
open step_cur
while (1=1)
begin

fetch next from step_cur into @stepId;
if (@@FETCH_STATUS <> 0) break;

select @sql = '(@batchId,@stepId,'
		 + '''' + StepName + '''' + ','
		 + isnull('''' + StepDesc + '''','null') + ','
		 + cast(StepProcId as nvarchar(10)) + ','
		 + isnull(cast(OnSuccessID as nvarchar(10)),'null') + ','
		 + isnull(cast(OnFailureID as nvarchar(10)),'null') + ','
		 + isnull(cast(IgnoreErr as nvarchar(10)),'0') + ','
		 + isnull('''' + StepOrder + '''','null') + ')
'
from dbo.ETLStep where batchId = @batchId and stepId = @stepId;

set @Script += '

-------------------------------------------------------
--step: ' + cast(@stepId as nvarchar(10)) + '
-------------------------------------------------------
set @stepId = ' + cast(@stepId as nvarchar(10)) + ';
--set identity_insert dbo.ETLStep on;
insert dbo.ETLStep
(BatchID,StepID,StepName,StepDesc,StepProcID,OnSuccessID,OnFailureID,IgnoreErr,StepOrder)
values
 ' + @sql + ';
--set identity_insert dbo.ETLStep off;
';

--step attributes
--system attributes
set @sql = '';
select @sql += ',(@batchId,@stepId,''' + sa.attributeName + ''',''' + replace(sa.attributeValue,'''','''''') + ''')
'
from dbo.ETLStepAttribute sa
where sa.batchId = @batchId and sa.stepId = @stepId
and  sa.AttributeName in ('DISABLED','SEQGROUP','PRIGROUP','RETRY','DELAY','RESTART','LOOPGROUP');

if (len(@sql) > 0)
set @Script += '
-------------------------------------------------------
--Define workflow step level system attributes
--those attributes can be referenced in wf body like <etl:Disabled> etc.
-------------------------------------------------------
insert dbo.ETLStepAttribute
(BatchID,StepId,AttributeName,AttributeValue)
values
 ' + right(@sql,len(@sql) - 1) + ';';


--user attributes
set @sql = '';
select @sql += ',(@batchId,@stepId,''' + sa.attributeName + ''',''' + replace(sa.attributeValue,'''','''''') + ''')
'
from dbo.ETLStepAttribute sa
where sa.batchId = @batchId and sa.stepId = @stepId
and  sa.AttributeName not in ('DISABLED','SEQGROUP','PRIGROUP','RETRY','DELAY','RESTART','LOOPGROUP');

if (len(@sql) > 0)
set @Script += '
-------------------------------------------------------
--Define workflow step level user attributes
-------------------------------------------------------
insert dbo.ETLStepAttribute
(BatchID,StepId,AttributeName,AttributeValue)
values
 ' + right(@sql,len(@sql) - 1) + ';';


if exists (select 1 from dbo.ETLStepConstraint where batchId = @batchId and stepId = @stepId)
begin

set @Script += '
-------------------------------------------------------
--create step level constraints
-------------------------------------------------------

'
set @sql = '';
select @sql += ',(@batchId,@stepId,'
		 + cast(constId as nvarchar(10)) + ','
		 + isnull(cast(ProcessId as nvarchar(10)),'null') + ','
		 + isnull('''' + ConstOrder + '''','null') + ','
		 + isnull(cast(WaitPeriod as nvarchar(10)),'0') + ')'
from dbo.ETLStepConstraint where batchId = @batchId and stepId = @stepId
order by ConstId;


if (len(@sql) > 0)
set @Script += '
--set identity_insert dbo.ETLStepConstraint on
insert dbo.ETLStepConstraint
(BatchID,StepID,ConstID,,ProcessID,ConstOrder,WaitPeriod)
values
' + right(@sql,len(@sql) - 1) + ';
--set identity_insert dbo.ETLStepConstraint off;
';

--system attributes
set @sql = '';
select @sql += ',(@batchId,@stepId,' + cast(sca.ConstId as nvarchar(10)) + ',''' + sca.attributeName + ''',''' + replace(sca.attributeValue,'''','''''') + ''')
'
from dbo.ETLStepConstraintAttribute sca
where sca.batchId = @batchId and sca.stepId = @stepId
and  sca.AttributeName in ('DISABLED','PING')
order by sca.constId;

if (len(@sql) > 0)
begin
set @Script += '
-------------------------------------------------------
--Define workflow step constraint level system attributes
-------------------------------------------------------
insert dbo.ETLStepConstraintAttribute
(BatchID,StepID,ConstId,AttributeName,AttributeValue)
values
 ' + right(@sql,len(@sql) - 1) + ';';

--user attributes
set @sql = '';
select @sql += ',(@batchId,@stepId,' + cast(sca.ConstId as nvarchar(10)) + ',''' + sca.attributeName + ''',''' + replace(sca.attributeValue,'''','''''') + ''')
'
from dbo.ETLStepConstraintAttribute sca
where sca.batchId = @batchId
and  sca.AttributeName not in ('DISABLED','PING')
order by sca.constId;

set @Script += '

-------------------------------------------------------
--Define workflow step constraint level user attributes
-------------------------------------------------------
insert dbo.ETLStepConstraintAttribute
(BatchID,StepID,ConstId,AttributeName,AttributeValue)
values
 ' + right(@sql,len(@sql) - 1) + ';';

end
end

end
deallocate step_cur;

set @Script += '

end try
begin catch
   declare @msg nvarchar(1000)
   set @msg = ''ERRROR: set metadata failed with message: '' + error_message();
   throw 50001, @msg, 1;
end catch
'
;

--print substring (@Script,1,4000)
--print substring (@Script,4001,8000)
--print substring (@Script,8001,12000)
--print substring (@Script,12001,16000)

end try
begin catch
   set @msg = 'Failed to script etadata: ' + error_message();
   throw 50001, @msg, 1;
end catch