using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
// ReSharper disable once InconsistentNaming
public class DSA007Tests
{
    [TestMethod]
    public async Task Empty()
    {
        var test = string.Empty;

        await CSharpAnalyzerVerifier<DSA007Analyzer>.VerifyAnalyzerAsync(test).ConfigureAwait(false);
    }

    private static IEnumerable<object[]> GetQueryExpressionSyntaxNotMatchedCases =>
    [
        [
            "if-lock-if (value from constant) 1", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public void IsOk(int id)
      {     
        if(_theField == null) {
            lock(_theLock){
                if(_theField == null) {
                   _theField = ""Test Value"";
                }
            }
        }
      }
    }
}
"
        ],
        [
            "if-lock-if (value from constant) 2", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public void IsOk(int id)
      {     
        if(_theField == null) 
            lock(_theLock){
                if(_theField == null) {
                   _theField = ""Test Value"";
                }
            }
      }
    }
}
"
        ],
        [
            "if-lock-if (value from constant) 3", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public void IsOk(int id)
      {     
        if(_theField == null) 
            lock(_theLock)
                if(_theField == null) {
                   _theField = ""Test Value"";
                }            
      }
    }
}
"
        ],
        [
            "if-lock-if (value from constant) 4", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public void IsOk(int id)
      {     
        if(_theField == null) 
            lock(_theLock)
                if(_theField == null) 
                   _theField = ""Test Value"";
      }
    }
}
"
        ],
        [
            "if-lock-if (value from constant) 5", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public void IsOk(int id)
      {     
        if(_theField == null) 
            if(id>0)
                lock(_theLock)
                    if(_theField == null) 
                       _theField = ""Test Value"";
      }
    }
}
"
        ],
        [
            "if-lock-if (value from constant) 6", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public void IsOk(int id)
      {     
        if(_theField == null) 
            if(id>0){
                lock(_theLock)
                    if(_theField == null) 
                       _theField = ""Test Value"";
            }
      }
    }
}
"
        ],
        [
            "if-lock-if (value from constant) 7", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public void IsOk(int id)
      {     
        if(_theField == null) 
            if(id>0){
                lock(_theLock)
                    if(_theField == null) 
                       _theField = ""Test Value"";
            } 
            else
               return;
      }
    }
}
"
        ],
        [
            "if-lock-if (value from constant) 8", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public void IsOk(int id)
      {     
        if(_theField == null) 
            if(id>0){
                lock(_theLock)
                    if(_theField == null) 
                       _theField = ""Test Value"";
                    else
                        return;
            } 
            else
               return;
      }
    }
}
"
        ],
        [
            "if-lock-if (value from constant) 8", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public void IsOk(int id)
      {     
        if(_theField == null) {
            if(id>0){
                lock(_theLock)
                    if(_theField == null) 
                       _theField = ""Test Value"";
                    else
                        return;
            } 
            else
               return;
        }
        else
          return;
      }
    }
}
"
        ],
        [
            "if-lock-if (value from method) 9", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
      
      private string ComputeValue(int id) => id.ToString();
 
      public void IsOk(int id)
      {     
        if(_theField == null) {
            lock(_theLock){
                if(_theField == null) {
                   _theField = ComputeValue(id);
                }
            }
        }
      }
    }
}
"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxNotMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxCaseDisplayName))]
    public async Task QueryExpressionSyntax_NotMatched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA007Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        await test.RunAsync().ConfigureAwait(false);
    }


    private static IEnumerable<object[]> GetQueryExpressionSyntaxMatchedCases =>
    [
        [
            "if (value from constant) 1", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public string GetValue(int id)
      {     
        if(_theField == null)
           {|#0:_theField = ""Test Value""|};

        return _theField;
      }
    }
}
"
        ],
        [
            "if (value from constant) 2", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public string GetValue(int id)
      {     
        if(_theField == null) {
           {|#0:_theField = ""Test Value""|};
        }

        return _theField;
      }
    }
}
"
        ],
        [
            "lock-if (value from constant) 1", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public string GetValue(int id)
      {     
        lock(_theLock)
            if(_theField == null)
               {|#0:_theField = ""Test Value""|};

        return _theField;
      }
    }
}
"
        ],
        [
            "lock-if (value from constant) 2", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public string GetValue(int id)
      {     
        lock(_theLock) {
            if(_theField == null)
               {|#0:_theField = ""Test Value""|};
        }

        return _theField;
      }
    }
}
"
        ],
        [
            "if-lock (value from constant) 1", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public string GetValue(int id)
      {     
        if(_theField == null)
            lock(_theLock) {        
               {|#0:_theField = ""Test Value"" + id|};
        }

        return _theField;
      }
    }
}
"
        ],
        [
            "if-lock (value from constant) 2", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object();
 
      public string GetValue(int id)
      {     
        if(_theField == null)
            lock(_theLock)       
               {|#0:_theField = ""Test Value"" + id|};        

        return _theField;
      }
    }
}
"
        ],
        [
            "if (value from method) 1", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object(); 
private string ComputeValue(int id) => id.ToString();
 
      public string GetValue(int id)
      {     
        if(_theField == null)
           {|#0:_theField = ComputeValue(id)|};

        return _theField;
      }
    }
}
"
        ],
        [
            "if (value from method) 2", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object(); 
      private string ComputeValue(int id) => id.ToString();
 
      public string GetValue(int id)
      {     
        if(_theField == null) {
           {|#0:_theField = ComputeValue(id)|};
        }

        return _theField;
      }
    }
}
"
        ],
        [
            "lock-if (value from method) 1", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object(); 
private string ComputeValue(int id) => id.ToString();
 
      public string GetValue(int id)
      {     
        lock(_theLock)
            if(_theField == null)
               {|#0:_theField = ComputeValue(id)|};

        return _theField;
      }
    }
}
"
        ],
        [
            "lock-if (value from method) 2", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object(); 
      private string ComputeValue(int id) => id.ToString();
 
      public string GetValue(int id)
      {     
        lock(_theLock) {
            if(_theField == null)
               {|#0:_theField = ComputeValue(id)|};
        }

        return _theField;
      }
    }
}
"
        ],
        [
            "if-lock (value from method) 1", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object(); 
      private string ComputeValue(int id) => id.ToString();
 
      public string GetValue(int id)
      {     
        if(_theField == null)
            lock(_theLock) {        
               {|#0:_theField = ComputeValue(id)|};
        }

        return _theField;
      }
    }
}
"
        ],
        [
            "if-lock (value from method) 2", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {    
      private string _theField;
      private readonly object _theLock = new object(); 
      private string ComputeValue(int id) => id.ToString();
 
      public string GetValue(int id)
      {     
        if(_theField == null)
            lock(_theLock)       
               {|#0:_theField = ComputeValue(id)|};        

        return _theField;
      }
    }
}
"
        ],
    ];

    public static string GetQueryExpressionSyntaxCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        #pragma warning disable CA1062
        return (string)data[0];
        #pragma warning restore CA1062
    }

    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxCaseDisplayName))]
    public async Task QueryExpressionSyntax_Matched(
        string title,
        string sourceCode
        )
    {
        var test = new CSharpAnalyzerVerifier<DSA007Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA007Analyzer>.Diagnostic(DSA007Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}