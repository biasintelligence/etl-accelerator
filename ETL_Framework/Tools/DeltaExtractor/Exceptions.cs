using System;
using System.Collections.Generic;
using System.Text;

namespace BIAS.Framework.DeltaExtractor
{
    #region Exceptions
    public class InvalidTableNameException : Exception
    {
        public InvalidTableNameException(string err, Exception inner)
            : base(err, inner)
        {
        }
        public InvalidTableNameException(string err)
            : base(err)
        {
        }

    }
    public class InvalidArgumentException : Exception
    {
        public InvalidArgumentException(string err)
            : base(err)
        {
        }
        public InvalidArgumentException(string err, Exception inner)
            : base(err, inner)
        {
        }
    }

    public class DeltaExtractorRuntimeException : Exception
    {
        public DeltaExtractorRuntimeException(string err)
            : base(err)
        {
        }
        public DeltaExtractorRuntimeException(string err, Exception inner)
            : base(err, inner)
        {
        }
    }

    public class DeltaExtractorBuildException : Exception
    {
        public DeltaExtractorBuildException(string err)
            : base(err)
        {
        }
        public DeltaExtractorBuildException(string err, Exception inner)
            : base(err, inner)
        {
        }
    }


    public class CouldNotRenameFileDestination : Exception
    {
        public CouldNotRenameFileDestination(string filename)
            : base("Could not rename the output file: " + filename)
        {
        }
        public CouldNotRenameFileDestination(string filename, Exception inner)
            : base("Could not rename the output file: " + filename + " : " + inner.Message, inner)
        {
        }
    }
    public class CouldNotStartDeltaExtraction : Exception
    {
        public CouldNotStartDeltaExtraction(string err)
            : base("An error occured while starting a delta extraction." + err)
        {
        }
        public CouldNotStartDeltaExtraction(string err, Exception inner)
            : base(err, inner)
        {
        }
    }
    public class EndDeltaExtractionException : Exception
    {
        public EndDeltaExtractionException(string err)
            : base("An error occured while ending a delta extraction." + err)
        {
        }
        public EndDeltaExtractionException(string err, Exception inner)
            : base(err, inner)
        {
        }
    }

    public class CouldNotCreateStagingTableException : Exception
    {
        public CouldNotCreateStagingTableException(string TableSchema)
            : base("An error occurred while executing table DDL: " + TableSchema )
        {
        }
        public CouldNotCreateStagingTableException(string err, Exception inner)
            : base(err, inner)
        {
        }
    }

    public class CouldNotUploadStagingTableException : Exception
    {
        public CouldNotUploadStagingTableException(string TableSchema)
            : base("An error occurred while executing upload DDL: " + TableSchema)
        {
        }
        public CouldNotUploadStagingTableException(string err, Exception inner)
            : base(err, inner)
        {
        }
    }

    public class CouldNotEndExtractException : Exception
    {
        public CouldNotEndExtractException(string err)
            : base(err)
        {
        }
        public CouldNotEndExtractException(string err, Exception inner)
            : base(err, inner)
        {
        }
    }
    public class PushToDestinationsException : Exception
    {
        public PushToDestinationsException(string err)
            : base(err)
        {
        }
    }
    public class CouldNotSetValue : Exception
    {
        public CouldNotSetValue(string Name)
            : base("Could not set value for counter:" + Name)
        {
        }
        public CouldNotSetValue(string Name, Exception e)
            : base("Could not set value for counter:" + Name, e)
        {
        }
    }
    public class CouldNotGetValue : Exception
    {
        public CouldNotGetValue(string Name, Exception e)
            : base("Could not get value for counter:" + Name, e)
        {
        }
    }
    public class CouldNotPerformUpsert : Exception
    {
        public CouldNotPerformUpsert(string errMessage)
            : base(errMessage)
        {
        }
        public CouldNotPerformUpsert(string errMessage, Exception e)
            : base(errMessage, e)
        {
        }
    }
    public class CouldNotConnectToDBController : Exception
    {
        public CouldNotConnectToDBController(string srv, string db,Exception e)
            : base("Could not connect to ETL Controller: " + srv + "." + db + ": " + e.Message, e)
        {
        }
    }

    public class CouldNotConnectToDB : Exception
    {
        public CouldNotConnectToDB(string srv, string db, Exception e)
            : base("Could not connect to Database: " + srv + "." + db + ": " + e.Message, e)
        {
        }
    }

    public class CouldNotSendMessage : Exception
    {
        public CouldNotSendMessage(string msg, Exception e)
            : base("Could not send to ETL Controller: " + msg + ": " + e.Message, e)
        {
        }
        public CouldNotSendMessage(string msg)
            : base("Could not send to ETL Controller: " + msg)
        {
        }
    }
   public class CouldNotStartDeltaLoad : Exception
    {
        public CouldNotStartDeltaLoad(string srv, Exception e)
           : base("Could not start delta load: " + srv + ": " + e.Message, e)
        {
        }
        public CouldNotStartDeltaLoad(string srv)
            : base("Could not start delta load: " + srv)
        {
        }
    }

   public class CouldNotFinishDeltaLoad : Exception
   {
       public CouldNotFinishDeltaLoad(string srv, Exception e)
           : base("Could not finish delta load: " + srv + ": " + e.Message, e)
       {
       }
       public CouldNotFinishDeltaLoad(string srv)
           : base("Could not finish delta load: " + srv)
       {
       }
   }
   public class DsvTableNotFound : Exception
   {
       public DsvTableNotFound(string path, string tbl, Exception e)
           : base(String.Format("Could not find the table {0} in dsv at {1}: {2}",tbl,path,e.Message), e)
       {
       }
       public DsvTableNotFound(string path, string tbl)
           : base(String.Format("Could not find the table {0} in dsv at {1}",tbl,path))
       {
       }
   }

   public class InvalidDestinations : Exception
   {
       public InvalidDestinations(string err)
           : base(err)
       {
       }
   }


   public class UnknownSourceType : Exception
   {
       public UnknownSourceType()
           : base("Unknown Source Type")
       {
       }
   }

    #endregion
   #region template Exceptions

   public class CouldNotLoadTemplatePackageException : Exception
   {
       public CouldNotLoadTemplatePackageException(string err)
           : base("Could not load TemplatePackage: " + err)
       {
       }

   }
   public class CouldNotReintializeMetaDataException : Exception
   {
       public CouldNotReintializeMetaDataException(string CompName, string err)
           : base("Could not reintialize metadata for component " + CompName + ": " + err)
       {
       }

   }
   public class CouldNotSetPackageVariablesException : Exception
   {
       public CouldNotSetPackageVariablesException(string err)
           : base("An error occured while setting a package variable: " + err)
       {
       }
   }
   public class CouldNotAssignSortKeyOrderException : Exception
   {
       public CouldNotAssignSortKeyOrderException(string columnName, string err)
           : base("An error occurred while attempting to assign a sort order to column " + columnName + ": " + err)
       {
       }
   }
   public class ErrorMappingColumnToDestinationException : Exception
   {
       public ErrorMappingColumnToDestinationException(string columnName, string err)
           : base("An error occurred while mapping column " + columnName + " to a database destination: " + err)
       {
       }
   }
   public class UnexpectedSsisException : Exception
   {
       public UnexpectedSsisException(string err)
           : base("An unexpected error occurred while executing DeltaExtractor's SSIS package: " + err)
       {
       }
   }

    #endregion
}
