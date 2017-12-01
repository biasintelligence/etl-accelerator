/******************************************************************
**          BIAS Intelligence LLC
**
**
**Auth:     Andrey Shishkarev
**Date:     02/20/2015
*******************************************************************
**      Change History
*******************************************************************
**  Date:            Author:            Description:
*******************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceModel;

using Serilog;


namespace ControllerRuntime
{

    [ServiceContract(Name = "WorkflowCommand")]
    public interface IWorkflowCommand
    {
        [OperationContract]
        WfResult Start();
        [OperationContract]
        WfResult Stop();
        [OperationContract]
        WfResult Pause();
        [OperationContract]
        WfResult Resume();
        WfResult Status { get; }
    }


    // Workflow Activity interface.
    public interface IWorkflowActivity
    {
        // Log message
        string[] RequiredAttributes
        { get; }

        void Configure(WorkflowActivityArgs args);
        WfResult Run(CancellationToken token);

    }

    public interface IWorkflowRunner
    {
        WfResult Start(WorkflowActivityParameters args, ILogger logger);
    }


}
