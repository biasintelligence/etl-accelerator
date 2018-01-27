using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControllerRuntime
{
    static public class WorkflowConstants
    {
        public const string ATTRIBUTE_WORKFLOW_NAME = "etl:WorkflowName";
        public const string ATTRIBUTE_CONTROLLER_CONNECTIONSTRING = "etl:ConnectionString";
        public const string ATTRIBUTE_PROCESSOR_NAME = "etl:ProcessorName";
        public const string ATTRIBUTE_DEBUG = "etl:Debug";
        public const string ATTRIBUTE_FORCESTART = "etl:ForceStart";
        public const string ATTRIBUTE_VERBOSE = "etl:Verbose";

        public const string ATTRIBUTE_BATCH_ID = "@BatchId";
        public const string ATTRIBUTE_STEP_ID = "@StepId";
        public const string ATTRIBUTE_CONST_ID = "@ConstId";
        public const string ATTRIBUTE_RUN_ID = "@RunId";

        public const string ATTRIBUTE_ETL_BATCH_ID = "etl:BatchId";
        public const string ATTRIBUTE_ETL_STEP_ID = "etl:StepId";
        public const string ATTRIBUTE_ETL_CONST_ID = "etl:ConstId";
        public const string ATTRIBUTE_ETL_RUN_ID = "etl:RunId";

    }
}
