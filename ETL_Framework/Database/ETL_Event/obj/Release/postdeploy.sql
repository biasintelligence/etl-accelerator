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

insert EventType
select NEWID(),'Process1_FINISHED','EventArgs.XSD','RROD',100,getdate(), getdate()
union
select NEWID(),'Process2_FINISHED','EventArgs.XSD','RROD',100,getdate(), getdate()
GO
