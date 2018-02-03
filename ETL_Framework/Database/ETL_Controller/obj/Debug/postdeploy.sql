﻿/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

--Environment script
--select * from dbo.systemparameters
--select dbo.fn_systemparameter('Environment','BuildLocation','DEV')
begin try
-------------------------------------------------------------------
-- CURRENT ENV
-------------------------------------------------------------------
exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'Current'
	,@ParameterValue = 'DEV'
	,@ParameterDefault = ''
	,@ParameterDesc = 'Current Environment'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'ALL';

-------------------------------------------------------------------
-- DEV
-------------------------------------------------------------------
exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'BuildLocation'
	,@ParameterValue = 'c:\Builds'
	,@ParameterDefault = ''
	,@ParameterDesc = 'Build location'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';

exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'DW'
	,@ParameterValue = 'DW'
	,@ParameterDefault = ''
	,@ParameterDesc = 'Datawarehouse'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';

exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'Staging'
	,@ParameterValue = 'ETL_Staging'
	,@ParameterDefault = ''
	,@ParameterDesc = 'Staging DB'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';

exec prc_SystemParameterLet
	@ParameterType = 'Environment'
	,@ParameterName = 'DW_Server'
	,@ParameterValue = '.'
	,@ParameterDefault = ''
	,@ParameterDesc = 'DW Server'
	,@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';


--EventServer 
exec prc_SystemParameterLet 
	@ParameterType = 'Environment',
	@ParameterName = 'EventServer',
	@ParameterValue = '.',
	@ParameterDesc = 'Event Server ',
	@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';


--EventDB 
exec prc_SystemParameterLet 
	@ParameterType = 'Environment',
	@ParameterName = 'EventDB',
	@ParameterValue = 'ETL_Event', 
	@ParameterDesc = 'Event database',
	@EffectiveImmediately = 1
	,@EnvironmentName = 'DEV';

-------------------------------------------------------------------
-- TEST
-------------------------------------------------------------------
-------------------------------------------------------------------
-- UAT
-------------------------------------------------------------------
-------------------------------------------------------------------
-- PROD
-------------------------------------------------------------------

end try
begin catch
   declare @msg nvarchar(1000);
   set @msg = error_message();
   raiserror('ERRROR: set metadata failed with message: %s',11,11,@msg);
end catch

USE ETL_Controller
GO
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
	,(35,'DefaultActivities.DefaultActivities.SqlServerActivity','Query=>OnSuccessQuery,Query;Timeout=>Timeout,etl:Timeout',15)
	,(36,'DefaultActivities.DefaultActivities.SqlServerActivity','Query=>OnFailureQuery,Query;Timeout=>Timeout,etl:Timeout',15)
	,(37,'DefaultActivities.DefaultActivities.BsonSqlLoaderActivity','Timeout=>Timeout,etl:Timeout',15)
	,(38,'DefaultActivities.DefaultActivities.AzureBlobDownloadActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"SortOrder","Default":"None"},{"Name":"Count","Default":"1000"},{"Name":"CounterName","Default":""},{"Name":"isSasToken","Default":"false"}]',15)
	,(39,'DefaultActivities.DefaultActivities.FileListToCounterActivity','Timeout=>Timeout,etl:Timeout',15)
	,(40,'DefaultActivities.DefaultActivities.PostWorkflowEventActivity','[{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"EventArgs","Default":""},{"Name":"ConnectionString","Override":["Event.ConnectionString"]}]',15)
	,(41,'DefaultActivities.DefaultActivities.CheckWorkflowEventActivity','[{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"ConnectionString","Override":["Event.ConnectionString"]}]',12)
	,(42,'DefaultActivities.DefaultActivities.AzureTableCopyActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"ControlColumn","Default":""},{"Name":"ControlValue","Default":""},{"Name":"isSasToken","Default":"false"}]',15)
	,(43,'DefaultActivities.DefaultActivities.TableControlToCounterActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"CounterName","Default":"ControlValue"}]',15)
	,(44,'DefaultActivities.DefaultActivities.AzureFileDownloadActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"SortOrder","Default":"None"},{"Name":"Count","Default":"0"},{"Name":"CounterName","Default":""},{"Name":"isSasToken","Default":"false"}]',15)
	,(45,'DefaultActivities.DefaultActivities.AwsS3DownloadActivity','[{"Name":"ConnectionString","Override":["Controller.ConnectionString"]},{"Name":"Timeout","Override":["Timeout","etl:Timeout"]},{"Name":"SortOrder","Default":"None"},{"Name":"Count","Default":"0"},{"Name":"CounterName","Default":""}]',15)



	set identity_insert dbo.etlprocess off;
end

--:r .\Misc\PopulateLegacyImportProcess.sql
--Create ETL Monitor role. Add users to this role to enable them to run the ETL_Monitor
--Set their default schema to [ETLMonitor]

IF  NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'ETLMonitor' AND type = 'R')
   CREATE ROLE [ETLMonitor] AUTHORIZATION [dbo]
GO
IF  NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'ETLMonitor')
   exec ('CREATE SCHEMA [ETLMonitor] AUTHORIZATION [ETLMonitor]');
GO
--Database level permissions
GRANT CREATE PROCEDURE TO [ETLMonitor];
GRANT SELECT TO [ETLMonitor];
GRANT EXECUTE ON OBJECT::dbo.prc_ETLCounterSet TO [ETLMonitor]; 
GRANT CREATE QUEUE TO [ETLMonitor];
GRANT CREATE SERVICE TO [ETLMonitor];
GRANT SUBSCRIBE QUERY NOTIFICATIONS TO [ETLMonitor];
GRANT VIEW DEFINITION TO [ETLMonitor];
 --Service Broker permissions
GRANT REFERENCES ON CONTRACT::[http://schemas.microsoft.com/SQL/Notifications/PostQueryNotification] TO [ETLMonitor];
GRANT RECEIVE ON dbo.QueryNotificationErrorsQueue TO [ETLMonitor];

--:r .\Workflows\Test.sql
--:r .\Workflows\Call_BCP.sql
--:r .\Workflows\Call_Powershell.sql
--:r .\Workflows\Call_SP.sql
--:r .\Workflows\FileCheck.sql
--:r .\Workflows\IncrementalStaging.sql
--:r .\Workflows\MoveData_Excel.sql
--:r .\Workflows\MoveData_TableToFile.sql
--:r .\Workflows\MoveData_TableToTable.sql
--:r .\Workflows\Process1.sql
--:r .\Workflows\Process2.sql
--:r .\Workflows\QueryType_MDX.sql
--:r .\Workflows\SeqGroup.sql
--:r .\Workflows\Step_WaitConstraint_Met.sql
--:r .\Workflows\Loop.sql




GO