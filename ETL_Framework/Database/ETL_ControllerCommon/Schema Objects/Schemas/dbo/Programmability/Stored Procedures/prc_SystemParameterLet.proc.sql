
CREATE PROCEDURE [dbo].[prc_SystemParameterLet] (
        @ParameterType varchar(32)				 -- The system parameter category.
      , @ParameterName varchar(57)				 -- The system parameter name.
      , @ParameterValue varchar(1024)			 -- The new setting.
      , @ParameterDefault varchar(1024) = NULL   -- The default setting.
      , @ParameterDesc varchar(1024) = NULL    -- Parameter description
      , @LastModifiedDtim datetime = NULL -- Last updated date/time.
      , @AffectiveImmediately bit = 0            -- 0= update later, 1= update now.
	  , @EnvironmentName VARCHAR(100) = 'All'  --The environment Name for which this variable is defined
    ) AS

/*
** Name:  [dbo].[prc_SystemParameterLet]
**
** $Workfile: prc_systemparameterlet.sql $
** $Archive: /Development/SubjectAreas/Dimensions20/Database/Schema/Procedure/prc_systemparameterlet.sql $
**
** Purpose:
**      This script creates a stored procedure to set a system parameter to
**  a new value.
**
** $Author: Karlj $
** $Revision: 3 $
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
**      Default:      None
**      Description:  The name of the system parameter category.
**
**      Name:         @ParameterName
**      Datatype:     udt_ParameterName
**      Default:      None
**      Description:  The name of the system parameter to set.
**
**      Name:         @ParameterValue
**      Datatype:     udt_ParameterValue
**      Default:      None
**      Description:  The new value for the system parameter.
**
**      Name:         @ParameterDefault
**      Datatype:     udt_ParameterValue
**      Default:      NULL
**      Description:  The default value for the system parameter.
**
**      Name:         @ParameterDesc
**      Datatype:     udt_Description
**      Default:      NULL
**      Description:  The description of the system parameter.
**
**      Name:         @LastModifiedDtim
**      Datatype:     udt_UpdatedTime
**      Default:      NULL
**      Description:  The last time the record was modified.  Optional
**                    Use this to guarantee the record wasn't modified
**                    since it was fetched to the client.
**
**      Name:         @AffectiveImmediately
**      Datatype:     bit
**      Default:      0 (update later)
**      Description:  If 1 then update immediately, otherwise dbo
**                    must issue [dbo].[prc_SystemParameterSet] to commit.
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
**      Run [dbo].[prc_SystemParameterSet] to make the changes active within
**  the system.
*/

    SET NOCOUNT ON

    /*
    ** Declarations.
    */

    DECLARE @FAIL smallint                      -- Failure code for RETURN.
    DECLARE @SUCCEED int                        -- Success code for RETURN.

    DECLARE @ProcedureName sysname              -- This procedure.
    DECLARE @RetCode int                        -- Procedure return code.

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
    ** Make sure that the @Parameter name exists and is not NULL.
    */

    IF @ParameterName IS NULL
        BEGIN
            RAISERROR (50008, 16, -1, @ProcedureName, '@ParameterName')
            RETURN @FAIL
        END

    /*
    ** Parameter Check:  @ParameterType
    ** Make sure that the parameter category exists and is not NULL.
    */

    IF @ParameterType IS NULL
        BEGIN
            RAISERROR (50008, 16, -1, @ProcedureName, '@ParameterType')
            RETURN @FAIL
        END

    /*
    ** If this is a new parameter, let's create it in the table.
    */

    IF NOT EXISTS (SELECT *
                     FROM [dbo].[SystemParameters]
                    WHERE [ParameterType] = @ParameterType
                      AND [ParameterName] = @ParameterName
					  AND [EnvironmentName] = @EnvironmentName)
        BEGIN

            INSERT INTO [dbo].[SystemParameters] (
                [ParameterType]
              , [ParameterName]
              , [ParameterValue_Current]
              , [ParameterValue_New]
              , [ParameterValue_Default]
              , [ParameterDesc]
			  , [EnvironmentName]
              , [LastModifiedBy]
              , [LastModifiedDtim]
            ) VALUES (
                @ParameterType
              , @ParameterName
              , NULL
              , NULL
              , @ParameterDefault
              , @ParameterDesc
              , @EnvironmentName
			  , SUSER_SNAME()
              , CURRENT_TIMESTAMP
            )

            IF @@ERROR <> 0
                BEGIN
                    RAISERROR (50003, 16, -1, @ProcedureName, '[dbo].[SystemParameters]')
                    RETURN @FAIL
                END
        END

    /*
    ** Before we update, let's make sure someone else hasn't already updated!
    */

    IF @LastModifiedDtim IS NOT NULL
        BEGIN
            IF NOT EXISTS (
                SELECT *
                  FROM [dbo].[SystemParameters]
                 WHERE [ParameterName] = @ParameterName
                   AND [ParameterType] = @ParameterType
                   AND CONVERT(varchar, [LastModifiedDtim], 121) = CONVERT(varchar, @LastModifiedDtim, 121)
                )
                BEGIN
                    RAISERROR (50030, 16, -1, @ProcedureName)
                    RETURN @FAIL
                END
        END

    /*
    ** Set the parameter value.
    */

    UPDATE [dbo].[SystemParameters]
      WITH (HOLDLOCK)
       SET [ParameterValue_New] = @ParameterValue
         , [LastModifiedBy] = SUSER_SNAME()
         , [LastModifiedDtim] = GETDATE()
     WHERE [ParameterName] = @ParameterName
       AND [ParameterType] = @ParameterType
	   AND [EnvironmentName] = @EnvironmentName

    IF @@ERROR <> 0
        BEGIN
            RAISERROR (50001, 16, -1, @ProcedureName, '[dbo].[SystemParameters]')
            RETURN @FAIL
        END

    /*
    ** Commit if necessary.
    */

    IF @AffectiveImmediately = 1
        BEGIN

            EXECUTE @RetCode = [dbo].[prc_SystemParameterSet]
                @ParameterType = @ParameterType
              , @ParameterName = @ParameterName
			  , @EnvironmentName = @EnvironmentName;

            IF @@ERROR <> 0
                BEGIN
                    RAISERROR (50017, 16, -1, @ProcedureName, '[dbo].[prc_SystemParameterSet]')
                    RETURN @FAIL
                END

            IF @RetCode <> 0 RETURN @FAIL

        END

    RETURN @SUCCEED