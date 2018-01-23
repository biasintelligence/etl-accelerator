CREATE PROCEDURE [dbo].[prc_CLREventReceive]
@Server [sysname], @Database [sysname], @EventID UNIQUEIDENTIFIER=NULL OUTPUT, @EventPosted DATETIME=NULL OUTPUT, @EventReceived DATETIME=NULL OUTPUT, @EventArgs XML OUTPUT, @EventType [sysname], @options NVARCHAR (100)=NULL
AS EXTERNAL NAME [ControllerCLRExtensions].[ETL_Framework.ControllerCLRExtensions.ControllerExtensions].[EventReceive]

