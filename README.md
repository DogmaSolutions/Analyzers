# Dogma Solutions Roslyn Analyzers
[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

A set of Roslyn Analyzer aimed to enforce some design good practices and code quality (QA) rules.

# Rules structure
This section describes the rules included into this package.

Every rule is accompanied by the following information and clues:
- **Category** → identify the area of interest of the rule, and can have one of the following values: _Design / Naming / Style / Usage / Performance / Security_ 
- **Severity** → state the default severity level of the rule. The severity level can be changed by editing the _.editorconfig_ file used by the project/solution. Possible values are enumerated by the [DiagnosticSeverity enum](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnosticseverity)
- **Description, motivations and fixes** → a detailed explanation of the detected issue, and a brief description on how to change your code in order to solve it.
- **See also** → a list of similar/related rules, or related knownledge base


# Rules list
| Id                | Category    | Description                                                                                                                                                                                                |Severity|Is enabled|Code fix|
|-------------------|-------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------:|:--------:|:------:|
| [DSA001](#dsa001) | Design      | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression**. |⚠|✅|❌|
| [DSA002](#dsa002) | Design      | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ fluent query**.     |⚠|✅|❌|
| [DSA003](#dsa003) | Code Smells | Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty`                                                                                                                                          |⚠|✅|❌|
| [DSA004](#dsa004) | Code Smells | Use `DateTime.UtcNow` instead of `DateTime.Now`                                                                                                                                                            |⚠|✅|❌|
| [DSA005](#dsa005) | Code Smells | Potential non-deterministic point-in-time execution                                                                                                                                                        |⛔|✅|❌|

---
       
# DSA001
Don't use Entity Framework to launch LINQ queries in a WebApi controller.
- **Category**: Design
- **Severity**: Warning ⚠
- **Related rules**: [DSA002](#dsa002)

## Description
[WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression**.  
In the analyzed code, a [WebApi controller method](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) is using [Entity Framework DbContext](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext) to directly manipulate data through a LINQ query expression.  
WebApi controllers should not contain data-manipulation business logics. 

## See also
This is a typical violation of the ["Single Responsibility" rule](https://en.wikipedia.org/wiki/Single-responsibility_principle) of the ["SOLID" principles](https://en.wikipedia.org/wiki/SOLID), because the controller is doing too many things outside its own purpose.  

## Fix / Mitigation
In order to fix the problem, the code could be modified in order to rely on the ["Indirection pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Indirection) and maximize the ["Low coupling evalutative pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Low_coupling) of the ["GRASP"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)) principles.
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
In the analyzed code, a [WebApi controller method](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) is using [Entity Framework DbSet](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbset-1) to directly manipulate data through a LINQ fluent query.  
WebApi controllers should not contain data-manipulation business logics.

## See also
This is a typical violation of the ["Single Responsibility" rule](https://en.wikipedia.org/wiki/Single-responsibility_principle) of the ["SOLID" principles](https://en.wikipedia.org/wiki/SOLID), because the controller is doing too many things outside its own purpose. 

## Fix / Mitigation
In order to fix the problem, the code could be modified in order to rely on the ["Indirection pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Indirection) and maximize the ["Low coupling evalutative pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Low_coupling) of the ["GRASP"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)) principles.   
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
Usually, business logics distinguish between "string with content", and "string NULL or without meaningfull content".  
Thus, statistically speaking, almost every call to `string.IsNullOrEmpty` could or should be replaced by a call to `string.IsNullOrWhiteSpace`, because in the large majority of cases, a string composed by only spaces, tabs, and return chars is not considered valid because it doesn't have "meaningfull content".  
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
*"This category represents one of the phyla in the Seven Pernicious Kingdoms vulnerability classification. It includes weaknesses related to the improper management of time and state in an environment that supports simultaneous or near-simultaneous computation by multiple systems, processes, or threads. According to the authors of the Seven Pernicious Kingdoms, "Distributed computation is about time and state. That is, in order for more than one component to communicate, state must be shared, and all that takes time. Most programmers anthropomorphize their work. They think about one thread of control carrying out the entire program in the same way they would if they had to do the job themselves. Modern computers, however, switch between tasks very quickly, and in multi-core, multi-CPU, or distributed systems, two events may take place at exactly the same time. Defects rush to fill the gap between the programmer's model of how a program executes and what happens in reality. These defects are related to unexpected interactions between threads, processes, time, and information. These interactions happen through shared state: semaphores, variables, the file system, and, basically, anything that can store information."*

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
  *"This category represents one of the phyla in the Seven Pernicious Kingdoms vulnerability classification. It includes weaknesses related to the improper management of time and state in an environment that supports simultaneous or near-simultaneous computation by multiple systems, processes, or threads. According to the authors of the Seven Pernicious Kingdoms, "Distributed computation is about time and state. That is, in order for more than one component to communicate, state must be shared, and all that takes time. Most programmers anthropomorphize their work. They think about one thread of control carrying out the entire program in the same way they would if they had to do the job themselves. Modern computers, however, switch between tasks very quickly, and in multi-core, multi-CPU, or distributed systems, two events may take place at exactly the same time. Defects rush to fill the gap between the programmer's model of how a program executes and what happens in reality. These defects are related to unexpected interactions between threads, processes, time, and information. These interactions happen through shared state: semaphores, variables, the file system, and, basically, anything that can store information."*

## Fix/Mitigation
In order to avoid problems, apply one of these, depending on the situation:
- When measuring elapsed time, use a `StopWatch.StartNew()` combined with `StopWatch.Elapsed`
- When NOT measuring elapsed time, set a `var now = DateTime.UtcNow` variable at the top of the method, or at the beginning of an execution flow/algorithm, and reuse that variable in all places instead of `DateTime.***Now`.

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

# Installation
Just download and install the NuGet package  
[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/) 

[https://www.nuget.org/packages/DogmaSolutions.Analyzers](https://www.nuget.org/packages/DogmaSolutions.Analyzers)
