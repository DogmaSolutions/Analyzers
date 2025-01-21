﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DogmaSolutions.Analyzers {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DogmaSolutions.Analyzers.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to A WebApi method is using Entity Framework DbContext to directly manipulate data through a LINQ query expression.
        ///WebApi controllers should not contain data-manipulation business logics.
        ///Move the data-manipulation business logics into a more appropriate class, or even better, an injected service..
        /// </summary>
        internal static string DSA001AnalyzerDescription {
            get {
                return ResourceManager.GetString("DSA001AnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The WebApi method &apos;{0}&apos; is using Entity Framework DbContext to directly manipulate data through a LINQ query expression. WebApi controllers should not contain data-manipulation business logics. Move the data-manipulation business logics into a more appropriate class, or even better, an injected service..
        /// </summary>
        internal static string DSA001AnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("DSA001AnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to WebApi controllers should not contain data-manipulation business logics through a LINQ query expression.
        /// </summary>
        internal static string DSA001AnalyzerTitle {
            get {
                return ResourceManager.GetString("DSA001AnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A WebApi method is using Entity Framework DbSet to directly manipulate data through a LINQ fluent query.
        ///WebApi controllers should not contain data-manipulation business logics.
        ///Move the data-manipulation business logics into a more appropriate class, or even better, an injected service..
        /// </summary>
        internal static string DSA002AnalyzerDescription {
            get {
                return ResourceManager.GetString("DSA002AnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The WebApi method &apos;{0}&apos; is invoking the method &apos;{1}&apos; of the DbSet &apos;{2}&apos; to directly manipulate data through a LINQ fluent query. WebApi controllers should not contain data-manipulation business logics. Move the data-manipulation business logics into a more appropriate class, or even better, an injected service..
        /// </summary>
        internal static string DSA002AnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("DSA002AnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to WebApi controllers should not contain data-manipulation business logics through a LINQ fluent query.
        /// </summary>
        internal static string DSA002AnalyzerTitle {
            get {
                return ResourceManager.GetString("DSA002AnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Usually, business logics distinguish between &quot;string with content&quot;, and &quot;string NULL or without meaningfull content&quot;. Thus, statistically speaking, almost every call to `string.IsNullOrEmpty` could or should be replaced by a call to `string.IsNullOrWhiteSpace`, because in the large majority of cases, a string composed by only spaces, tabs, and return chars is not considered valid because it doesn&apos;t have &quot;meaningfull content&quot;. In most cases, `string.IsNullOrEmpty` is used by mistake, or has been written when [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DSA003AnalyzerDescription {
            get {
                return ResourceManager.GetString("DSA003AnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t use `string.IsNullOrEmpty`. Use `string.IsNullOrWhiteSpace` instead. Usually, business logics distinguish between &quot;string with content&quot;, and &quot;string NULL or without meaningfull content&quot;. Thus, statistically speaking, almost every call to `string.IsNullOrEmpty` could or should be replaced by a call to `string.IsNullOrWhiteSpace`, because in the large majority of cases, a string composed by only spaces, tabs, and return chars is not considered valid because it doesn&apos;t have &quot;meaningfull content&quot;. In most [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DSA003AnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("DSA003AnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t use `string.IsNullOrEmpty`. Use `string.IsNullOrWhiteSpace` instead.
        /// </summary>
        internal static string DSA003AnalyzerTitle {
            get {
                return ResourceManager.GetString("DSA003AnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Using `DateTime.Now` into business logics potentially leads to many different problems:
        ///- Incoherence between nodes or processes running in different timezones (even in the same country, i.e. USA, Soviet Union, China, etc)
        ///- Unexpected behaviours in-between legal time changes
        ///- Code conversion problems and loss of timezone info when saving/loading data to/from a datastore
        ///
        ///Security-wise, this is correlated to the CWE category “7PK”
        ///https://cwe.mitre.org/data/definitions/361.html
        ///Cit:
        ///*&quot;This category [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DSA004AnalyzerDescription {
            get {
                return ResourceManager.GetString("DSA004AnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to In a i18n-compliant system, it&apos;s almost never a good idea to use System.DateTime.Now. Please use System.DateTime.UtcNow instead. If you are sure you want to use System.DateTime.Now, use a &apos;#pragma warning disable DSA004 / #pragma warning restore DSA004&apos; directives pair..
        /// </summary>
        internal static string DSA004AnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("DSA004AnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t use `DateTime.Now`. Use `DateTime.UtcNow` instead.
        /// </summary>
        internal static string DSA004AnalyzerTitle {
            get {
                return ResourceManager.GetString("DSA004AnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An execution flow must always be as deterministic as possible.
        ///This means that all decisions inside a scope or algorithm must be performed on a &quot;stable&quot; and immutable set of parameters/conditions.
        ///When dealing with dates and times, always ensure that the point-in-time reference is fixed, otherwise the algorithm would work on a &quot;sliding window&quot;, leading to unpredictable results.
        ///This is particularly impacting in datasource-dependent flows, slow algorithms, and in-between legal time changes.
        ///Security-wise [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DSA005AnalyzerDescription {
            get {
                return ResourceManager.GetString("DSA005AnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Referencing multiple times `DateTime***.Now` or `DateTime***.UtcNow` in the same method, could lead to a non-deterministic point-in-time execution.  
        ///        In order to avoid problems, apply one of these:  
        ///        - When measuring elapsed time, use a `StopWatch.StartNew()` combined with `StopWatch.Elapsed`  
        ///        - When NOT measuring elapsed time, set a `var now = DateTime.UtcNow` variable at the top of the method, or at the beginning of an execution flow/algorithm, and reuse that variable in all pl [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DSA005AnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("DSA005AnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Potential non-deterministic point-in-time execution.
        /// </summary>
        internal static string DSA005AnalyzerTitle {
            get {
                return ResourceManager.GetString("DSA005AnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to General exceptions should never be thrown, because throwing them, prevents calling methods from discriminating between system-generated exceptions, and application-generated errors.  
        ///This is a bad smell, and could lead to stability and security concerns.  
        ///General exceptions that triggers this rule are: Exception, SystemException, ApplicationException, IndexOutOfRangeException, NullReferenceException, OutOfMemoryException and ExecutionEngineException prevents calling methods from handling true, system-ge [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DSA006AnalyzerDescription {
            get {
                return ResourceManager.GetString("DSA006AnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to General exceptions should never be thrown, because throwing them, prevents calling methods from discriminating between system-generated exceptions, and application-generated errors.  
        ///This is a bad smell, and could lead to stability and security concerns.  
        ///General exceptions that triggers this rule are: Exception, SystemException, ApplicationException, IndexOutOfRangeException, NullReferenceException, OutOfMemoryException and ExecutionEngineException prevents calling methods from handling true, system-ge [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string DSA006AnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("DSA006AnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to General exceptions should not be thrown by user code.
        /// </summary>
        internal static string DSA006AnalyzerTitle {
            get {
                return ResourceManager.GetString("DSA006AnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to When initializing a lazy field, use a robust locking pattern, i.e. the &quot;if-lock-if&quot; (aka &quot;double checked locking&quot;) to efficiently ensure that the variable is not initialized multiple times, and that no race-conditions occurs.
        ///.
        /// </summary>
        internal static string DSA007AnalyzerDescription {
            get {
                return ResourceManager.GetString("DSA007AnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to When initializing a lazy field, use a robust locking pattern, i.e. the &quot;if-lock-if&quot; (aka &quot;double checked locking&quot;) to efficiently ensure that the variable is not initialized multiple times, and that no race-conditions occurs.       
        ///    .
        /// </summary>
        internal static string DSA007AnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("DSA007AnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use the &quot;if-lock-if&quot; (aka &quot;double checked locking&quot;) initialization pattern instead of a simple lock.
        /// </summary>
        internal static string DSA007AnalyzerTitle {
            get {
                return ResourceManager.GetString("DSA007AnalyzerTitle", resourceCulture);
            }
        }
    }
}
