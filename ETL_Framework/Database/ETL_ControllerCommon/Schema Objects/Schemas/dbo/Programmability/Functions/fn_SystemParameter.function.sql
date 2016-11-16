
/*
** Name:  [dbo].[fn_SystemParameter]
**
** $Workfile: fn_systemparameter.sql $
** $Archive: /Development/SubjectAreas/UserActivityStats/Database/Schema/Function/fn_systemparameter.sql $
**
** Purpose:
**      This script creates a function to return the current setting for
**  the specified system parameter.
**
** $Author: Karlj $
** $Revision: 6 $
** $BuildVersion: $
**
** Pre-conditions:
**      The system parameters are defined in the [dbo].[SystemParameters]
**  table.
**
** Input Arguments:
**
**      Name:         @ParameterCategory
**      Datatype:     udt_ParameterType
**      Default:      None
**      Description:  The system parameter category.
**
**      Name:         @ParameterName
**      Datatype:     udt_ParameterName
**      Default:      None
**      Description:  The system parameter name.  Names are unique to a
**                    parameter category.
**
** Return Type:
**      [udt_ParameterValue]
**
** Results Set:
**      None
**
** Post-conditions:
**      No changes are made to the database.
*/

CREATE FUNCTION [dbo].[fn_SystemParameter] (
        @ParameterCategory VARCHAR(32)    -- System parameter category.
      , @ParameterName VARCHAR(57)        -- System parameter name.
	  , @EnvironmentName VARCHAR(100)	  -- Environment Name
    ) RETURNS varchar(1024) AS

    BEGIN

        /*
        ** Declarations.
        */

        DECLARE @ReturnVar varchar(1024)

        /*
        ** Get the current system parameter setting.
        */

        SELECT @ReturnVar = [ParameterValue_Current]
          FROM [dbo].[SystemParameters]
         WHERE [ParameterType] = @ParameterCategory
           AND [ParameterName] = @ParameterName
		   AND EnvironmentName = @EnvironmentName;
        /*
        ** Return the results.
        */

        RETURN (@ReturnVar)

    END