USE ETL_Controller
GO
delete etlprocess where ProcessId between 50 and 69;
if not exists (select 1 from etlprocess where ProcessId between 50 and 69)
begin
	--set identity_insert dbo.etlprocess on;
	insert etlprocess
	(ProcessId,Process,[Param],ScopeId)
	values
	 (50,'AwsActivities.AwsActivities.BsonConverterActivity','Timeout=>Timeout,etl:Timeout',15)
	,(51,'AwsActivities.AwsActivities.BsonSqlLoaderActivity','Timeout=>Timeout,etl:Timeout',15)
	,(52,'AwsActivities.AwsActivities.AwsS3DownloadActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"SortOrder","Default":"None"},{"Name":"Count","Default":"0"},{"Name":"CounterName","Default":""}]',15)



	--set identity_insert dbo.etlprocess off;
end
