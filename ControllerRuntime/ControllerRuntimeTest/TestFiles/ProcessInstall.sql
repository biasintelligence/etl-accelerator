delete etlprocess where ProcessId between 20 and 50;
if not exists (select 1 from etlprocess where ProcessId between 20 and 50)
begin
	set identity_insert dbo.etlprocess on;
	insert etlprocess
	(ProcessId,Process,[Param],ScopeId)
	values
	(20,'DefaultActivities.DefaultActivities.SqlServerActivity','Timeout=>Timeout,etl:Timeout',3)
	,(21,'DefaultActivities.DefaultActivities.SqlServerExecuteScalarActivity','Timeout=>Timeout,etl:Timeout',3)
	,(22,'DefaultActivities.DefaultActivities.CheckFileActivity',null,12)
	,(23,'DefaultActivities.DefaultActivities.ConsoleActivity','Timeout=>Timeout,etl:Timeout',15)
	,(24,'DefaultActivities.DefaultActivities.DeltaExtractorActivity','ConnectionString=>Controller.ConnectionString',3)
	--,(25,'DefaultActivities.DefaultActivities.WaitActivity','Timeout=>WaitTimeout',3)
	,(25,'DefaultActivities.DefaultActivities.WaitActivity','[{"Name":"Timeout","Override":["WaitTimeout"]}]',3)
	--this is OnSuccess/OnError substitute for #20,#23. For Example Query attribute is substituted with CleanUpQuery.
	,(26,'DefaultActivities.DefaultActivities.SqlServerActivity','Query=>CleanUpQuery',3)
	,(27,'DefaultActivities.DefaultActivities.ConsoleActivity','Console=>CleanUpConsole;Arg=>CleanUpArg',15)
	,(28,'DefaultActivities.DefaultActivities.TGZCompressActivity','Timeout=>Timeout,etl:Timeout',15)
	,(29,'DefaultActivities.DefaultActivities.TGZDecompressActivity','Timeout=>Timeout,etl:Timeout',15)
	,(29,'DefaultActivities.DefaultActivities.TGZDecompressActivity','[{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"Mode","Default":"tgz"},{"Name":"OutputExt","Default":""}]',15)
	,(30,'DefaultActivities.DefaultActivities.BsonConverterActivity','Timeout=>Timeout,etl:Timeout',15)
	,(31,'DefaultActivities.DefaultActivities.FileRegisterActivity','Timeout=>Timeout,etl:Timeout',15)
	,(32,'DefaultActivities.DefaultActivities.FileGetProcessListActivity','Timeout=>Timeout,etl:Timeout',15)
	,(33,'DefaultActivities.DefaultActivities.FileSetProgressStatusActivity','FileStatus=>OnSuccessStatus,FileStatus;Timeout=>Timeout,etl:Timeout',15)
	,(34,'DefaultActivities.DefaultActivities.FileSetProgressStatusActivity','FileStatus=>OnFailureStatus,FileStatus;Timeout=>Timeout,etl:Timeout',15)
	,(35,'DefaultActivities.DefaultActivities.SqlServerActivity','Query=>OnSuccessQuery,Query;Timeout=>Timeout,etl:Timeout',15)
	,(36,'DefaultActivities.DefaultActivities.SqlServerActivity','Query=>OnFailureQuery,Query;Timeout=>Timeout,etl:Timeout',15)
	,(37,'DefaultActivities.DefaultActivities.BsonSqlLoaderActivity','Timeout=>Timeout,etl:Timeout',15)
	,(38,'DefaultActivities.DefaultActivities.AzureBlobDownloadActivity','Timeout=>Timeout,etl:Timeout',15)
	,(39,'DefaultActivities.DefaultActivities.FileListToCounterActivity','Timeout=>Timeout,etl:Timeout',15)



	set identity_insert dbo.etlprocess off;
end
