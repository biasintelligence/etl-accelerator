﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ETL_Framework.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ETL_Framework.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select BatchID, BatchName from dbo.ETLBatch with (nolock).
        /// </summary>
        internal static string QueryBatch {
            get {
                return ResourceManager.GetString("QueryBatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select RunID,BatchID,StatusID, StatusDT from dbo.ETLBatchRun with (nolock) where (StatusDT &gt; @StatusDT or @StatusDT is null) and StatusDT &gt; @TheDate.
        /// </summary>
        internal static string QueryBatchRun {
            get {
                return ResourceManager.GetString("QueryBatchRun", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select case StepID when 0 then &apos;*&apos; else &apos;&apos; end + CounterName as CounterName,CounterValue from dbo.ETLStepRunCounter with (nolock) where BatchID = @BatchID and StepId in (0,@StepID) and RunID= @RunID.
        /// </summary>
        internal static string QueryCounters {
            get {
                return ResourceManager.GetString("QueryCounters", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select LogID,Err,LogDT,LogMessage from dbo.ETLStepRunHistoryLog with (nolock) where BatchID = @BatchID and StepId = @StepID and RunID= @RunID and LogID &gt; @LogID.
        /// </summary>
        internal static string QueryLog {
            get {
                return ResourceManager.GetString("QueryLog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select r.RunID,s.StepDesc,r.StatusID,r.Err,r.StartTime,r.EndTime,r.StepID,r.SvcName from dbo.ETLStepRun r with (nolock) join dbo.ETLStep s with (nolock) on r.BatchID = s.BatchID and r.StepID = s.StepID where r.BatchID = @BatchID and (r.EndTime is null or r.EndTime &gt; @StatusDT or @StatusDT is null) order by r.PriGroup,r.StepOrder.
        /// </summary>
        internal static string QueryStepRun {
            get {
                return ResourceManager.GetString("QueryStepRun", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to select r.RunID,s.StepDesc,r.StatusID,r.Err,r.StartTime,r.EndTime,r.StepID,r.SvcName from dbo.ETLStepRunHistory r with (nolock) join dbo.ETLStep s with (nolock) on r.BatchID = s.BatchID and r.StepID = s.StepID where r.BatchID = @BatchID and r.RunID = @RunID order by r.PriGroup,r.StepOrder.
        /// </summary>
        internal static string QueryStepRunHistory {
            get {
                return ResourceManager.GetString("QueryStepRunHistory", resourceCulture);
            }
        }
    }
}
