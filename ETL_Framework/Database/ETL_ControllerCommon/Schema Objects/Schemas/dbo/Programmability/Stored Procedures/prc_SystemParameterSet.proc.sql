
CREATE PROCEDURE [dbo].[prc_SystemParameterSet] (
        @ParameterType VARCHAR(32) = NULL  -- The system parameter category.
      , @ParameterName VARCHAR(57) = NULL  -- The system parameter name.
	  , @EnvironmentName VARCHAR(100) = 'All' --The environment Name
    ) AS
/*
** Name:  [dbo].[prc_SystemParameterSet]
**
** $Workfile: prc_systemparameterset.sql $
** $Archive: /Development/SubjectAreas/Dimensions20/Database/Schema/Procedure/prc_systemparameterset.sql $
**
** Purpose:
**      This script creates a stored procedure to replace the current system
**  parameter setting(s) with the new setting(s).
**
** $Author: Karlj $
** $Revision: 1 $
** $BuildVersion: $
**
** Pre-conditions:
**      The SystemParameters table must exist as well as the specified
**  parameter.
**
** Input Arguments:
**
**      Name:         @ParameterType
**      Datatype:     udt_ParameterType
**      Default:      NULL
**      Description:  The name of the system parameter category.
**
**      Name:         @ParameterName
**      Datatype:     udt_ParameterName
**      Default:      NULL
**      Description:  The name of the system parameter to set.
**
**      Name:         @EnvironmentName
**      Datatype:     VARCHAR(100)
**      Default:      All
**      Description:  The name of the Environment where this parameter applies.
**                    
**
** Output Arguments:
**      None.
**
** Return Code:
**      0 = SUCCEED
**      1 = FAILURE
**
** Results Set:
**      None.
**
** Post-conditions:
**      None.
*/




    SET NOCOUNT ON

    /*
    ** Declarations.
    */

    DECLARE @FAIL smallint                      -- Failure code for RETURN.
    DECLARE @SUCCEED int                        -- Success code for RETURN.

    DECLARE @ProcedureName sysname              -- This procedure.

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

    IF @ParameterName IS NOT NULL
        IF NOT EXISTS (SELECT *
                         FROM [dbo].[SystemParameters]
                        WHERE [ParameterType] = COALESCE(@ParameterType, '')
                          AND [ParameterName] = @ParameterName
						  AND [EnvironmentName] = @EnvironmentName)
            BEGIN
                RAISERROR (50009, 16, -1, @ProcedureName, @ParameterName)
                RETURN @FAIL
            END

    /*
    ** Set the parameter value.
    */

    IF @ParameterType IS NOT NULL
        IF @ParameterName IS NULL
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

    IF @@ERROR <> 0
        BEGIN
            RAISERROR (50001, 16, -1, @ProcedureName, '[dbo].[SystemParameters]')
            RETURN @FAIL
        END

    RETURN @SUCCEED