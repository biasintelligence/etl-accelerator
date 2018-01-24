
/*
** Name:  [trg_SystemParameters_Del]
**
** Purpose:
**      This script creates a trigger on table [dbo].[SystemParameters].
**  Prevent users from removing system parameters.
**
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

    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    THROW 50026, 'SystemParameters delete operation is disabled', 1;