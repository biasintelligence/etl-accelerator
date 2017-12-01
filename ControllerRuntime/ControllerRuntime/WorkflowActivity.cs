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
// 2017-01-26       andrey              allow default param value to be empty string

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using Serilog;


namespace ControllerRuntime
{
    /// <summary>
    /// Activity Args exchange class.  
    /// </summary>
    public class WorkflowActivityArgs
    {

        public WorkflowAttribute[] RequiredAttributes
        { get; set; }

        public ILogger Logger
        { get; set; }

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
        private ILogger _logger;
        Dictionary<string, WorkflowAttribute> _attributes = new Dictionary<string, WorkflowAttribute>(StringComparer.InvariantCultureIgnoreCase);

        public WorkflowActivity(WorkflowProcess process, WorkflowAttribute[] attributes, ILogger logger)
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
                _logger.Information("Activate Activity {0}", _process.Process);
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
            string activity_namespace = String.Empty;
            for (int i = 1; i < activity_name.Length - 1; i++)
            {
                activity_namespace += ((i == 1) ? "" : ".") + activity_name[i];
            }
            string activity_classname = activity_name[activity_name.Length - 1];

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
            List<string> attr_list = new List<string>();

            if (required != null && required.Length > 0)
            {

                IList<WorkflowParameter> list = _process.Parameters;
                foreach (string name in required)
                {

                    ret = false;
                    WorkflowParameter curr_param = list.FirstOrDefault(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                    //find if override exist
                    if (curr_param != null && curr_param.Override != null)
                    {
                        foreach (string attr_name in curr_param.Override)
                        {
                            if (_attributes.Keys.Contains(attr_name))
                            {
                                found.Add(new WorkflowAttribute(name, _attributes[attr_name].Value));
                                ret = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (_attributes.Keys.Contains(name))
                        {
                            found.Add(new WorkflowAttribute(name, _attributes[name].Value));
                            ret = true;
                        }
                    }

                    if (!ret)
                    {
                        if (curr_param == null || curr_param.Default == null)
                        {
                            _logger.Error("Error {ErrorCode}: Attribute {Name} is not found", -11,name);
                            break;
                        }
                        else
                        {
                            found.Add(new WorkflowAttribute(name, curr_param.Default));
                        }
                    }
                }
            }

            args.RequiredAttributes = found.ToArray();
            return ret;
        }
    }
}
