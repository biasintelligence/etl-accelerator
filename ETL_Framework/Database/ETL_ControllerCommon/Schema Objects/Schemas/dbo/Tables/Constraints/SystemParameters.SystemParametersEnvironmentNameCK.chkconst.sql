ALTER TABLE [dbo].[SystemParameters]
	ADD CONSTRAINT [SystemParametersEnvironmentNameCK] 
	CHECK  (EnvironmentName IN ('ALL', 'DEV', 'TEST', 'UAT','PPE', 'PROD'))
;
