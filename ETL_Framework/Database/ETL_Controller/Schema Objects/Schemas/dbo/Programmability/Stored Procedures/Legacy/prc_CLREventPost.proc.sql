CREATE PROCEDURE [dbo].[prc_CLREventPost]
@Server [sysname], @Database [sysname], @EventType [sysname], @EventPosted DATETIME=NULL, @args XML, @options NVARCHAR (100)=NULL
AS EXTERNAL NAME [ControllerCLRExtensions].[ETL_Framework.ControllerCLRExtensions.ControllerExtensions].[EventPost]

