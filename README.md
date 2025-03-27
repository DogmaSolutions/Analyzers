# Dogma Solutions Roslyn Analyzers

[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

A set of Roslyn Analyzer aimed to enforce some design good practices and code quality (QA) rules.

# Rules structure

This section describes the rules included into this package.

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

---

# DSA001

Don't use Entity Framework to launch LINQ queries in a WebApi controller.

- **Category**: Design
- **Severity**: Warning ⚠
- **Related rules**: [DSA002](#dsa002)

## Description

[WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression
**.  
In the analyzed code, a [WebApi controller method](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) is
using [Entity Framework DbContext](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext) to directly manipulate data through a LINQ query expression.  
WebApi controllers should not contain data-manipulation business logics.

## See also

This is a typical violation of the ["Single Responsibility" rule](https://en.wikipedia.org/wiki/Single-responsibility_principle) of the ["SOLID" principles](https://en.wikipedia.org/wiki/SOLID),
because the controller is doing too many things outside its own purpose.

## Fix / Mitigation

In order to fix the problem, the code could be modified in order to rely on the ["Indirection pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Indirection) and maximize
the ["Low coupling evalutative pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Low_coupling) of the ["GRASP"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design))
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
the ["Low coupling evalutative pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Low_coupling) of the ["GRASP"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design))
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

- Incoherence between nodes or processes running in different timezones (even in the same country, i.e. USA, Soviet Union, China, etc)
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

General exceptions should never be thrown, because throwing them, prevents calling methods from discriminating between system-generated exceptions, and application-generated errors.  
This is a bad smell, and could lead to stability and security concerns.  
General exceptions that triggers this rule are:

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
      if(id < 0) // this NOT OK, and will be matched by the rule
        throw new SystemException("Invalid id");
    }

}
```

---

# DSA007

When initializing a lazy field (and in particular fields contains the instance of a singleton object), use a robust locking pattern, i.e. the “if-lock-if” (aka “double checked locking”)

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
    lock(_theLock){ // ❌ too early, very wastefull, poor performances
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
        lock(_theLock){ // ✅ protects againts race conditions and multithreading
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

Is a common misunderstanding that the [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute) is somehow able to validate a
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

Is a common misunderstanding that the [Required Attribute](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.requiredattribute) is somehow able to validate a
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

Self-contained static singleton properties, particularly when they involve lazy initialization within the property itself, can lead to several problems, especially in multithreaded environments.
Due to their static nature, they are also difficult to test, and could manifest unpredictable results if the testing framework (or the tests) doesn't clean the static instances in-between sessions.

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

Use a IoC/DI framework instead, or at least use proper locking when initializing the instance.

## See also

- [Singletons Are Evil](https://wiki.c2.com/?SingletonsAreEvil)
- [Singleton Pattern](https://en.wikipedia.org/wiki/Singleton_pattern)

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA011: Avoid lazily initialized, self-contained, static singleton properties
dotnet_diagnostic.DSA011.severity = warning
```

---

# Installation

Just download and install the NuGet package  
[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

[https://www.nuget.org/packages/DogmaSolutions.Analyzers](https://www.nuget.org/packages/DogmaSolutions.Analyzers)