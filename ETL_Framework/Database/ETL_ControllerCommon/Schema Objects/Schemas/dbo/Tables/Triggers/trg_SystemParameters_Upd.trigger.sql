
/*
** Name:  [trg_SystemParameters_Upd]
**
** $Workfile: systemparameters.sql $
** $Archive: /Development/SubjectAreas/WebUsage/Database/Schema/Table/Trigger/systemparameters.sql $
**
** Purpose:
**      This script creates a trigger on table [dbo].[SystemParameters].
**  Issue warning messages about changes to the system parameters.  These
**  errors should be trappable so that an operator can be notified of
**  changes.
**
** $Author: Karlj $
** $Revision: 4 $
** $BuildVersion: $
**
** Copyright (c) Microsoft Corporation 1996-2000.
** All rights reserved.
**
*/

CREATE TRIGGER [trg_SystemParameters_Upd] ON [dbo].[SystemParameters] FOR UPDATE AS

    /*
    ** Declarations.
    */

    DECLARE @ProcedureName sysname              -- This procedure.
    DECLARE @ParameterName varchar(57)
    DECLARE @ParameterValue_Current varchar(1024)
    DECLARE @ParameterValue_New varchar(1024)

    /*
    ** Initialize the @ProcedureName for error messages.
    */

    SELECT @ProcedureName = OBJECT_NAME(@@PROCID)

    /*
    ** Make sure that the current value is equal to the new value or
    ** else abort.
    */

    IF UPDATE([ParameterValue_Current]) AND
        EXISTS (SELECT *
                  FROM [inserted]
                 WHERE [ParameterValue_Current] <> [ParameterValue_New] )

        ROLLBACK TRANSACTION

    IF UPDATE([ParameterValue_Current]) AND UPDATE([ParameterValue_New])
        ROLLBACK TRANSACTION

    /*
    ** Post a warning message for each affected parameter.
    */

    DECLARE hC CURSOR FAST_FORWARD FOR
     SELECT i.[ParameterName]
          , i.[ParameterValue_Current]
          , i.[ParameterValue_New]
      FROM [inserted] AS i
      JOIN [deleted] AS d ON d.[ParameterName] = i.[ParameterName]
     WHERE d.[ParameterValue_Current] <> i.[ParameterValue_New]

    OPEN hC
    FETCH hC INTO @ParameterName, @ParameterValue_Current, @ParameterValue_New

    WHILE @@FETCH_STATUS <> -1
        BEGIN

            IF UPDATE(ParameterValue_Current)
                RAISERROR (50025, 10, -1, @ProcedureName, @ParameterName, @ParameterValue_Current)

            IF UPDATE(ParameterValue_New)
                RAISERROR (50024, 10, -1, @ProcedureName, @ParameterName, @ParameterValue_New)

            FETCH hC INTO @ParameterName, @ParameterValue_Current, @ParameterValue_New

        END

    CLOSE hC
    DEALLOCATE hC