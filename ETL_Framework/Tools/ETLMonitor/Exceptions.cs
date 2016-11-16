using System;
using System.Collections.Generic;
using System.Text;

namespace ETL_Framework
{
    #region Exceptions
    public class InvalidArgumentException : Exception
    {
        public InvalidArgumentException(string in_Error)
            : base(in_Error)
        {
        }
        public InvalidArgumentException(string in_Error, Exception inner)
            : base(in_Error, inner)
        {
        }
    }
    public class CouldNotConnectToDBController : Exception
    {
        public CouldNotConnectToDBController(string in_Server, string in_Database)
            : base("Could not connect to " + in_Server + "." + in_Database)
        {
        }
        public CouldNotConnectToDBController(string in_Server, string in_Database, Exception ex)
            : base("Could not connect to " + in_Server + "." + in_Database + ": " + ex.Message)
        {
        }
    }

    public class CouldNotReceiveNotifications : Exception
    {
        public CouldNotReceiveNotifications(string in_Server, string in_Database)
            : base("User doesnt have permissions to receive notifications from :" + in_Server + "." + in_Database)
        {
        }
    }


    #endregion
   #region template Exceptions

   public class CouldNotLoadTemplatePackageException : Exception
   {
       public CouldNotLoadTemplatePackageException(string in_Error)
           : base("Could not load TemplatePackage: " + in_Error)
       {
       }

   }
   public class CouldNotReintializeMetaDataException : Exception
   {
       public CouldNotReintializeMetaDataException(string in_CompName, string in_Error)
           : base("Could not reintialize metadata for component " + in_CompName + ": " + in_Error)
       {
       }

   }
   public class CouldNotSetPackageVariablesException : Exception
   {
       public CouldNotSetPackageVariablesException(string in_Error)
           : base("An error occured while setting a package variable: " + in_Error)
       {
       }
   }
   public class CouldNotAssignSortKeyOrderException : Exception
   {
       public CouldNotAssignSortKeyOrderException(string in_columnName, string in_Error)
           : base("An error occurred while attempting to assign a sort order to column " + in_columnName + ": " + in_Error)
       {
       }
   }
   public class ErrorMappingColumnToDestinationException : Exception
   {
       public ErrorMappingColumnToDestinationException(string in_columnName, string in_Error)
           : base("An error occurred while mapping column " + in_columnName + " to a database destination: " + in_Error)
       {
       }
   }
   public class UnexpectedSsisException : Exception
   {
       public UnexpectedSsisException(string in_Error)
           : base("An unexpected error occurred while executing DeltaExtractor's SSIS package: " + in_Error)
       {
       }
   }

    #endregion
}
