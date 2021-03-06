﻿
CREATE PROCEDURE [dbo].[prc_SystemParameterSet] (
        @ParameterType VARCHAR(100) = NULL  -- The system parameter category.
      , @ParameterName VARCHAR(100) = NULL  -- The system parameter name.
	  , @EnvironmentName VARCHAR(100) = 'All' --The environment Name
    ) AS
/*
** Name:  [dbo].[prc_SystemParameterSet]
**
** Purpose:
**      Set current systemParameters values 
**
**/




    SET NOCOUNT ON

    /*
    ** Declarations.
    */

    DECLARE @FAIL smallint                      -- Failure code for RETURN.
    DECLARE @SUCCEED smallint                   -- Success code for RETURN.

    DECLARE @ProcedureName sysname              -- This procedure.
	DECLARE @msg nvarchar(1000)
	DECLARE @err int;

    /*
    ** Initialize the @ProcedureName for error messages.
    */

    SET @ProcedureName = OBJECT_NAME(@@PROCID)

    /*
    ** Initialize some constants.
    */

    SET @FAIL = 1
    SET @SUCCEED = 0

    /*
    ** Parameter Check:  @ParameterName
    ** Make sure that the @Parameter name exists if it is not NULL.
    */

    IF (@ParameterName IS NULL
        OR NOT EXISTS (SELECT *
                         FROM [dbo].[SystemParameters]
                        WHERE [ParameterType] = COALESCE(@ParameterType, '')
                          AND [ParameterName] = @ParameterName
						  AND [EnvironmentName] = @EnvironmentName))
    BEGIN
		SET @msg = '@ParameterName: ' + ISNULL(@ParameterName,'null') + ' is not found or null'; 
		THROW 50008, @msg, 1;
		RETURN @FAIL
    END

    /*
    ** Set the parameter value.
    */
	BEGIN TRY
    IF (@ParameterType IS NOT NULL)
        IF (@ParameterName IS NULL)
            UPDATE [dbo].[SystemParameters]
               SET [ParameterValue_Current] = ParameterValue_New
             WHERE [ParameterType] = @ParameterType
			 AND [EnvironmentName] = @EnvironmentName
        ELSE
            UPDATE [dbo].[SystemParameters]
               SET [ParameterValue_Current] = ParameterValue_New
             WHERE [ParameterName] = @ParameterName
               AND [ParameterType] = @ParameterType
			   AND [EnvironmentName] = @EnvironmentName
    ELSE
        UPDATE [dbo].[SystemParameters]
           SET [ParameterValue_Current] = ParameterValue_New
		WHERE [EnvironmentName] = @EnvironmentName


    RETURN @SUCCEED;
	END TRY
	BEGIN CATCH
		SET @err = ERROR_NUMBER();
		SET @msg = CAST(@err AS NVARCHAR(100)) + ': ' + ERROR_MESSAGE();
		THROW 50000, @msg,1;
		THROW @Err, @msg,1;
		RETURN @FAIL;
	END CATCH