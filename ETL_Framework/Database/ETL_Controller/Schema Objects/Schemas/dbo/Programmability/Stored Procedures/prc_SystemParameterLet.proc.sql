
CREATE PROCEDURE [dbo].[prc_SystemParameterLet] (
        @ParameterType varchar(32)				 -- The system parameter category.
      , @ParameterName varchar(57)				 -- The system parameter name.
      , @ParameterValue varchar(1024)			 -- The new setting.
      , @ParameterDefault varchar(1024) = NULL   -- The default setting.
      , @ParameterDesc varchar(1024) = NULL    -- Parameter description
      , @LastModifiedDtim datetime = NULL -- Last updated date/time.
      , @EffectiveImmediately bit = 0            -- 0= update later, 1= update now.
	  , @EnvironmentName VARCHAR(100) = 'All'  --The environment Name for which this variable is defined
    ) AS

/*
** Name:  [dbo].[prc_SystemParameterLet]
**
**
** Purpose:
**      Set systemParameters Values
**
*
** */

    SET NOCOUNT ON

    /*
    ** Declarations.
    */

    DECLARE @FAIL smallint                      -- Failure code for RETURN.
    DECLARE @SUCCEED smallint                   -- Success code for RETURN.

    DECLARE @ProcedureName sysname              -- This procedure.
    DECLARE @RetCode int                        -- Procedure return code.
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
    ** Make sure that the @Parameter name exists and is not NULL.
    */

    IF (@ParameterName IS NULL)
        BEGIN
            SET @msg = '@ParameterName can not be null'; 
			THROW 50008, @msg, 1;
            RETURN @FAIL
        END

    /*
    ** Parameter Check:  @ParameterType
    ** Make sure that the parameter category exists and is not NULL.
    */

    IF (@ParameterType IS NULL)
        BEGIN
            SET @msg = '@ParameterType can not be null'; 
			THROW 50008, @msg, 1;
            RETURN @FAIL
        END

    /*
    ** If this is a new parameter, let's create it in the table.
    */

    BEGIN TRY
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

        END

    /*
    ** Before we update, let's make sure someone else hasn't already updated!
    */

    IF (@LastModifiedDtim IS NOT NULL)
        BEGIN
            IF NOT EXISTS (
                SELECT *
                  FROM [dbo].[SystemParameters]
                 WHERE [ParameterName] = @ParameterName
                   AND [ParameterType] = @ParameterType
                   AND CONVERT(varchar, [LastModifiedDtim], 121) = CONVERT(varchar, @LastModifiedDtim, 121)
                )
                BEGIN
					SET @msg = '@ParameterName was modified outside of scope of this transaction';
					THROW 50030,@msg,1;
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

    /*
    ** Commit if necessary.
    */

    IF @EffectiveImmediately = 1
        BEGIN

            EXECUTE @RetCode = [dbo].[prc_SystemParameterSet]
                @ParameterType = @ParameterType
              , @ParameterName = @ParameterName
			  , @EnvironmentName = @EnvironmentName;

            IF (@RetCode <> 0) RETURN @FAIL;

        END

    RETURN @SUCCEED
	END TRY
	BEGIN CATCH
		SET @err = ERROR_NUMBER();
		SET @msg = CAST(@err AS NVARCHAR(100)) + ': ' + ERROR_MESSAGE();
		THROW 50000, @msg,1;
		RETURN @FAIL;
	END CATCH
