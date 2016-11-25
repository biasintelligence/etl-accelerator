delete etlprocess where ProcessId between 20 and 50;
if not exists (select 1 from etlprocess where ProcessId between 20 and 50)
begin
	set identity_insert dbo.etlprocess on;
	insert etlprocess
	(ProcessId,Process,[Param],ScopeId)
	values
	(20,'DefaultActivities.DefaultActivities.SqlServerActivity',null,3)
	,(21,'DefaultActivities.DefaultActivities.SqlServerExecuteScalarActivity',null,3)
	,(22,'DefaultActivities.DefaultActivities.CheckFileActivity',null,12)
	,(23,'DefaultActivities.DefaultActivities.ConsoleActivity',null,15)
	,(24,'DefaultActivities.DefaultActivities.DeltaExtractorActivity','ConnectionString=>Controller.ConnectionString',3)
	,(25,'DefaultActivities.DefaultActivities.WaitActivity','Timeout=>WaitTimeout',3)
	--this is OnSuccess/OnError substitute for #20,#23. For Example Query attribute is substituted with CleanUpQuery.
	,(26,'DefaultActivities.DefaultActivities.SqlServerActivity','Query=>CleanUpQuery',3)
	,(27,'DefaultActivities.DefaultActivities.ConsoleActivity','Console=>CleanUpConsole;Arg=>CleanUpArg',15)
	,(28,'DefaultActivities.DefaultActivities.TGZCompressActivity',null,15)
	,(29,'DefaultActivities.DefaultActivities.TGZDecompressActivity',null,15)
	,(30,'DefaultActivities.DefaultActivities.BsonConverterActivity',null,15)
	,(31,'DefaultActivities.DefaultActivities.FileRegisterActivity',null,15)
	,(32,'DefaultActivities.DefaultActivities.FileGetProcessListActivity',null,15)
	,(33,'DefaultActivities.DefaultActivities.FileSetProgressStatusActivity',null,15)



	set identity_insert dbo.etlprocess off;
end
