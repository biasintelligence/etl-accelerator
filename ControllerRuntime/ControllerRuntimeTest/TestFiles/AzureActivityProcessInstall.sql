USE ETL_Controller
GO
delete etlprocess where ProcessId between 70 and 89;
if not exists (select 1 from etlprocess where ProcessId between 70 and 89)
begin
	--set identity_insert dbo.etlprocess on;
	insert etlprocess
	(ProcessId,Process,[Param],ScopeId)
	values
	 (70,'AzureActivities.AzureActivities.AzureBlobDownloadActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"SortOrder","Default":"None"},{"Name":"Count","Default":"1000"},{"Name":"CounterName","Default":""},{"Name":"isSasToken","Default":"false"}]',15)
	,(71,'AzureActivities.AzureActivities.AzureTableCopyActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"ControlColumn","Default":""},{"Name":"ControlValue","Default":""},{"Name":"isSasToken","Default":"false"}]',15)
	,(72,'AzureActivities.AzureActivities.AzureFileDownloadActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"SortOrder","Default":"None"},{"Name":"Count","Default":"0"},{"Name":"CounterName","Default":""},{"Name":"isSasToken","Default":"false"}]',15)



	--set identity_insert dbo.etlprocess off;
end
