
/*
** Name:  [trg_SystemParameters_Del]
**
** $Workfile: systemparameters.sql $
** $Archive: /Development/SubjectAreas/WebUsage/Database/Schema/Table/Trigger/systemparameters.sql $
**
** Purpose:
**      This script creates a trigger on table [dbo].[SystemParameters].
**  Prevent users from removing system parameters.
**
** $Author: Karlj $
** $Revision: 4 $
** $BuildVersion: $
**
** Copyright (c) Microsoft Corporation 1996-2000.
** All rights reserved.
**
*/

CREATE TRIGGER [trg_SystemParameters_Del] ON [dbo].[SystemParameters] FOR DELETE AS

    /*
    ** Declarations.
    */

    DECLARE @ProcedureName sysname              -- This procedure.

    /*
    ** Initialize the @ProcedureName for error messages.
    */

    SELECT @ProcedureName = OBJECT_NAME(@@PROCID)

    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION

    RAISERROR (50026, 16, -1, @ProcedureName)