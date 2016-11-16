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
using System.Threading.Tasks;
using System.ServiceModel;


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
        WfResult Status { get;}
    }

    // Workflow Logger interface.
    public interface IWorkflowLogger
    {
        // Log message
        void Write(string Message);
        void WriteDebug(string Message);
        void WriteError(string Message, int ErrorCode);
        bool Mode {get;}

    }

    // Workflow Activity interface.
    public interface IWorkflowActivity
    {
        // Log message
        string[] RequiredAttributes
        { get; }

        void Configure(WorkflowActivityArgs args);
        WfResult Run();
        void Cancel();

    }


}
