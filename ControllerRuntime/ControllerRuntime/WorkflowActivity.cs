﻿/******************************************************************
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
using System.Reflection;


namespace ControllerRuntime
{
    /// <summary>
    /// Activity Args exchange class.  
    /// </summary>
    public class WorkflowActivityArgs
    {

        public WorkflowAttribute[] RequiredAttributes
        {get; set;}

        public IWorkflowLogger Logger
        {get; set;}

    }
/// <summary>
/// Activity Execution Wrapper.
/// Load/Configure/Run Workflow Activity modules
/// Load Activity Assembly
/// Query for required attributes
/// Provide Attribute values back to Activity
/// Run it
/// 
/// params hold attribute override directives in the form
/// name1=>override_name1;name2=>override_name2
/// </summary>
    
    public class WorkflowActivity
    {
        private const string ACTIVITY_LOCATION = @"ActivityLocation";
        
        private WorkflowProcess _process;
        private IWorkflowLogger _logger;
        Dictionary<string, WorkflowAttribute> _attributes = new Dictionary<string, WorkflowAttribute>();

        public WorkflowActivity(WorkflowProcess process,WorkflowAttribute[] attributes, IWorkflowLogger logger)
        {
            _process = process;
            _logger = logger;

            LoadAttributes(attributes);

        }

        public IWorkflowActivity Activate()
        {
            IWorkflowActivity activity = null;
            try
            {
                _logger.Write(String.Format("Activate Activity {0}", _process.Process));
                activity = LoadActivity();

                //get the list of the required attributes from Activity
                string[] required = activity.RequiredAttributes;
                //check if we have what we need
                WorkflowActivityArgs activity_args = new WorkflowActivityArgs();
                if (!ProcessAttributeRequest(activity_args, required))
                {
                    throw new Exception("Not all requested Attributes are available");
                }

                activity_args.Logger = _logger;
                activity.Configure(activity_args);
            }
            catch (Exception ex)
            {
                //result = WfResult.Create(WfStatus.Failed, ex.Message, -5);
                throw ex;
            }
            return activity;
        }

        private IWorkflowActivity LoadActivity()
        {

            
            string activity_path = String.Empty;
            if (_attributes.Keys.Contains(ACTIVITY_LOCATION))
                activity_path = _attributes[ACTIVITY_LOCATION].Value;

            string[] activity_name = _process.Process.Split('.');
            string activity_dll = activity_name[0] + ".dll";
            string activity_namespace = (activity_name.Length > 1) ? activity_name[1] : String.Empty;
            string activity_classname = (activity_name.Length > 2) ? activity_name[2] : String.Empty;

            string dll_path = System.IO.Path.Combine(activity_path, activity_dll);
            string path = System.IO.Path.GetFullPath(dll_path);
            IWorkflowActivity activity = null;
            try
            {

                // Load dll
                Assembly assembly = Assembly.LoadFile(path);

                // Look for the interface
                foreach (Type item in assembly.GetTypes())
                {
                    if (!item.IsClass) continue;

                    if (!(item.Namespace == activity_namespace && item.Name == activity_classname))
                        continue;

                    if (item.GetInterfaces().Contains(typeof(IWorkflowActivity)))
                    {
                        activity = (IWorkflowActivity)Activator.CreateInstance(item);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                ReflectionTypeLoadException tex = ex as ReflectionTypeLoadException;
                if (tex != null && tex.LoaderExceptions != null)
                {
                    foreach (Exception lex in tex.LoaderExceptions)
                    {
                        message += " " + lex.Message;
                    }
                }
                throw new Exception(string.Format("Error loading activity DLL {0}: {1}!", path, message));
            }

            // no IWorkflowActivity interface
            if (activity == null)
            {
                throw new Exception(string.Format("IWorkflowActivity interface is missing {0}!", path));
            }

            return activity;
        }

        private void LoadAttributes(WorkflowAttribute[] attributes)
        {
            foreach (WorkflowAttribute att in attributes)
            {
                _attributes.Add(att.Name, att);
            }
        }

        private bool ProcessAttributeRequest(WorkflowActivityArgs args, string[] required)
        {
            bool ret = true;
            List<WorkflowAttribute> found = new List<WorkflowAttribute>();
            Dictionary<string, string> map = ParseParameterString(_process.Param);
            foreach (string name in required)
            {

                string attr_name = name;
                //find if override exist
                if (map.Keys.Contains(name))
                    attr_name = map[name];

                if (_attributes.Keys.Contains(attr_name))
                    found.Add(new WorkflowAttribute(name, _attributes[attr_name].Value));
                else
                {
                    ret = false;
                    _logger.WriteError(String.Format("Attribute {0} is not found", attr_name), -11);

                }
            }

            args.RequiredAttributes = found.ToArray();
            return ret;
        }

        private Dictionary<string,string> ParseParameterString(string param)
        {
            Dictionary<string,string> dic = new Dictionary<string,string>();

            if (String.IsNullOrEmpty(param))
                return dic;

            string[] map = param.Split(';');
            foreach (string pair in map)
            {
                string[] kvp = pair.Split(new string[] {"=>"},StringSplitOptions.None);
                if (kvp.Length == 2
                    && !String.IsNullOrEmpty(kvp[0])
                    && !String.IsNullOrEmpty(kvp[1])
                    )
                {
                    dic.Add(kvp[0], kvp[1]);
                }
            }
            return dic;
        }

    }
}
