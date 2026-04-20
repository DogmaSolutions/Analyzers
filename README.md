# Dogma Solutions Roslyn Analyzers

[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

A set of Roslyn Analyzers aimed to enforce some design good practices and code quality (QA) rules.

# Rules structure

This section describes the rules included in this package.

Every rule is accompanied by the following information and clues:

- **Category** → identify the area of interest of the rule, and can have one of the following values: _Design / Naming / Style / Usage / Performance / Security_
- **Severity** → state the default severity level of the rule. The severity level can be changed by editing the _.editorconfig_ file used by the project/solution. Possible values are enumerated by
  the [DiagnosticSeverity enum](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnosticseverity)
- **Description, motivations and fixes** → a detailed explanation of the detected issue, and a brief description on how to change your code in order to solve it.
- **See also** → a list of similar/related rules, or related knowledge base

# Rules list

| Id                | Category    | Description                                                                                                                                                                                                | Default severity | Is enabled | Code fix |
|-------------------|-------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------|------------|----------|
| [DSA001](#dsa001) | Design      | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression**. | ⚠ Warning        | ✅          | ❌        |
| [DSA002](#dsa002) | Design      | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ fluent query**.     | ⚠ Warning        | ✅          | ❌        |
| [DSA003](#dsa003) | Code Smells | Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty`                                                                                                                                          | ⚠ Warning        | ✅          | ❌        |
| [DSA004](#dsa004) | Code Smells | Use `DateTime.UtcNow` instead of `DateTime.Now`                                                                                                                                                            | ⚠ Warning        | ✅          | ❌        |
| [DSA005](#dsa005) | Code Smells | Potential non-deterministic point-in-time execution                                                                                                                                                        | ⛔ Error          | ✅          | ❌        |
| [DSA006](#dsa006) | Code Smells | General exceptions should not be thrown by user code                                                                                                                                                       | ⛔ Error          | ✅          | ❌        |
| [DSA007](#dsa007) | Code Smells | When initializing a lazy field, use a robust locking pattern, i.e. the "if-lock-if" (aka "double checked locking")                                                                                         | ⚠ Warning        | ✅          | ❌        |
| [DSA008](#dsa008) | Bug         | The Required Attribute has no impact on a not-nullable DateTime                                                                                                                                            | ⛔ Error          | ✅          | ❌        |
| [DSA009](#dsa009) | Bug         | The Required Attribute has no impact on a not-nullable DateTimeOffset                                                                                                                                      | ⛔ Error          | ✅          | ❌        |
| [DSA011](#dsa011) | Design      | Avoid lazily initialized, self-contained, static singleton properties                                                                                                                                      | ⚠ Warning        | ✅          | ❌        |
| [DSA012](#dsa012) | Design      | Avoid the "if not exists, then insert" check-then-act antipattern (TOCTOU)                                                                                                                                 | ⚠ Warning        | ✅          | ❌        |
| [DSA013](#dsa013) | Security    | Minimal API endpoints should have an explicit authorization configuration                                                                                                                                  | ⚠ Warning        | ✅          | ❌        |
| [DSA014](#dsa014) | Security    | Minimal API endpoints on route groups should have an explicit authorization configuration                                                                                                                   | ⚠ Warning        | ✅          | ❌        |
| [DSA015](#dsa015) | Security    | Minimal API endpoints on parameterized route builders should have an explicit authorization configuration                                                                                                   | ⚠ Warning        | ✅          | ❌        |
| [DSA016](#dsa016) | Code Smells | Avoid repeated invocation of the same enumeration method with identical arguments                                                                                                                           | ⚠ Warning        | ✅          | ❌        |

---

# DSA001

Don't use Entity Framework to launch LINQ queries in a WebApi controller.

- **Category**: Design
- **Severity**: Warning ⚠
- **Related rules**: [DSA002](#dsa002)

## Description

[WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression**.  
In the analyzed code, a [WebApi controller method](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) is
using [Entity Framework DbContext](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext) to directly manipulate data through a LINQ query expression.  
WebApi controllers should not contain data-manipulation business logics.

## See also

This is a typical violation of the ["Single Responsibility" rule](https://en.wikipedia.org/wiki/Single-responsibility_principle) of the ["SOLID" principles](https://en.wikipedia.org/wiki/SOLID),
because the controller is doing too many things outside its own purpose.

## Fix / Mitigation

In order to fix the problem, the code could be modified in order to rely on the ["Indirection pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Indirection) and maximize
the ["Low coupling evaluative pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Low_coupling) of the ["GRASP"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design))
principles.
Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA001: WebApi controller methods should not contain data-manipulation business logics through a LINQ query expression.
dotnet_diagnostic.DSA001.severity = error
```

## Code sample

```csharp
public class MyEntitiesController : ControllerBase
{
 protected MyDbContext DbContext { get; }

 public MyEntitiesController(MyDbContext dbContext)
 {
     DbContext = dbContext;
 }

 [HttpGet]
 public IEnumerable<MyEntity> GetAll_NotOk()
 {
     // this WILL trigger the rule
     var query = from entities in DbContext.MyEntities where entities.Id > 0 select entities;
     return query.ToList(); 
 }

 [HttpPost]
 public IEnumerable<long> GetAll_Ok()
 {
     // this WILL NOT trigger the rule
     var query = DbContext.MyEntities.Where(entities => entities.Id > 0).Select(entities=>entities.Id);
     return query.ToList(); 
 }
}
```

---

# DSA002

Don't use an Entity Framework `DbSet` to launch queries in a WebApi controller.

- **Category**: Design
- **Severity**: Warning ⚠
- **Related rules**: [DSA001](#dsa001)

## Description

[WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ fluent query**.  
In the analyzed code, a [WebApi controller method](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) is
using [Entity Framework DbSet](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbset-1) to directly manipulate data through a LINQ fluent query.  
WebApi controllers should not contain data-manipulation business logics.

## See also

This is a typical violation of the ["Single Responsibility" rule](https://en.wikipedia.org/wiki/Single-responsibility_principle) of the ["SOLID" principles](https://en.wikipedia.org/wiki/SOLID),
because the controller is doing too many things outside its own purpose.

## Fix / Mitigation

In order to fix the problem, the code could be modified in order to rely on the ["Indirection pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Indirection) and maximize
the ["Low coupling evaluative pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Low_coupling) of the ["GRASP"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design))
principles.   
Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA002: WebApi controller methods should not contain data-manipulation business logics through a LINQ fluent query.
dotnet_diagnostic.DSA002.severity = error
```

## Code sample

```csharp
public class MyEntitiesController : Microsoft.AspNetCore.Mvc.ControllerBase
{
 protected MyDbContext DbContext { get; }

 public MyEntitiesController(MyDbContext dbContext)
 {
     this.DbContext = dbContext;
 }

 [HttpGet]
 public IEnumerable<MyEntity> GetAll0()
 {
     // this WILL NOT trigger the rule
     var query = from entities in DbContext.MyEntities where entities.Id > 0 select entities;
     return query.ToList(); 
 }

 [HttpPost]
 public IEnumerable<long> GetAll1()
 {
     // this WILL trigger the rule
     var query = DbContext.MyEntities.Where(entities => entities.Id > 0).Select(entities=>entities.Id);
     return query.ToList(); 
 }
}
```

---

# DSA003

Use `IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty`.

- **Category**: Code smells
- **Severity**: Warning ⚠

## Description

Usually, business logics distinguish between "string with content", and "string NULL or without meaningful content".  
Thus, statistically speaking, almost every call to `string.IsNullOrEmpty` could or should be replaced by a call to `string.IsNullOrWhiteSpace`, because in the large majority of cases, a string
composed by only spaces, tabs, and return chars is not considered valid because it doesn't have "meaningful content".  
In most cases, `string.IsNullOrEmpty` is used by mistake, or has been written when `string.IsNullOrWhiteSpace` was not available.

## Fix / Mitigation

Don't use `string.IsNullOrEmpty`. Use `string.IsNullOrWhiteSpace` instead.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA003: Use String.IsNullOrWhiteSpace instead of String.IsNullOrEmpty
dotnet_diagnostic.DSA003.severity = error
```

## Code sample

```csharp
public class MyClass
{

 public bool IsOk(string s)
 {
     // this WILL NOT trigger the rule
     return string.IsNullOrWhiteSpace(s);
 }

 public bool IsNotOk(string s)
 {
     // this WILL trigger the rule
     return string.IsNullOrEmpty(s);
 }

}
```

---

# DSA004

Use `DateTime.UtcNow` instead of `DateTime.Now`.

- **Category**: Code smells
- **Severity**: Warning ⚠

## Description

Using `DateTime.Now` into business logics potentially leads to many different problems:

- Incoherence between nodes or processes running in different timezones (even in the same country, i.e. USA, Russia, China, etc)
- Unexpected behaviours in-between legal time changes
- Code conversion problems and loss of timezone info when saving/loading data to/from a datastore

## See also

Security-wise, this is correlated to the CWE category “7PK” ([CWE-361](https://cwe.mitre.org/data/definitions/361.html))  
Cit:
*"This category represents one of the phyla in the Seven Pernicious Kingdoms vulnerability classification. It includes weaknesses related to the improper management of time and state in an environment
that supports simultaneous or near-simultaneous computation by multiple systems, processes, or threads. According to the authors of the Seven Pernicious Kingdoms, "Distributed computation is about
time and state. That is, in order for more than one component to communicate, state must be shared, and all that takes time. Most programmers anthropomorphize their work. They think about one thread
of control carrying out the entire program in the same way they would if they had to do the job themselves. Modern computers, however, switch between tasks very quickly, and in multi-core, multi-CPU,
or distributed systems, two events may take place at exactly the same time. Defects rush to fill the gap between the programmer's model of how a program executes and what happens in reality. These
defects are related to unexpected interactions between threads, processes, time, and information. These interactions happen through shared state: semaphores, variables, the file system, and,
basically, anything that can store information."*

## Fix / Mitigation

Don't use `DateTime.Now`. Use `DateTime.UtcNow` instead

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA004: Use DateTime.UtcNow instead of DateTime.Now
dotnet_diagnostic.DSA004.severity = error
```

## Code sample

```csharp
public class MyClass
{

 public DateTime IsOk()
 {
     // this WILL NOT trigger the rule
     return DateTime.UtcNow;
 }

 public DateTime IsNotOk()
 {
     // this WILL trigger the rule
     return DateTime.Now;
 }

}
```

---

# DSA005

Potential non-deterministic point-in-time execution due to multiple usages of `DateTime.UtcNow` or `DateTime.Now` in the same method.

- **Category**: Code smells
- **Severity**: Error ⛔

## Description

An execution flow must always be as deterministic as possible.
This means that all decisions inside a scope or algorithm must be performed on a "stable" and immutable set of parameters/conditions.
When dealing with dates and times, always ensure that the point-in-time reference is fixed, otherwise the algorithm would work on a "sliding window", leading to unpredictable results.
This is particularly impacting in:

- datasource-dependent flows
- slow-running algorithms
- and in-between legal time changes.

## See also

Security-wise, this is correlated to the CWE category “7PK” ([CWE-361](https://cwe.mitre.org/data/definitions/361.html))  
Cit:
*"This category represents one of the phyla in the Seven Pernicious Kingdoms vulnerability classification. It includes weaknesses related to the improper management of time and state in an environment
that supports simultaneous or near-simultaneous computation by multiple systems, processes, or threads. According to the authors of the Seven Pernicious Kingdoms, "Distributed computation is about
time and state. That is, in order for more than one component to communicate, state must be shared, and all that takes time. Most programmers anthropomorphize their work. They think about one thread
of control carrying out the entire program in the same way they would if they had to do the job themselves. Modern computers, however, switch between tasks very quickly, and in multi-core, multi-CPU,
or distributed systems, two events may take place at exactly the same time. Defects rush to fill the gap between the programmer's model of how a program executes and what happens in reality. These
defects are related to unexpected interactions between threads, processes, time, and information. These interactions happen through shared state: semaphores, variables, the file system, and,
basically, anything that can store information."*

## Fix/Mitigation

In order to avoid problems, apply one of these, depending on the situation:

- When measuring elapsed time, use a `StopWatch.StartNew()` combined with `StopWatch.Elapsed`
- When NOT measuring elapsed time, set a `var now = DateTime.UtcNow` variable at the top of the method, or at the beginning of an execution flow/algorithm, and reuse that variable in all places
  instead of `DateTime.***Now`.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA005: Potential non-deterministic point-in-time execution
dotnet_diagnostic.DSA005.severity = error
```

## Code sample

```csharp
public class MyClass
{

 public bool IsOk(string s)
 {     
     var now = DateTime.UtcNow; // fixed point-in-time reference
     
     DoSomething(now); // this WILL NOT trigger the rule
     
     for(int i; i < 10; i++)
     {
       DoOtherThings(now);  // this WILL NOT trigger the rule
     }
 }

 public bool IsNotOk(string s)
 {   
     DoSomething(DateTime.UtcNow); // this WILL trigger the rule
     
     for(int i; i < 10; i++)
     {
       DoOtherThings(DateTime.UtcNow);  // this WILL trigger the rule
     }
 }

}
```

---

# DSA006

General exceptions should not be thrown by user code.

- **Category**: Code smells
- **Severity**: Error ⛔

## Description

General exceptions should never be thrown, because throwing them prevents calling methods from discriminating between system-generated exceptions, and application-generated errors.  
This is a bad smell, and could lead to stability and security concerns.  
General exceptions that trigger this rule are:

- `Exception`
- `SystemException`
- `ApplicationException`
- `IndexOutOfRangeException`
- `NullReferenceException`
- `OutOfMemoryException`
- `ExecutionEngineException`

## See also

Security-wise, this is correlated to [MITRE, CWE-397 - Declaration of Throws for Generic Exception](https://cwe.mitre.org/data/definitions/397)

## Fix/Mitigation

Use scenario-specific exceptions, i.e. `ArgumentException`, `ArgumentNullException`, `InvalidOperationException`, etc.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA006: General exceptions should never be thrown. 
dotnet_diagnostic.DSA006.severity = error
```

## Code sample

```csharp
public class MyClass
{
    
    public void IsOk(int id)
    {     
      if(id < 0) // this is OK, and will NOT be matched by the rule
        throw new ArgumentException(nameof(id),"Invalid id");
    }

    public void IsNotOk(int id)
    {     
      if(id < 0) // this is NOT OK, and will be matched by the rule
        throw new SystemException("Invalid id");
    }

}
```

---

# DSA007

When initializing a lazy field (and in particular fields containing the instance of a singleton object), use a robust locking pattern, i.e. the “if-lock-if” (aka “double checked locking”)

- **Category**: Code smells
- **Severity**: Warning ⚠

## Description

Cit. Wikipedia:  
*The "double-checked locking" (also known as "double-checked locking optimization") is a software design pattern used to reduce the overhead of acquiring a lock by testing the locking criterion (the "
lock hint") before acquiring the lock.
Locking occurs only if the locking criterion check indicates that locking is required.
The pattern is typically used to reduce locking overhead when implementing "lazy initialization" in a multi-threaded environment, especially as part of the Singleton pattern.
Lazy initialization avoids initializing a value until the first time it is accessed.*

## See also

- [Wikipedia: Double-checked_locking](https://en.wikipedia.org/wiki/Double-checked_locking)
- [Microsoft Documentation: Managed Threading Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/threading/managed-threading-best-practices)
- [MITRE, CWE-667: Improper Locking (4.16)](https://cwe.mitre.org/data/definitions/667.html)
- [MITRE, CWE-413: Improper Resource Locking (4.16)](https://cwe.mitre.org/data/definitions/413.html)

## Fix/Mitigation

Instead of just writing something like this....

```cs
public class MyClass 
{    
  private string _theField;
  private readonly object _theLock = new object();

  public void IsOk(int id)
  {     
    lock(_theLock){ // ❌ too early, very wasteful, poor performances
        if(_theField == null) { 
           _theField = ComputeExpensiveValue(id);
        }
    }
  }
}
```

... or something like this....

```cs
public class MyClass 
{    
  private string _theField;
  private readonly object _theLock = new object();

  public void IsOk(int id)
  {     
    if(_theField == null) { 
        lock(_theLock){ // ❌ too late, and thread-unsafe      
           _theField = ComputeExpensiveValue(id); // ⚠ this could be executed multiple times !
        }
    }
  }
}
```

... use the following *if-lock-if* pattern:

```cs
public class MyClass 
{    
  private string _theField;
  private readonly object _theLock = new object();

  public void IsOk(int id)
  {     
    if(_theField == null) { // ✅ efficient and fast pre-check (few nanoseconds)
        lock(_theLock){ // ✅ protects against race conditions and multithreading
            if(_theField == null) { // ✅ only if really needed, safely initialize
               _theField = ComputeExpensiveValue(id); // ✅ guaranteed to be executed only once
            }
        }
    }
  }
}
```

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA007: Use the double-checked lazy initialization pattern
dotnet_diagnostic.DSA007.severity = warning
```

---

# DSA008

The [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute) has no impact on a
not-nullable [DateTime](https://learn.microsoft.com/it-it/dotnet/api/system.datetime) property.

- **Category**: Bug
- **Severity**: ⛔ Error

## Description

It is a common misunderstanding that the [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute) is somehow able to validate a
not-nullable [DateTime](https://learn.microsoft.com/it-it/dotnet/api/system.datetime) property.  
In reality, not-nullable, not-string types are ignored by the [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute), so it doesn't
make any sense to use it in this context.

## Fix/Mitigation

Remove the [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute), or make the property nullable.
If a "valid date"-like validation is needed, use [Range Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.rangeattribute).

## See also

- [DSA009](#dsa009) - The Required Attribute has no impact on a not-nullable DateTimeOffset
- [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute)
- [Range Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.rangeattribute)
- [DateTime](https://learn.microsoft.com/it-it/dotnet/api/system.datetime)
- [MITRE, CWE-20: Improper Input Validation](https://cwe.mitre.org/data/definitions/20.html)

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA008: The Required Attribute has no impact on a not-nullable DateTime
dotnet_diagnostic.DSA008.severity = warning
```

---

# DSA009

The [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute) has no impact on a
not-nullable [DateTimeOffset](https://learn.microsoft.com/it-it/dotnet/api/system.DateTimeOffset) property.

- **Category**: Bug
- **Severity**: ⛔ Error

## Description

It is a common misunderstanding that the [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute) is somehow able to validate a
not-nullable [DateTimeOffset](https://learn.microsoft.com/it-it/dotnet/api/system.DateTimeOffset) property.  
In reality, not-nullable, not-string types are ignored by the [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute), so it doesn't
make any sense to use it in this context.

## Fix/Mitigation

Remove the [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute), or make the property nullable.
If a "valid date"-like validation is needed, use [Range Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.rangeattribute).

## See also

- [DSA008](#dsa008) - The Required Attribute has no impact on a not-nullable DateTime
- [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute)
- [Range Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.rangeattribute)
- [DateTimeOffset](https://learn.microsoft.com/it-it/dotnet/api/system.DateTimeOffset)
- [MITRE, CWE-20: Improper Input Validation](https://cwe.mitre.org/data/definitions/20.html)

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA009: The Required Attribute has no impact on a not-nullable DateTimeOffset
dotnet_diagnostic.DSA009.severity = warning
```

---

# DSA011

Avoid lazily initialized, self-contained, static singleton properties

- **Category**: Design
- **Severity**: ⚠ Warning

## Description
The [Singleton Pattern](https://en.wikipedia.org/wiki/Singleton_pattern) is subject of many controversies. Technically, there is nothing wrong with it, but its usefulness and robustness is very implementation-dependent, and in some cases it's seen as an anti-pattern. 

A good strategy to make use of this pattern, is to use an IoC/DI framework that ensures proper thread-safeness, dependencies management, and resources allocation/deallocation. 
```cs
// Good/Safe implementation, based on Microsoft IoC/DI
services.AddSingleton<IMyService, MyService>();  
```

A simpler but **very problematic** strategy relies on directly exposing a public static property in the singleton class, like this.
```cs
public class MyClass
{
    private static MyClass _instance;
    
    // Bad practice: 
    // - fragile, because lacks proper locking
    // - badly designed, because forces the caller to "know" the implementor class
    public static MyClass Instance => _instance??=new MyClass();
}
```

Self-contained static singleton properties, particularly when they involve lazy initialization within the property itself, can lead to several problems, especially in multithreaded environments.
Due to their static nature, they are also difficult to test, and could manifest unpredictable results if the testing framework (or the tests) doesn't clean the static instances in-between sessions. Also, they force the caller to know the implementor class, instead of just an abstraction (i.e. an interface implemented by the singleton class).

This analyzer aims to find occurrences of this kind of ill-conceived implementations.  
The following patterns are matched:

```cs
public class MyClass
{
    private static MyClass _instance;

    public static MyClass Instance
    {
        get
        {
            if (_instance == null)
                _instance = new MyClass();
            return _instance;
        }
    }
}
```

```cs
public class MyClass
{
    private static MyClass _instance;

    public static MyClass Instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            
            _instance = new MyClass();
            return _instance;
        }
    }
}
```

```cs
public class MyClass
{
    private static MyClass _instance;

    public static MyClass Instance => _instance??=new MyClass();
}
```

## Fix/Mitigation

Use an IoC/DI framework instead, or at least use proper locking when initializing the instance.

## See also

- [Singletons Are Evil](https://wiki.c2.com/?SingletonsAreEvil)
- [Singleton Pattern](https://en.wikipedia.org/wiki/Singleton_pattern)
- [MITRE, CWE-543: Use of Singleton Pattern Without Proper Synchronization in a Multithreaded Context](https://cwe.mitre.org/data/definitions/543.html)
- [MITRE, CWE-362: Concurrent Execution using Shared Resource with Improper Synchronization](https://cwe.mitre.org/data/definitions/362.html)

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA011: Avoid lazily initialized, self-contained, static singleton properties
dotnet_diagnostic.DSA011.severity = warning
```

---

# DSA012

Avoid the "if not exists, then insert" check-then-act antipattern (TOCTOU).

- **Category**: Design
- **Severity**: Warning ⚠
- **Related rules**: [DSA005](#dsa005)

## Description
The _"if not exists, then insert"_ pattern (also known as "check-then-act") is a non-atomic sequence that first checks whether a record exists and then, based on the result, inserts a new one.  
This pattern **is not a bad thing per-se**, but _suggests_ (or at least gives the suspicion) that the coherence of the data is _only_ handled by application-level logics which, if true, can lead to undesired effects.

## Rationale
Since the database is usually the _Single Source of Truth_ for the data, then the uniqueness and semantic consistency of such data must be guaranteed at database-level, not at application-level (or at least not _only_ at application-level).
If the DB is the _Single Source of Truth_ and guarantees the semantic consistency of the data, then a _"if not exists, then insert"_ pattern shouldn't be necessary at all, unless the developer wanted to be proactive and provide to the caller a user-friendly message.

## Potential problems
If the DB is not the _Single Source of Truth_, this pattern leads to false confidence, because it's prone to [TOCTOU (Time-of-Check to Time-of-Use)](https://en.wikipedia.org/wiki/Time-of-check_to_time-of-use) race conditions: between the moment the existence check completes and the insert executes, another thread or process could insert the same record, leading to duplicate entries and data corruption.

This is particularly dangerous in database operations when no `UNIQUE` constraint is in place. Without such a constraint, the only safeguard against duplication is the application-level check, which is inherently non-atomic and unreliable under concurrent access.

## Matched patterns

The following patterns are matched:

```cs
// Pattern A: negated existence check + insert
if (!items.Any(x => x.Id == id))
{
    items.Add(newItem);
}

// Pattern B: existence check + throw, followed by insert
if (items.Any(x => x.Id == id))
    throw new ConflictException("Already exists");
items.Add(newItem);

// Pattern C: existence check + else insert
if (items.Any(x => x.Id == id))
{
    // update or other logic
}
else
{
    items.Add(newItem);
}
```

Variants using `Count(...) == 0`, `FirstOrDefault(...) == null`, `Contains(...)`, and their async counterparts are also detected.

## See also

- [TOCTOU - Time-of-Check to Time-of-Use](https://en.wikipedia.org/wiki/Time-of-check_to_time-of-use)
- [MITRE, CWE-367: Time-of-check Time-of-use (TOCTOU) Race Condition](https://cwe.mitre.org/data/definitions/367.html)
- [EAFP - Easier to Ask for Forgiveness than Permission](https://devblogs.microsoft.com/python/idiomatic-python-eafp-versus-lbyl/)
- [OWASP: Race Conditions](https://owasp.org/www-community/vulnerabilities/Race_condition)

## When to ignore this rule
If the `if` in the code is **ONLY** a precaution you added to proactively handle errors (e.g. to show a more user-friendly message), **AND** the database is protected by a `UNIQUE` constraint or other mechanisms that guarantee data consistency and uniqueness, then you can explicitly ignore this warning with `#pragma warning disable DSA012`.

Otherwise, it's almost guaranteed that this is an issue to handle, and you shouldn't ignore it.

## Fix / Mitigation

In order to fix the problem, apply one of the following approaches depending on the situation:

- **Atomic upsert**: Use a database-level atomic operation such as SQL `MERGE`, `INSERT ... ON CONFLICT DO NOTHING/UPDATE` (PostgreSQL), or `INSERT ... ON DUPLICATE KEY UPDATE` (MySQL).
- **UNIQUE constraint**: Add a `UNIQUE` constraint to the database so that duplicate inserts are rejected at the database level, regardless of application-level checks.
- **EAFP approach**: Attempt the insert directly and catch the resulting exception (e.g., `DbUpdateException` for a unique constraint violation) instead of checking beforehand.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA012: Avoid the "if not exists, then insert" check-then-act antipattern
dotnet_diagnostic.DSA012.severity = error
```

## Code sample

```csharp
public class MyService
{
    private readonly MyDbContext _dbContext;

    public void AddItem_NotOk(string name)
    {
        // this WILL trigger the rule: non-atomic check-then-act (suspicious, leads
        // into thinking that the DB is not taking care of the uniqueness of the data on its own).
        if (!_dbContext.Items.Any(x => x.Name == name))
        {
            _dbContext.Items.Add(new Item { Name = name });
            _dbContext.SaveChanges();
        }
    }

    public void AddItem_Ok(string name)
    {
        // this WILL NOT trigger the rule: EAFP approach
        try
        {
            _dbContext.Items.Add(new Item { Name = name });
            
            // Assumes that there IS a UNIQUE constraint on Name column taking care of the uniqueness.
            _dbContext.SaveChanges(); 
        }
        catch (DbUpdateException)
        {
            // Handle duplicate gracefully
        }
    }
}
```

---

# DSA013

Minimal API endpoints should have an explicit authorization configuration.

- **Category**: Security
- **Severity**: Warning ⚠
- **Related rules**: [DSA014](#dsa014), [DSA015](#dsa015)

## Description

This rule fires when a Minimal API endpoint (`MapGet`, `MapPost`, `MapPut`, `MapDelete`, `MapPatch`, `MapMethods`, or `Map`) is called on a **local (non-parameter) `IEndpointRouteBuilder`** without `.RequireAuthorization()` or `.AllowAnonymous()` in its fluent chain.

Without an explicit authorization configuration, the endpoint silently defaults to **anonymous access**, creating an unauthenticated attack surface. Every endpoint should make a conscious, reviewable authorization decision.

This rule has the highest confidence level among the three authorization rules: the builder is a local variable (not received from a caller), and it is not a `RouteGroupBuilder` (no group-level inheritance to consider). If auth is missing from the chain, it is almost certainly a bug.

For endpoints on `RouteGroupBuilder`, see [DSA014](#dsa014).  
For endpoints on `IEndpointRouteBuilder` received as a method parameter, see [DSA015](#dsa015).

## See also

- [MITRE, CWE-862: Missing Authorization](https://cwe.mitre.org/data/definitions/862.html)
- [ASP.NET Core Minimal APIs - Authorization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security)
- [OWASP: Broken Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)

## Matched patterns

```cs
var app = WebApplication.Create();

// direct endpoint without auth
app.MapGet("/api/items", GetItemsAsync);

// fluent chain without auth
app.MapGet("/api/items", GetItemsAsync)
    .WithName("GetItems")
    .Produces<List<Item>>(StatusCodes.Status200OK);
```

## Fix / Mitigation

Add `.RequireAuthorization()` or `.AllowAnonymous()` directly to the endpoint:

```cs
// explicit auth
app.MapGet("/api/items", GetItemsAsync)
    .RequireAuthorization();  // ✅

// explicit anonymous access (conscious decision)
app.MapGet("/public/health", HealthCheckAsync)
    .AllowAnonymous();  // ✅
```

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA013: Minimal API endpoints should have an explicit authorization configuration
dotnet_diagnostic.DSA013.severity = error
```

## Code sample

```csharp
public class Program
{
    public static void Main()
    {
        var app = WebApplication.Create();

        // this WILL trigger the rule
        app.MapGet("/api/items", GetItems)
            .WithName("GetItems")
            .Produces<List<DataItem>>(StatusCodes.Status200OK);

        // this WILL NOT trigger the rule
        app.MapGet("/api/items", GetItems)
            .WithName("GetItems")
            .Produces<List<DataItem>>(StatusCodes.Status200OK)
            .RequireAuthorization();
    }
}
```

---

# DSA014

Minimal API endpoints on route groups should have an explicit authorization configuration.

- **Category**: Security
- **Severity**: Warning ⚠
- **Related rules**: [DSA013](#dsa013), [DSA015](#dsa015)

## Description

This rule fires when a Minimal API endpoint is called on a **`RouteGroupBuilder`** (obtained via `MapGroup`) and neither the endpoint's fluent chain nor the route group (or any ancestor group) carries `.RequireAuthorization()` or `.AllowAnonymous()`.

The analyzer checks multiple levels:

1. **Endpoint chain**: `.RequireAuthorization()` or `.AllowAnonymous()` on the `MapGet`/`MapPost`/etc. call itself.
2. **Local group auth**: auth on the group's `MapGroup()` chain or as a separate statement in the same method.
3. **Nested group ancestry**: auth inherited from a parent group (e.g., outer group has auth, inner group inherits).
4. **Cross-method tracing**: when the `RouteGroupBuilder` is received as a method parameter, the analyzer searches the compilation for all call sites and verifies that every caller passes a group with authorization configured.

## See also

- [MITRE, CWE-862: Missing Authorization](https://cwe.mitre.org/data/definitions/862.html)
- [ASP.NET Core Minimal APIs - Authorization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security)
- [OWASP: Broken Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)

## Matched patterns

```cs
// Pattern A: group without auth, endpoint without auth
var group = builder.MapGroup("/api");
group.MapGet("/items", GetItemsAsync); // ❌

// Pattern B: nested groups, neither has auth
var api = builder.MapGroup("/api");
var v1 = api.MapGroup("/v1");
v1.MapGet("/items", GetItemsAsync); // ❌

// Pattern C: group parameter, call site has no auth
public static void MapItems(RouteGroupBuilder group)
{
    group.MapGet("/items", GetItemsAsync); // ❌ if caller doesn't authorize
}
```

## Fix / Mitigation

Add auth to the endpoint, the group, or ensure all callers pass an authorized group:

```cs
// Fix 1: auth on the endpoint itself
group.MapGet("/items", GetItemsAsync)
    .RequireAuthorization();  // ✅

// Fix 2: auth on the group (inline)
var group = builder.MapGroup("/api").RequireAuthorization();  // ✅
group.MapGet("/items", GetItemsAsync);  // covered

// Fix 3: auth on the group (separate statement)
var group = builder.MapGroup("/api");
group.RequireAuthorization();  // ✅
group.MapGet("/items", GetItemsAsync);  // covered

// Fix 4: auth at the call site for parameterized groups
var api = builder.MapGroup("/api").RequireAuthorization();
MapItems(api);  // ✅ caller provides authorized group
```

## When to ignore this rule

If the `RouteGroupBuilder` is received as a parameter and the method is part of a public API consumed by external assemblies (where call sites are not visible to the analyzer), you may suppress this rule with `#pragma warning disable DSA014` and document that callers are expected to provide an authorized group.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA014: Minimal API endpoints on route groups should have an explicit authorization configuration
dotnet_diagnostic.DSA014.severity = error
```

## Code sample

```csharp
public class Startup
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        // this WILL trigger the rule: group has no auth
        var unprotected = builder.MapGroup("/api");
        unprotected.MapGet("/items", GetItemsAsync);

        // this WILL NOT trigger the rule: group has auth
        var secured = builder.MapGroup("/api").RequireAuthorization();
        secured.MapGet("/items", GetItemsAsync);
    }
}
```

---

# DSA015

Minimal API endpoints on parameterized route builders should have an explicit authorization configuration.

- **Category**: Security
- **Severity**: Warning ⚠
- **Related rules**: [DSA013](#dsa013), [DSA014](#dsa014)

## Description

This rule fires when a Minimal API endpoint is called on an **`IEndpointRouteBuilder` received as a method parameter** (not a `RouteGroupBuilder` — that is handled by [DSA014](#dsa014)) without `.RequireAuthorization()` or `.AllowAnonymous()` in its fluent chain.

Since the builder comes from the caller, authorization may be configured at the call site. The analyzer performs **cross-method tracing**: it searches the entire compilation for all invocations of the method and verifies that every call site passes a builder with authorization configured. This includes:

- Arguments with inline auth (e.g., `group.RequireAuthorization()`)
- Local variables whose declaration or separate statements carry auth
- Nested group ancestry (auth inherited from parent groups)
- Recursive pass-through (parameter passed through multiple method layers)

If authorization cannot be confirmed at every call site, the endpoint is flagged.

**Note**: if no call sites are found in the compilation (e.g., the method is a public API consumed by an external assembly), the rule flags the endpoint. In this case, add auth directly to the endpoint or suppress with `#pragma warning disable DSA015`.

## See also

- [MITRE, CWE-862: Missing Authorization](https://cwe.mitre.org/data/definitions/862.html)
- [ASP.NET Core Minimal APIs - Authorization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security)
- [OWASP: Broken Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)

## Matched patterns

```cs
// Pattern A: extension method, no call sites or call sites lack auth
public static void MapItems(this IEndpointRouteBuilder builder)
{
    builder.MapGet("/items", GetItemsAsync); // ❌
}

// Pattern B: call site passes unauthenticated builder
public static void MapItems(IEndpointRouteBuilder builder)
{
    builder.MapGet("/items", GetItemsAsync); // ❌
}

var app = GetBuilder();
MapItems(app); // no auth on app
```

## Fix / Mitigation

Add auth directly to the endpoint, or ensure all callers provide an authorized builder:

```cs
// Fix 1: auth on the endpoint itself
public static void MapItems(this IEndpointRouteBuilder builder)
{
    builder.MapGet("/items", GetItemsAsync)
        .RequireAuthorization();  // ✅
}

// Fix 2: ensure all callers pass authorized builders
var group = builder.MapGroup("/api").RequireAuthorization();
group.MapItems();  // ✅ group has auth
```

## When to ignore this rule

If the method is part of a public API or a shared library consumed by external assemblies (where call sites are not visible to the analyzer), and you have ensured that all external callers provide an authorized builder, you may suppress this rule with `#pragma warning disable DSA015`.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA015: Minimal API endpoints on parameterized route builders should have an explicit authorization configuration
dotnet_diagnostic.DSA015.severity = error
```

## Code sample

```csharp
public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapDataItems(this IEndpointRouteBuilder builder)
    {
        // this WILL trigger the rule if any call site lacks auth
        builder.MapGet("/api/items", GetItems)
            .WithName("GetItems")
            .Produces<List<DataItem>>(StatusCodes.Status200OK);

        // this WILL NOT trigger the rule: explicit authorization
        builder.MapGet("/api/items", GetItems)
            .WithName("GetItems")
            .Produces<List<DataItem>>(StatusCodes.Status200OK)
            .RequireAuthorization();

        return builder;
    }
}
```

---

# DSA016

Avoid repeated invocation of the same enumeration method with identical arguments.

- **Category**: Code Smells
- **Severity**: Warning ⚠
- **Related rules**: [DSA005](#dsa005)

## Description

This rule fires when the same LINQ/enumeration method is called **multiple times** on the **same receiver** with the **same arguments** within the **same scope** (method body, lambda body, or local function body).

Each redundant call re-enumerates the source, which causes two distinct problems:

1. **Performance**: if the source is backed by a database query, a network stream, or a large in-memory collection, each call repeats the full scan. For N elements and K duplicate calls, the work becomes O(N * K) instead of O(N).
2. **Non-determinism**: if the source is a deferred `IEnumerable<T>` (e.g., a LINQ query, a generator, or a stream), consecutive enumerations may return different results. The duplicate calls could see different data, leading to inconsistent state within the same object or method.

The analyzer tracks the following method families:
- **Element access**: `First`, `FirstOrDefault`, `Single`, `SingleOrDefault`, `Last`, `LastOrDefault`, `ElementAt`, `ElementAtOrDefault`, `Find`
- **Boolean checks**: `Any`, `All`, `Contains`, `Exists`
- **Counting**: `Count`, `LongCount`
- **Aggregation**: `Min`, `Max`, `Sum`, `Average`, `Aggregate`
- **Async variants**: all of the above with the `Async` suffix

Each scope (method body, lambda, local function) is analyzed independently; invocations in nested lambdas are not compared with invocations in the outer scope.

## See also

- [CA1851: Possible multiple enumerations of IEnumerable collection](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1851)
- [DSA005: Potential non-deterministic point-in-time execution](#dsa005) (similar concept for `DateTime.Now`)

## Matched patterns

The following patterns are matched:

```cs
// Pattern A: same FirstOrDefault with same predicate called multiple times in a lambda
var result = orders.Select(o => new
{
    Line = orderLines.FirstOrDefault(l => l.OrderId == o.OrderId)?.Description,    // ❌
    Qty  = orderLines.FirstOrDefault(l => l.OrderId == o.OrderId)?.Quantity,        // ❌
    Unit = orderLines.FirstOrDefault(l => l.OrderId == o.OrderId)?.UnitOfMeasure,   // ❌
});

// Pattern B: same Count() called twice in a method body
var count1 = items.Count();  // ❌
var count2 = items.Count();  // ❌

// Pattern C: same Any with same predicate
var exists1 = items.Any(x => x.Id == id);  // ❌
var exists2 = items.Any(x => x.Id == id);  // ❌

// Pattern D: same Min/Max/Sum/Average called twice
var min1 = values.Min();  // ❌
var min2 = values.Min();  // ❌

// Pattern E: conditional access ?.FirstOrDefault called twice
var name = items?.FirstOrDefault(x => x.Id == id)?.Name;    // ❌
var code = items?.FirstOrDefault(x => x.Id == id)?.Code;    // ❌

// Pattern F: chained receiver
var a = items.Where(x => x.Active).FirstOrDefault(x => x.Id == id);  // ❌
var b = items.Where(x => x.Active).FirstOrDefault(x => x.Id == id);  // ❌

// Pattern G: Contains with same argument
var has1 = items.Contains(value);  // ❌
var has2 = items.Contains(value);  // ❌
```

## Not matched patterns

The following patterns are NOT matched:

```cs
// Different predicates on the same receiver
var a = items.FirstOrDefault(x => x.Id == id);
var b = items.FirstOrDefault(x => x.Name == name);  // ✅ different predicate

// Same predicate on different receivers
var a = items1.FirstOrDefault(x => x.Id == id);
var b = items2.FirstOrDefault(x => x.Id == id);  // ✅ different receiver

// Different methods on the same receiver (Any vs FirstOrDefault)
var exists = items.Any(x => x.Id == id);
var item = items.FirstOrDefault(x => x.Id == id);  // ✅ different method

// Same invocation in different scopes (method body vs nested lambda)
var a = items.FirstOrDefault(x => x.Id == 1);          // scope: method body
var results = ids.Select(id =>
    items.FirstOrDefault(x => x.Id == 1));              // ✅ scope: lambda (separate)

// Same invocation in two separate lambdas (each is its own scope)
Action a1 = () => { var x = items.FirstOrDefault(x => x.Id == 1); };  // scope: lambda 1
Action a2 = () => { var x = items.FirstOrDefault(x => x.Id == 1); };  // ✅ scope: lambda 2

// Count with vs without predicate (different argument signatures)
var total = items.Count();
var filtered = items.Count(x => x.Id > 0);  // ✅ different arguments

// Non-tracked methods (ToString, custom methods, etc.)
var s1 = value.ToString();
var s2 = value.ToString();  // ✅ not a tracked enumeration method

// Called only once
var item = items.FirstOrDefault(x => x.Id == id);  // ✅ single invocation
```

## Fix / Mitigation

Extract the result of the enumeration method into a local variable and reuse it:

```cs
// Fix for Pattern A:
var result = orders.Select(o =>
{
    var line = orderLines.FirstOrDefault(l => l.OrderId == o.OrderId);  // ✅ once
    return new
    {
        Line = line?.Description,
        Qty  = line?.Quantity,
        Unit = line?.UnitOfMeasure,
    };
});

// Fix for Pattern B:
var count = items.Count();  // ✅ once
// use 'count' wherever needed

// Fix for Pattern C:
var exists = items.Any(x => x.Id == id);  // ✅ once
// use 'exists' wherever needed
```

## When to ignore this rule

If the collection is known to be modified between the two calls and re-enumeration is intentional, you may suppress this rule with `#pragma warning disable DSA016`. However, in most cases, modifying a collection between two identical queries suggests a design issue that should be addressed.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA016: Avoid repeated invocation of the same enumeration method with identical arguments
dotnet_diagnostic.DSA016.severity = error
```

## Code sample

```csharp
public class OrderService
{
    public object BuildSummary(IEnumerable<Order> orders, IEnumerable<OrderLine> lines)
    {
        // this WILL trigger the rule: FirstOrDefault called 3 times
        // with the same predicate on the same receiver
        var summary = orders.Select(o => new
        {
            Description = lines.FirstOrDefault(l => l.OrderId == o.Id)?.Description,
            Quantity = lines.FirstOrDefault(l => l.OrderId == o.Id)?.Quantity,
            Price = lines.FirstOrDefault(l => l.OrderId == o.Id)?.UnitPrice,
        });

        // this WILL NOT trigger the rule: result extracted into a variable
        var summaryFixed = orders.Select(o =>
        {
            var line = lines.FirstOrDefault(l => l.OrderId == o.Id);
            return new
            {
                Description = line?.Description,
                Quantity = line?.Quantity,
                Price = line?.UnitPrice,
            };
        });

        return summaryFixed;
    }
}
```

---

# Installation

Just download and install the NuGet package  
[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

[https://www.nuget.org/packages/DogmaSolutions.Analyzers](https://www.nuget.org/packages/DogmaSolutions.Analyzers)