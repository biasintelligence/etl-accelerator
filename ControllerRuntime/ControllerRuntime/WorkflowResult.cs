using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControllerRuntime
{

    public enum WfStatus
    {
        Unknown,
        Running,
        Suspended,
        Succeeded,
        Failed,
        Disabled
    }

    /// <summary>
    /// Workflow Result class. Workflow Status communication token. 
    /// </summary>
    public class WfResult
    {

        protected WfResult(WfStatus Status, string Message, int Error)
        {
            this.StatusCode = Status;
            this.Message = Message;
            this.ErrorCode = Error;
        }

        public static WfResult Create(WfStatus Status, string Message, int Error)
        {
            return new WfResult(Status, Message, Error);
        }

        public static WfResult Create(WfResult result)
        {
            return new WfResult(result.StatusCode, result.Message, result.ErrorCode);
        }

        public static WfResult Started
        { get { return new WfResult(WfStatus.Running, "Started", 0); } }

        public static WfResult Succeeded
        { get { return new WfResult(WfStatus.Succeeded, "Succeeded", 0); } }

        public static WfResult Canceled
        { get { return new WfResult(WfStatus.Failed, "Canceled", -1); } }

        public static WfResult Failed
        { get { return new WfResult(WfStatus.Failed, "Failed", -1); } }

        public static WfResult Paused
        { get { return new WfResult(WfStatus.Suspended, "Paused", 0); } }
        public static WfResult Unknown
        { get { return new WfResult(WfStatus.Unknown, "Not Started", 0); } }

        public void SetTo(WfResult result)
        {
            this.StatusCode = result.StatusCode;
            this.Message = result.Message;
            this.ErrorCode = result.ErrorCode;
        }
        public WfStatus StatusCode
        {
            get { return wf_status; }
            set { wf_status = value; }
        }
        private WfStatus wf_status = WfStatus.Unknown;

        public string Message
        { get; set; }

        public int ErrorCode
        { get; set; }

        public void Clear()
        {
            ErrorCode = 0;
            Message = String.Empty;
            StatusCode = WfStatus.Unknown;
        }
    }
}
