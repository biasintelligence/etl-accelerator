ALTER TABLE [dbo].[SystemParameters]
	ADD CONSTRAINT [SystemParametersEnvironmentNameCK] 
	CHECK  (EnvironmentName IN ('All', 'Dev', 'SIT', 'UAT', 'PROD'))
;
