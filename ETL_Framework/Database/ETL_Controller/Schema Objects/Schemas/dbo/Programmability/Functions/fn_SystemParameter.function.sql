
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
        @ParameterType VARCHAR(100)    -- System parameter type.
      , @ParameterName VARCHAR(100)        -- System parameter name.
	  , @EnvironmentName VARCHAR(100)	  -- Environment Name
	  --, @Passphrase VARCHAR(100) = '7CCC1B81-EA9E-4710-AD10-43452169017E'
    ) RETURNS varchar(max) AS

    BEGIN

		DECLARE @Passphrase VARCHAR(100) = '7CCC1B81-EA9E-4710-AD10-43452169017E';
        RETURN (SELECT TOP 1 CAST(  
			DecryptByPassphrase(@Passphrase,ISNULL([ParameterValue_Current],[ParameterValue_Default]), 1 ,   
			HashBytes('SHA1', CONVERT(varbinary(100), ParameterName))) as varchar(max)) 		
          FROM [dbo].[SystemParameters]
         WHERE [ParameterType] = @ParameterType
           AND [ParameterName] = @ParameterName
		   AND EnvironmentName = @EnvironmentName);

    END