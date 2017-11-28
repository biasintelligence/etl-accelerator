USE ETL_Controller
GO
delete etlprocess where ProcessId between 20 and 50;
if not exists (select 1 from etlprocess where ProcessId between 20 and 50)
begin
	--set identity_insert dbo.etlprocess on;
	insert etlprocess
	(ProcessId,Process,[Param],ScopeId)
	values
	(20,'DefaultActivities.DefaultActivities.SqlServerActivity','Timeout=>Timeout,etl:Timeout',3)
	,(21,'DefaultActivities.DefaultActivities.SqlServerExecuteScalarActivity','Timeout=>Timeout,etl:Timeout',3)
	,(22,'DefaultActivities.DefaultActivities.CheckFileActivity',null,12)
	,(23,'DefaultActivities.DefaultActivities.ConsoleActivity','Timeout=>Timeout,etl:Timeout',15)
	,(24,'DefaultActivities.DefaultActivities.DeltaExtractorActivity','ConnectionString=>Controller.ConnectionString',3)
	,(25,'DefaultActivities.DefaultActivities.WaitActivity','Timeout=>WaitTimeout',3)
	--this is OnSuccess/OnError substitute for #20,#23. For Example Query attribute is substituted with CleanUpQuery.
	,(26,'DefaultActivities.DefaultActivities.SqlServerActivity','Query=>CleanUpQuery;Timeout=>Timeout,etl:Timeout',3)
	,(27,'DefaultActivities.DefaultActivities.ConsoleActivity','Console=>CleanUpConsole;Arg=>CleanUpArg;Timeout=>Timeout,etl:Timeout',15)
	,(28,'DefaultActivities.DefaultActivities.TGZCompressActivity','Timeout=>Timeout,etl:Timeout',15)
	,(29,'DefaultActivities.DefaultActivities.TGZDecompressActivity','[{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"Mode","Default":"tgz"},{"Name":"OutputExt","Default":""}]',15)
	,(30,'DefaultActivities.DefaultActivities.BsonConverterActivity','Timeout=>Timeout,etl:Timeout',15)
	,(31,'DefaultActivities.DefaultActivities.FileRegisterActivity','Timeout=>Timeout,etl:Timeout',15)
	,(32,'DefaultActivities.DefaultActivities.FileGetProcessListActivity','Timeout=>Timeout,etl:Timeout',15)
	,(33,'DefaultActivities.DefaultActivities.FileSetProgressStatusActivity','FileStatus=>OnSuccessStatus,FileStatus;Timeout=>Timeout,etl:Timeout',15)
	,(34,'DefaultActivities.DefaultActivities.FileSetProgressStatusActivity','FileStatus=>OnFailureStatus,FileStatus;Timeout=>Timeout,etl:Timeout',15)
	,(35,'DefaultActivities.DefaultActivities.SqlServerActivity','Query=>OnSuccessQuery,Query;ConnectionString=>OnSuccessConnectionString,ConnectionString;Timeout=>Timeout,etl:Timeout',15)
	,(36,'DefaultActivities.DefaultActivities.SqlServerActivity','Query=>OnFailureQuery,Query;ConnectionString=>OnFailureConnectionString,ConnectionString;Timeout=>Timeout,etl:Timeout',15)
	,(37,'DefaultActivities.DefaultActivities.BsonSqlLoaderActivity','Timeout=>Timeout,etl:Timeout',15)
	,(38,'DefaultActivities.DefaultActivities.AzureBlobDownloadActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"SortOrder","Default":"None"},{"Name":"Count","Default":"1000"},{"Name":"CounterName","Default":""},{"Name":"isSasToken","Default":"false"}]',15)
	,(39,'DefaultActivities.DefaultActivities.FileListToCounterActivity','Timeout=>Timeout,etl:Timeout',15)
	,(40,'DefaultActivities.DefaultActivities.PostWorkflowEventActivity','[{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"EventArgs","Default":""},{"Name":"ConnectionString","Override":["Event.ConnectionString"]}]',15)
	,(41,'DefaultActivities.DefaultActivities.CheckWorkflowEventActivity','[{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"ConnectionString","Override":["Event.ConnectionString"]}]',12)
	,(42,'DefaultActivities.DefaultActivities.AzureTableCopyActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"ControlColumn","Default":""},{"Name":"ControlValue","Default":""},{"Name":"isSasToken","Default":"false"}]',15)
	,(43,'DefaultActivities.DefaultActivities.TableControlToCounterActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"CounterName","Default":"ControlValue"}]',15)
	,(44,'DefaultActivities.DefaultActivities.AzureFileDownloadActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"SortOrder","Default":"None"},{"Name":"Count","Default":"0"},{"Name":"CounterName","Default":""},{"Name":"isSasToken","Default":"false"}]',15)
	,(45,'DefaultActivities.DefaultActivities.AwsS3DownloadActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"SortOrder","Default":"None"},{"Name":"Count","Default":"0"},{"Name":"CounterName","Default":""}]',15)



	--set identity_insert dbo.etlprocess off;
end
