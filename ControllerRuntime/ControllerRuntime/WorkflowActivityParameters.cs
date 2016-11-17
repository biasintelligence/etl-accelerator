using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControllerRuntime
{
    public class WorkflowActivityParameters
    {
        NameValueCollection _parameters = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

        private WorkflowActivityParameters() { }

        public static WorkflowActivityParameters Create()
        {
            return new WorkflowActivityParameters();
        }

        public void Add(string key, string value)
        {
            _parameters.Add(key, value);
        }

        public string Get(string key)
        {
            string value = _parameters.Get(key);
            return value;
        }

        public NameValueCollection KeyList { get { return _parameters; } }
    }
}
