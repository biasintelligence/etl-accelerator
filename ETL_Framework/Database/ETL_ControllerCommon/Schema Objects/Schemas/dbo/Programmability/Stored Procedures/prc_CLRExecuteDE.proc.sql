CREATE PROCEDURE [dbo].[prc_CLRExecuteDE]
@exe NVARCHAR (4000), @args NVARCHAR (MAX), @timeout INT=0, @options NVARCHAR (1000)=NULL
AS EXTERNAL NAME [DeSqlClr].[ETL_Framework.DESQLCLR.DeFunctions].[ExecuteDE]

