/*
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

:r .\Environment.sql
:r .\Misc\PopulateWFRunnerImportProcess.sql
--:r .\Misc\PopulateLegacyImportProcess.sql
:r .\Misc\ETLMonitorPermissions.sql
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




