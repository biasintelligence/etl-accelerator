CREATE TABLE [dbo].[SystemParameters] (
    [ParameterType]          VARCHAR (100)   NOT NULL,
    [ParameterName]          VARCHAR (100)   NOT NULL,
    [ParameterValue_Current]	 VARBINARY (max) NULL,
    [ParameterValue_New]		 VARBINARY (max) NULL,
    [ParameterValue_Default]	 VARBINARY (max) NULL,
    [ParameterDesc]          VARCHAR (1024)  NOT NULL,
	[EnvironmentName]		 VARCHAR(100)	 NOT NULL,
    [LastModifiedBy]         SYSNAME		 NOT NULL DEFAULT SYSTEM_USER,
    [LastModifiedDtim]       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
);

