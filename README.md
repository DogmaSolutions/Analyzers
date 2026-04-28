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

| Id                | Category      | Description                                                                                                                                                                                                | Default severity | Is enabled | Code fix |
|-------------------|---------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------|------------|----------|
| [DSA001](#dsa001) | Design        | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression**. | ⚠ Warning        | ✅          | ❌        |
| [DSA002](#dsa002) | Design        | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ fluent query**.     | ⚠ Warning        | ✅          | ❌        |
| [DSA003](#dsa003) | Code Smells   | Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty`                                                                                                                                          | ⚠ Warning        | ✅          | ✅        |
| [DSA004](#dsa004) | Code Smells   | Use `DateTime.UtcNow` instead of `DateTime.Now`                                                                                                                                                            | ⚠ Warning        | ✅          | ✅        |
| [DSA005](#dsa005) | Code Smells   | Potential non-deterministic point-in-time execution                                                                                                                                                        | ⛔ Error          | ✅          | ❌        |
| [DSA006](#dsa006) | Code Smells   | General exceptions should not be thrown by user code                                                                                                                                                       | ⛔ Error          | ✅          | ❌        |
| [DSA007](#dsa007) | Code Smells   | When initializing a lazy field, use a robust locking pattern, i.e. the "if-lock-if" (aka "double checked locking")                                                                                         | ⚠ Warning        | ✅          | ❌        |
| [DSA008](#dsa008) | Bug           | The Required Attribute has no impact on a not-nullable DateTime                                                                                                                                            | ⛔ Error          | ✅          | ❌        |
| [DSA009](#dsa009) | Bug           | The Required Attribute has no impact on a not-nullable DateTimeOffset                                                                                                                                      | ⛔ Error          | ✅          | ❌        |
| [DSA011](#dsa011) | Design        | Avoid lazily initialized, self-contained, static singleton properties                                                                                                                                      | ⚠ Warning        | ✅          | ❌        |
| [DSA012](#dsa012) | Design        | Avoid the "if not exists, then insert" check-then-act antipattern on database types (TOCTOU)                                                                                                               | ⚠ Warning        | ✅          | ❌        |
| [DSA013](#dsa013) | Security      | Minimal API endpoints should have an explicit authorization configuration                                                                                                                                  | ⚠ Warning        | ✅          | ✅        |
| [DSA014](#dsa014) | Security      | Minimal API endpoints on route groups should have an explicit authorization configuration                                                                                                                   | ⚠ Warning        | ✅          | ✅        |
| [DSA015](#dsa015) | Security      | Minimal API endpoints on parameterized route builders should have an explicit authorization configuration                                                                                                   | ⚠ Warning        | ✅          | ✅        |
| [DSA016](#dsa016) | Code Smells   | Avoid repeated invocation of the same enumeration method with identical arguments                                                                                                                           | ⚠ Warning        | ✅          | ❌        |
| [DSA017](#dsa017) | Design        | Use the collection's atomic operation instead of the check-then-act pattern                                                                                                                                 | ⚠ Warning        | ✅          | ❌        |
| [DSA018](#dsa018) | Design        | Protect the check-then-act pattern with a lock or use a collection with built-in duplicate handling                                                                                                         | ⚠ Warning        | ✅          | ❌        |
| [DSA019](#dsa019) | Code Smells   | Avoid repeated deeply nested member access chains                                                                                                                                                           | ⚠ Warning        | ✅          | ✅        |
| [DSA020](#dsa020) | Code Smells   | Remove redundant async/await on Task.FromResult                                                                                                                                                             | ⚠ Warning        | ✅          | ✅        |
| [DSA021](#dsa021) | Best Practice | Entity Framework queries should be tagged with TagWith or TagWithCallSite for traceability                                                                                                                  | ⚠ Warning        | ✅          | ✅        |
| [DSA022](#dsa022) | Performance | Hoist loop-invariant expression out of inner loop                                                                                                                                                           | ⚠ Warning        | ✅          | ✅        |

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

Security-wise, mixing data access logic directly into the presentation layer weakens compartmentalization and increases the attack surface, making it harder to apply consistent authorization, input validation, and audit logging at the data access boundary.

- [MITRE, CWE-653: Improper Isolation or Compartmentalization](https://cwe.mitre.org/data/definitions/653.html)
- [MITRE, CWE-1057: Data Access Operations Outside of Expected Data Manager Component](https://cwe.mitre.org/data/definitions/1057.html)
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 5 (Restricted Data Flow): enforcing proper separation between presentation and data access layers aligns with the zones and conduits model, where each zone has clearly defined data flow boundaries

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

Security-wise, mixing data access logic directly into the presentation layer weakens compartmentalization and increases the attack surface, making it harder to apply consistent authorization, input validation, and audit logging at the data access boundary.

- [MITRE, CWE-653: Improper Isolation or Compartmentalization](https://cwe.mitre.org/data/definitions/653.html)
- [MITRE, CWE-1057: Data Access Operations Outside of Expected Data Manager Component](https://cwe.mitre.org/data/definitions/1057.html)
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 5 (Restricted Data Flow): enforcing proper separation between presentation and data access layers aligns with the zones and conduits model, where each zone has clearly defined data flow boundaries

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

Security-wise, using `IsNullOrEmpty` instead of `IsNullOrWhiteSpace` can allow whitespace-only strings to bypass input validation, potentially leading to injection attacks, data corruption, or logic bypass when the application treats whitespace-only input as valid content.

## See also

- [MITRE, CWE-20: Improper Input Validation](https://cwe.mitre.org/data/definitions/20.html)
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity): robust input validation is a prerequisite for maintaining system integrity; whitespace-only strings bypassing validation can lead to unauthorized state changes

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
*”This category represents one of the phyla in the Seven Pernicious Kingdoms vulnerability classification. It includes weaknesses related to the improper management of time and state in an environment
that supports simultaneous or near-simultaneous computation by multiple systems, processes, or threads. According to the authors of the Seven Pernicious Kingdoms, “Distributed computation is about
time and state. That is, in order for more than one component to communicate, state must be shared, and all that takes time. Most programmers anthropomorphize their work. They think about one thread
of control carrying out the entire program in the same way they would if they had to do the job themselves. Modern computers, however, switch between tasks very quickly, and in multi-core, multi-CPU,
or distributed systems, two events may take place at exactly the same time. Defects rush to fill the gap between the programmer's model of how a program executes and what happens in reality. These
defects are related to unexpected interactions between threads, processes, time, and information. These interactions happen through shared state: semaphores, variables, the file system, and,
basically, anything that can store information.”*

- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity) and FR 6 (Timely Response to Events): consistent UTC timestamps are essential for reliable audit trails, event correlation across distributed nodes, and forensic analysis during incident response

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
*”This category represents one of the phyla in the Seven Pernicious Kingdoms vulnerability classification. It includes weaknesses related to the improper management of time and state in an environment
that supports simultaneous or near-simultaneous computation by multiple systems, processes, or threads. According to the authors of the Seven Pernicious Kingdoms, “Distributed computation is about
time and state. That is, in order for more than one component to communicate, state must be shared, and all that takes time. Most programmers anthropomorphize their work. They think about one thread
of control carrying out the entire program in the same way they would if they had to do the job themselves. Modern computers, however, switch between tasks very quickly, and in multi-core, multi-CPU,
or distributed systems, two events may take place at exactly the same time. Defects rush to fill the gap between the programmer's model of how a program executes and what happens in reality. These
defects are related to unexpected interactions between threads, processes, time, and information. These interactions happen through shared state: semaphores, variables, the file system, and,
basically, anything that can store information.”*

- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity) and FR 6 (Timely Response to Events): a sliding time reference within a single execution flow undermines the determinism required for reliable audit logs, security event correlation, and time-bound access control decisions

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

- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity): generic exceptions prevent callers from distinguishing between operational errors and security-relevant failures, undermining the ability to implement targeted error handling and maintain system integrity under fault conditions

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
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity): improper locking during lazy initialization can lead to race conditions that corrupt shared state, violating the integrity guarantees required for security-critical components

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
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity): a silently ineffective validation attribute creates a false sense of security; unvalidated input reaching business logic or persistence layers can compromise data integrity

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
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity): a silently ineffective validation attribute creates a false sense of security; unvalidated input reaching business logic or persistence layers can compromise data integrity

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
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity): unsynchronized lazy initialization of shared state can produce partially constructed or duplicate instances, violating the integrity invariants that security-critical services depend on

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA011: Avoid lazily initialized, self-contained, static singleton properties
dotnet_diagnostic.DSA011.severity = warning
```

---

# DSA012

Avoid the "if not exists, then insert" check-then-act antipattern on database types (TOCTOU).

- **Category**: Design
- **Severity**: Warning ⚠
- **Related rules**: [DSA005](#dsa005), [DSA017](#dsa017), [DSA018](#dsa018)

## Description
This rule fires when the _"if not exists, then insert"_ check-then-act pattern is used on **database types** (`DbSet<T>`, `IQueryable<T>`).

The pattern is a non-atomic sequence that first checks whether a record exists and then, based on the result, inserts a new one.  
This pattern **is not a bad thing per-se**, but _suggests_ (or at least gives the suspicion) that the coherence of the data is _only_ handled by application-level logics which, if true, can lead to undesired effects.

For the same pattern on **in-memory collections with atomic alternatives** (e.g., `Dictionary.TryAdd`, `HashSet.Add`), see [DSA017](#dsa017).  
For the same pattern on **collections without atomic alternatives** (e.g., `List<T>`, `ICollection<T>`), see [DSA018](#dsa018).

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
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity): non-atomic check-then-act on database types can lead to duplicate records or data corruption under concurrent access, violating the data integrity guarantees required by the standard

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

# DSA017

Use the collection's atomic operation instead of the check-then-act pattern.

- **Category**: Design
- **Severity**: Warning ⚠
- **Related rules**: [DSA012](#dsa012), [DSA018](#dsa018)

## Description

This rule fires when the _"if not exists, then insert"_ check-then-act pattern is used on a **collection type that offers an atomic alternative**. The check is redundant because the collection provides a built-in operation that combines existence verification and insertion atomically. Using the check-then-act pattern instead of the atomic operation is prone to TOCTOU race conditions in multithreaded code.

The following collection types and their suggested alternatives are covered:

| Collection type | Suggested atomic alternative |
|---|---|
| `Dictionary<K,V>` | `TryAdd` or indexer assignment `[key] = value` |
| `ConcurrentDictionary<K,V>` | `GetOrAdd`, `AddOrUpdate`, or `TryAdd` |
| `HashSet<T>` | `Add` (already returns a `bool` indicating whether the element was added) |
| `SortedSet<T>` | `Add` (already returns a `bool`) |
| `SortedDictionary<K,V>` | `TryAdd` or indexer assignment `[key] = value` |
| `SortedList<K,V>` | Indexer assignment `[key] = value` |
| `ImmutableHashSet<T>` | `Add` (already handles duplicates) |
| `ImmutableDictionary<K,V>` | `SetItem` (upsert semantics) |
| `ImmutableSortedSet<T>` | `Add` (already handles duplicates) |
| `ImmutableSortedDictionary<K,V>` | `SetItem` (upsert semantics) |

**Set-like types and complex bodies**: for `HashSet<T>`, `SortedSet<T>`, `ImmutableHashSet<T>`, and `ImmutableSortedSet<T>`, the rule only fires when the if-body contains **only** the `Add` call (simple deduplication). When the body contains additional logic (e.g., expensive computation, logging, cache initialization), the check-then-act is treated as an intentional cache/guard pattern and is **not flagged**, because these types do not offer a "get-or-add" strategy equivalent to `ConcurrentDictionary.GetOrAdd`.

## See also

- [DSA012: Check-then-act on database types](#dsa012)
- [DSA018: Check-then-act on collections without atomic alternatives](#dsa018)
- [MITRE, CWE-367: Time-of-check Time-of-use (TOCTOU) Race Condition](https://cwe.mitre.org/data/definitions/367.html)
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity): using the collection's atomic operation eliminates a class of race conditions that could corrupt shared state or bypass deduplication logic in security-critical data structures

## Matched patterns

```cs
// Dictionary: ContainsKey + Add
if (!dict.ContainsKey(key))
{
    dict.Add(key, value);  // ❌ use dict.TryAdd(key, value) or dict[key] = value
}

// HashSet: Contains + Add
if (!set.Contains(item))
{
    set.Add(item);  // ❌ just call set.Add(item) — it returns false if already present
}

// Dictionary: TryGetValue negated + Add
if (!dict.TryGetValue(key, out var existing))
{
    dict.Add(key, value);  // ❌ use dict.TryAdd(key, value)
}
```

## Not matched patterns

```cs
// List (no atomic alternative — handled by DSA018)
if (!list.Contains(item)) { list.Add(item); }  // ✅ not flagged by DSA017

// DbSet (database type — handled by DSA012)
if (!dbSet.Any(x => x.Id == id)) { dbSet.Add(entity); }  // ✅ not flagged by DSA017

// ContainsKey without Add in body
if (!dict.ContainsKey(key)) { Console.WriteLine("not found"); }  // ✅ no insert

// HashSet: Contains + Add with additional logic (cache/guard pattern)
if (!cache.Contains(key))
{
    var data = LoadExpensiveData(key);  // additional logic beyond Add
    cache.Add(key);                     // ✅ not flagged — intentional cache pattern
}
```

## Fix / Mitigation

Replace the check-then-act pattern with the collection's atomic operation:

```cs
// Dictionary: use TryAdd
dict.TryAdd(key, value);  // ✅ atomic: returns false if key already exists

// Dictionary: use indexer (upsert — overwrites if exists)
dict[key] = value;  // ✅ atomic upsert

// HashSet: just call Add
set.Add(item);  // ✅ returns bool; the Contains check is redundant

// ConcurrentDictionary: use GetOrAdd
var val = concurrentDict.GetOrAdd(key, _ => ComputeValue());  // ✅ thread-safe
```

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA017: Use the collection's atomic operation instead of the check-then-act pattern
dotnet_diagnostic.DSA017.severity = error
```

## Code sample

```csharp
public class RegistryService
{
    private readonly Dictionary<string, int> _registry = new();

    public void Register_NotOk(string key, int value)
    {
        // this WILL trigger the rule
        if (!_registry.ContainsKey(key))
        {
            _registry.Add(key, value);
        }
    }

    public void Register_Ok(string key, int value)
    {
        // this WILL NOT trigger the rule
        _registry.TryAdd(key, value);
    }
}
```

---

# DSA018

Protect the check-then-act pattern with a lock or use a collection with built-in duplicate handling.

- **Category**: Design
- **Severity**: Warning ⚠
- **Related rules**: [DSA012](#dsa012), [DSA017](#dsa017)

## Description

This rule fires when the _"if not exists, then insert"_ check-then-act pattern is used on a **collection type that does not offer an atomic alternative** (e.g., `List<T>`, `IList<T>`, `ICollection<T>`, `LinkedList<T>`), or on an **unknown type** where the analyzer cannot determine whether an atomic operation exists.

Between the existence check and the insert, another thread could modify the collection, leading to duplicate entries or corruption. Since the collection type does not provide a built-in atomic operation, the check-then-act sequence must be protected externally.

## See also

- [DSA012: Check-then-act on database types](#dsa012)
- [DSA017: Check-then-act on collections with atomic alternatives](#dsa017)
- [MITRE, CWE-367: Time-of-check Time-of-use (TOCTOU) Race Condition](https://cwe.mitre.org/data/definitions/367.html)
- [MITRE, CWE-667: Improper Locking](https://cwe.mitre.org/data/definitions/667.html)
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 3 (System Integrity): on collection types without atomic alternatives, the check-then-act sequence must be externally synchronized to prevent race conditions that corrupt shared state or introduce duplicate entries in security-relevant data structures

## Matched patterns

```cs
// List: Any + Add
if (!items.Any(x => x == value))
{
    items.Add(value);  // ❌ TOCTOU: another thread could add between check and insert
}

// List: Contains + Add
if (!items.Contains(value))
{
    items.Add(value);  // ❌
}

// ICollection: Contains + Add
if (!collection.Contains(item))
{
    collection.Add(item);  // ❌
}
```

## Not matched patterns

```cs
// Dictionary (has atomic alternative — handled by DSA017)
if (!dict.ContainsKey(key)) { dict.Add(key, value); }  // ✅ not flagged by DSA018

// HashSet (has atomic alternative — handled by DSA017)
if (!set.Contains(item)) { set.Add(item); }  // ✅ not flagged by DSA018

// DbSet (database type — handled by DSA012)
if (!dbSet.Any(x => x.Id == id)) { dbSet.Add(entity); }  // ✅ not flagged by DSA018
```

## Fix / Mitigation

Protect the sequence with a `lock` or `SemaphoreSlim`, or switch to a collection type with built-in duplicate handling:

```cs
// Fix 1: protect with lock
lock (_syncRoot)
{
    if (!items.Contains(value))
    {
        items.Add(value);  // ✅ protected by lock
    }
}

// Fix 2: switch to HashSet (which handles duplicates inherently)
var set = new HashSet<string>();
set.Add(value);  // ✅ returns false if already present, no check needed

// Fix 3: switch to ConcurrentDictionary for thread-safe keyed access
var dict = new ConcurrentDictionary<string, bool>();
dict.TryAdd(value, true);  // ✅ thread-safe, atomic
```

## When to ignore this rule

If the code is guaranteed to run single-threaded (e.g., inside a single-threaded console application or a synchronization context that serializes access), the TOCTOU risk does not apply. Suppress with `#pragma warning disable DSA018`.

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA018: Protect the check-then-act pattern with a lock or use a collection with built-in duplicate handling
dotnet_diagnostic.DSA018.severity = error
```

## Code sample

```csharp
public class TagService
{
    private readonly List<string> _tags = new();
    private readonly object _lock = new();

    public void AddTag_NotOk(string tag)
    {
        // this WILL trigger the rule
        if (!_tags.Contains(tag))
        {
            _tags.Add(tag);
        }
    }

    public void AddTag_Ok(string tag)
    {
        // this WILL NOT trigger the rule (lock protection)
        lock (_lock)
        {
            if (!_tags.Contains(tag))
            {
                _tags.Add(tag);
            }
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
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 1 (Identification and Authentication Control) and FR 2 (Use Control): every endpoint must enforce an explicit, reviewable authorization decision to satisfy the identification, authentication, and use control requirements

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
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 1 (Identification and Authentication Control) and FR 2 (Use Control): route groups that silently default to anonymous access violate the principle that all access paths must carry an explicit, auditable authorization policy

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
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 1 (Identification and Authentication Control) and FR 2 (Use Control): when authorization responsibility is distributed across call sites, cross-method tracing is essential to verify that every access path enforces the required authentication and use control policies

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

**Static method exclusion**: methods called on a type rather than an instance (e.g., `File.Exists(path)`, `Directory.Exists(path)`) are excluded — these are not enumeration methods and calling them multiple times is legitimate.

## See also

Security-wise, repeated enumeration of a deferred `IEnumerable` backed by a database query or external data source can lead to non-deterministic behavior if the underlying data changes between enumerations, potentially causing inconsistent authorization decisions, data integrity violations, or information disclosure.

- [MITRE, CWE-1049: Interaction Frequency](https://cwe.mitre.org/data/definitions/1049.html) — excessive repeated operations consuming unnecessary resources
- [MITRE, CWE-834: Excessive Iteration](https://cwe.mitre.org/data/definitions/834.html) — redundant enumeration cycles over the same data source
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

// Static method calls (not instance enumeration methods)
var a = File.Exists(path);
var b = File.Exists(path);  // ✅ static method on a type, not enumeration

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

# DSA019

Avoid repeated deeply nested member access chains.

- **Category**: Code Smells
- **Severity**: Warning ⚠
- **Related rules**: [DSA016](#dsa016)

## Description

This rule fires when the same deeply nested member access chain (e.g., `home.Rooms.Bathroom.Lights`) appears **multiple times** in the **same scope** (method body, lambda, or local function). Repeated deep dereferencing reduces readability and may incur unnecessary overhead if the intermediate accesses involve computation, virtual dispatch, or property getters with side effects.

The depth threshold is configurable: only chains whose depth (number of member accesses and element accesses from the root) meets or exceeds the threshold are checked. The default threshold is **3**.

Each scope is analyzed independently; chains in nested lambdas are not compared with chains in the outer scope.

**Depth counting**: each `.Property`, `[index]`, and `.Method` access adds one level. `InvocationExpression` wrappers (e.g., `.GetData()`) are traversed transparently without adding depth. Expressions inside `nameof()` are excluded.

## Configuration

The threshold is configurable via `.editorconfig`:

```
# Default is 3. Set higher to allow deeper repeated chains; lower to be stricter.
dotnet_diagnostic.DSA019.max_repeated_dereferenciation_depth = 3
```

**Excluded prefixes**: certain APIs use deeply nested fluent chains as part of their design (e.g., NUnit's `Is.Not.Null.And`, FluentAssertions' `Should().Be`, LINQ builder patterns). These chains are intentional and should not be extracted into variables. To exclude specific prefixes from the analysis, use the `excluded_prefixes` option:

```
# Comma-separated list of chain prefixes to exclude from analysis.
# Any member access chain starting with one of these prefixes will not be flagged.
dotnet_diagnostic.DSA019.excluded_prefixes = Is.Not, Has.No, Does.Not, Should
```

This is particularly useful in test projects where fluent assertion frameworks produce deep chains by design.

## See also

Security-wise, repeated deep member access chains can expose code to non-deterministic behavior if any intermediate property getter has side effects, performs lazy initialization, or reads from a volatile source. In security-sensitive code paths (e.g., authorization checks, input validation), evaluating the same chain multiple times could yield different values, leading to time-of-check to time-of-use vulnerabilities.

- [MITRE, CWE-1049: Interaction Frequency](https://cwe.mitre.org/data/definitions/1049.html) — excessive repeated access operations
- [MITRE, CWE-367: Time-of-check Time-of-use (TOCTOU) Race Condition](https://cwe.mitre.org/data/definitions/367.html) — repeated evaluation may yield different values if the object graph is mutated concurrently
- [DSA016: Avoid repeated enumeration method invocations](#dsa016) (similar concept for LINQ method calls)

## Matched patterns

```cs
// Pattern A: same chain with different indexer
var primary = home.Rooms.Bathroom.Lights[0].IsOn();    // ❌ home.Rooms.Bathroom.Lights (depth 3)
var secondary = home.Rooms.Bathroom.Lights[1].IsOn();  // ❌ repeated

// Pattern B: same chain with different terminal property
var connStr = config.Settings.Infrastructure.Database.ConnectionString;  // ❌ config.Settings.Infrastructure.Database (depth 3)
var timeout = config.Settings.Infrastructure.Database.Timeout;           // ❌ repeated

// Pattern C: chain with method call in the middle
var count = provider.Service.GetReport().Summary.Count;  // ❌ provider.Service.GetReport().Summary (depth 3)
var label = provider.Service.GetReport().Summary.Label;  // ❌ repeated
```

## Not matched patterns

```cs
// Chain depth below threshold (depth 2, threshold 3)
var a = outer.Inner.Value;
var b = outer.Inner.Value;  // ✅ depth 2, below threshold

// Chain appears only once
var v = a.B.C.D.Value;  // ✅ single occurrence

// Inside nameof (compile-time, no actual dereferencing)
var n1 = nameof(A.B.C);
var n2 = nameof(A.B.C);  // ✅ excluded

// Different scopes (method body vs nested lambda)
var v1 = a.B.C.D.Value;
Action act = () => { var v2 = a.B.C.D.Value; };  // ✅ separate scopes

// Chains that differ at the root
var v1 = a1.B.C.D.Value;
var v2 = a2.B.C.D.Value;  // ✅ different root identifiers

// Already extracted into a variable
var lights = home.Rooms.Bathroom.Lights;
var primary = lights[0].IsOn();
var secondary = lights[1].IsOn();  // ✅ no deep chain repeated

// Excluded prefix (configured via excluded_prefixes in .editorconfig)
// With: dotnet_diagnostic.DSA019.excluded_prefixes = Is.Not
var a = Is.Not.Null.And.GreaterThan(0);
var b = Is.Not.Null.And.GreaterThanOrEqualTo(0);  // ✅ excluded by prefix
```

## Fix / Mitigation

Extract the repeated chain into a local variable:

```cs
// Before:
var primary = home.Rooms.Bathroom.Lights[0].IsOn();
var secondary = home.Rooms.Bathroom.Lights[1].IsOn();

// After:
var lights = home.Rooms.Bathroom.Lights;
var primary = lights[0].IsOn();
var secondary = lights[1].IsOn();
```

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA019: Avoid repeated deeply nested member access chains
dotnet_diagnostic.DSA019.severity = error
```

## Code sample

```csharp
public class HomeAutomationService
{
    public object GetLightStatus(Home home)
    {
        // this WILL trigger the rule: home.Rooms.Bathroom.Lights repeated
        return new
        {
            Primary = home.Rooms.Bathroom.Lights[0].IsOn(),
            Secondary = home.Rooms.Bathroom.Lights[1].IsOn(),
            Tertiary = home.Rooms.Bathroom.Lights[2].IsOn(),
        };
    }

    public object GetLightStatus_Fixed(Home home)
    {
        // this WILL NOT trigger the rule: extracted into a variable
        var lights = home.Rooms.Bathroom.Lights;
        return new
        {
            Primary = lights[0].IsOn(),
            Secondary = lights[1].IsOn(),
            Tertiary = lights[2].IsOn(),
        };
    }
}
```

---

# DSA020

Remove redundant async/await on Task.FromResult.

- **Category**: Code Smells
- **Severity**: Warning ⚠
- **Related rules**: none

## Description

This rule fires when an async lambda only awaits `Task.FromResult(...)`. The async/await is redundant because `Task.FromResult` already returns a completed `Task<T>`; the `async` modifier forces the compiler to generate a state machine that unwraps and re-wraps the value unnecessarily.

**Why the compiler does not simplify this automatically**: the C# compiler (Roslyn) transforms every `async` method or lambda into a full state machine — a struct implementing `IAsyncStateMachine` with a `MoveNext` method, an `AsyncTaskMethodBuilder`, and the associated dispatch logic. This transformation is a faithful implementation of the language specification, not an optimization pass; the compiler does not perform semantic analysis to detect whether the `async`/`await` is "trivially removable." Specifically, it cannot special-case `Task.FromResult` because it has no compile-time knowledge about which methods return already-completed tasks — `Task.FromResult` is just a regular method call from the compiler's perspective.

More importantly, removing `async`/`await` changes the **exception propagation semantics**: in an `async` lambda, any exception thrown during evaluation is captured in the returned `Task` (the caller observes it via `await` or `.Result`); without `async`, the same exception propagates synchronously to the caller. This behavioral difference prevents the compiler from performing the transformation automatically. However, in the specific case of `Task.FromResult` — where the developer is explicitly constructing a completed task from a value — this semantic difference is rarely meaningful, and the developer is in the best position to make that judgment.

**JIT behavior**: the JIT (RyuJIT) optimizes synchronously completed awaits at runtime — when the task's `IsCompleted` returns `true`, the continuation is not scheduled and the result is extracted immediately, avoiding actual suspension. However, the state machine struct, the builder setup, and the `MoveNext` method body are still compiled and executed. The JIT cannot eliminate the state machine itself because the async contract requires it. In .NET 6+, the runtime pools async state machines and the `AsyncTaskMethodBuilder` has been optimized to reduce allocations for synchronously completing paths, but the fundamental overhead of entering and exiting the state machine remains. Tier-0 JIT (used during startup and in ReadyToRun scenarios) does not inline aggressively, so the overhead is more pronounced in those contexts. NativeAOT compilation preserves the state machine as well.

Writing the code without the unnecessary `async`/`await` avoids all of this overhead at the source level, produces cleaner IL, and removes any dependency on runtime optimization quality across .NET versions and platforms.

The rule detects:
- Expression-body lambdas: `async ct => await Task.FromResult(x)`
- Block-body lambdas with a single return: `async ct => { return await Task.FromResult(x); }`
- Generic variants: `Task.FromResult<T>(x)`
- `ConfigureAwait` chains: `await Task.FromResult(x).ConfigureAwait(false)` (the `ConfigureAwait` is stripped since it's only meaningful in an `await` context)

All lambda forms are handled: simple (`async ct =>`), parenthesized (`async (ct) =>`), discard (`async _ =>`), and parameterless (`async () =>`).

## See also

Security-wise, unnecessary async state machines increase the attack surface for resource exhaustion: each state machine allocates heap memory and scheduler overhead for work that completes synchronously. In high-throughput scenarios (e.g., middleware pipelines, message handlers, event-driven architectures), this overhead compounds across thousands of invocations per second, contributing to GC pressure, increased Gen0/Gen1 collection frequency, and latency spikes under load.

The runtime does optimize synchronously completed awaits — when the task's `IsCompleted` returns `true`, the continuation is not scheduled and the result is extracted immediately, avoiding actual thread suspension. However, the state machine struct is still instantiated, the `AsyncTaskMethodBuilder` is still initialized, and the `MoveNext` method is still called. These costs are small individually but measurable at scale. Furthermore, relying on these optimizations is fragile: their effectiveness varies across .NET versions (the pooling and caching strategies have changed across .NET 5, 6, 7, and 8), across JIT tiers (Tier-0, used during application startup and in ReadyToRun scenarios, does not inline the state machine aggressively; only Tier-1 with PGO achieves maximum optimization), and across compilation modes (NativeAOT preserves the full state machine and does not benefit from tiered JIT). Writing the code correctly at the source level eliminates this entire class of overhead regardless of the runtime environment.

- [MITRE, CWE-1049: Interaction Frequency](https://cwe.mitre.org/data/definitions/1049.html)
- [CA1849: Call async methods when in an async method](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1849)
- [Stephen Toub: How Async/Await Really Works in C#](https://devblogs.microsoft.com/dotnet/how-async-await-really-works/) — comprehensive explanation of the state machine transformation, builder mechanics, and runtime fast-path for completed tasks
- [Sergey Tepliakov: Dissecting the async methods in C#](https://devblogs.microsoft.com/premier-developer/dissecting-the-async-methods-in-c/) — deep dive into the generated IL, state machine struct, and MoveNext dispatch with before/after comparisons
- [.NET 6 Performance Improvements: Async](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-6/#async) — documents the runtime optimizations for synchronously completing async methods, including state machine pooling and their limitations
- [.NET 7 Performance Improvements: Async](https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/#async) — further async optimizations including on-stack async method builders
- [Roslyn Async Design](https://github.com/dotnet/roslyn/blob/main/docs/features/async.md) — compiler design document explaining why the state machine transformation is applied uniformly to all async methods without semantic simplification

## Matched patterns

```cs
// All of these are flagged:
async (ct) => await Task.FromResult(42)
async ct => await Task.FromResult(42)
async _ => await Task.FromResult(42)
async () => await Task.FromResult("hello")
async (ct) => await Task.FromResult<int>(42)
async () => await Task.FromResult<string>("hello")
async (ct) => { return await Task.FromResult(42); }
async (ct) => await Task.FromResult(42).ConfigureAwait(false)
```

## Not matched patterns

```cs
// Already correct — no async/await
_ => Task.FromResult(42)  // ✅

// Async lambda with real async work
async () => await GetValueFromDatabaseAsync()  // ✅ not Task.FromResult

// Multiple statements in the block body
async () => { var x = Compute(); return await Task.FromResult(x); }  // ✅ multiple statements

// Non-lambda async method
async Task<int> GetValueAsync() => await Task.FromResult(42);  // ✅ not a lambda
```

## Fix / Mitigation

Remove the `async` modifier and the `await` keyword. For lambdas with an unused parameter, replace it with a discard (`_`). The code fix also strips `.ConfigureAwait(...)` since it has no effect without `await`.

```cs
// Before:
async (ct) => await Task.FromResult(42)

// After:
_ => Task.FromResult(42)

// Before (parameterless):
async () => await Task.FromResult("hello")

// After:
() => Task.FromResult("hello")

// Before (parameter used in expression):
async ct => await Task.FromResult(ct.IsCancellationRequested)

// After (parameter name preserved):
ct => Task.FromResult(ct.IsCancellationRequested)

// Before (with ConfigureAwait):
async (ct) => await Task.FromResult(42).ConfigureAwait(false)

// After (ConfigureAwait stripped):
_ => Task.FromResult(42)
```

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA020: Remove redundant async/await on Task.FromResult
dotnet_diagnostic.DSA020.severity = error
```

## Code sample

```csharp
public class EventHandlerSetup
{
    public void Configure()
    {
        // this WILL trigger the rule
        Register(async (ct) => await Task.FromResult(GetDefaultValue()));

        // this WILL NOT trigger the rule (already optimized)
        Register(_ => Task.FromResult(GetDefaultValue()));
    }

    private void Register(Func<CancellationToken, Task<int>> handler) { }
    private int GetDefaultValue() => 42;
}
```

---

# DSA021

Entity Framework queries should be tagged with TagWith or TagWithCallSite for traceability.

- **Category**: Best Practice
- **Severity**: Warning ⚠
- **Related rules**: none

## Description

This rule fires when an Entity Framework Core LINQ query is materialized (via `ToListAsync`, `FirstOrDefaultAsync`, `CountAsync`, `ToList`, and similar terminal operations) without a preceding call to `TagWith()` or `TagWithCallSite()` anywhere in the query chain. The analyzer also traces queries received as `IQueryable<T>` parameters, performing a best-effort cross-method analysis to determine whether callers have already applied a tag before passing the query.

**What TagWithCallSite and TagWith do**: Entity Framework Core translates LINQ queries into SQL. By default, there is no link in the generated SQL that identifies which line of application code produced it. `TagWithCallSite()` (introduced in EF Core 6.0) automatically embeds the source file path, line number, and calling method name into the generated SQL as a comment; `TagWith("label")` allows adding a custom descriptive label. Database monitoring tools, query profilers, and log aggregators then surface these tags alongside query performance metrics and error reports, creating a direct traceable link from database activity back to the responsible code path.

### Rapid Root Cause Analysis and Incident Response

In production environments, when a slow, failing, or malicious query pattern is detected in database logs, monitoring dashboards, or intrusion detection systems, the absence of a source-identifying tag forces incident responders to manually reverse-engineer the generated SQL back to application source code. This reverse-engineering process is error-prone, time-consuming, and potentially catastrophic under the time pressure of an active security incident or a cascading production failure. Tagged queries eliminate this bottleneck entirely: the responder reads the tag from the database log entry and immediately identifies the originating code path, reducing mean time to resolution from hours to seconds.

### CWE-400 Mitigation: Uncontrolled Resource Consumption

In the context of cybersecurity, query tagging directly supports mitigation of **CWE-400 (Uncontrolled Resource Consumption)**. When a resource-exhaustion attack targets the database layer — through deliberately expensive queries, parameter manipulation that disables indexing, or amplification patterns that multiply query volume — tagged queries enable rapid identification and surgical isolation of the offending code path. Without tags, the only recourse during such an attack may be to throttle or shut down the entire application; with tags, responders can isolate and block the specific query origin while the rest of the application continues operating.

### IEC 62443 Certification: Traceability and Integrity

In the context of **IEC 62443 certification**, query tagging is vital for two foundational requirements:

- **FR 2 — Use Control (Traceability)**: tagged queries create auditable chains from observed database activity back to the originating application code and the identity context in which it executed. This traceability is essential for demonstrating that all data access operations are attributable to specific software components and, transitively, to specific user actions or automated processes.
- **FR 3 — System Integrity**: tagged queries enable integrity verification of the database access layer by making it possible to detect unauthorized or unexpected query patterns. When every legitimate query carries a known tag, the absence of a tag (or the presence of an unknown tag) becomes a detectable anomaly, supporting both real-time integrity monitoring and post-incident forensic analysis.

## See also

- [MITRE, CWE-400: Uncontrolled Resource Consumption](https://cwe.mitre.org/data/definitions/400.html)
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 2 (Use Control), FR 3 (System Integrity)
- [Microsoft: Query tags in Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/querying/tags)
- [Microsoft: TagWithCallSite](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.tagwithcallsite)

## Matched patterns

```cs
// All of these are flagged:
_context.Users.ToListAsync()
_context.Users.Where(u => u.IsActive).ToListAsync()
_context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).FirstOrDefaultAsync()
_context.Users.AsNoTracking().Where(u => u.IsActive).CountAsync()
_context.Users.Where(u => u.IsActive).ToList()

// Variable without tag:
var query = _context.Users.Where(u => u.IsActive);
await query.ToListAsync();  // flagged

// Parameter without tag at any caller:
async Task<List<User>> Execute(IQueryable<User> query) {
    return await query.ToListAsync();  // flagged if callers don't tag
}
```

## Not matched patterns

```cs
// Tagged with TagWithCallSite:
_context.Users.TagWithCallSite().Where(u => u.IsActive).ToListAsync()  // not flagged

// Tagged with TagWith:
_context.Users.Where(u => u.IsActive).TagWith("GetActiveUsers").ToListAsync()  // not flagged

// Tag before filter:
_context.Users.TagWithCallSite().Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync()  // not flagged

// In-memory collection (not Entity Framework):
users.Where(u => u.IsActive).ToList()  // not flagged

// Variable with tagged initializer:
var query = _context.Users.TagWithCallSite().Where(u => u.IsActive);
await query.ToListAsync();  // not flagged

// Parameter tagged at all call sites:
async Task<List<User>> Execute(IQueryable<User> query) {
    return await query.ToListAsync();  // not flagged if all callers tag before passing
}
Execute(_context.Users.TagWithCallSite().Where(u => u.IsActive));
```

## Fix / Mitigation

Two code fixes are available:

**Add TagWithCallSite()**: Inserts `.TagWithCallSite()` immediately before the terminal materialization method. This is the recommended fix for most cases, as it automatically embeds the file path, line number, and calling method name — no manual labeling required.

```cs
// Before:
await _context.Users.Where(u => u.IsActive).ToListAsync();

// After:
await _context.Users.Where(u => u.IsActive).TagWithCallSite().ToListAsync();
```

**Add TagWith("...")**: Inserts `.TagWith("TODO: describe this query")` immediately before the terminal method, with a placeholder string for the developer to replace with a meaningful label. This is useful when a custom label is preferred over automatic call-site information.

```cs
// Before:
await _context.Users.Where(u => u.IsActive).ToListAsync();

// After:
await _context.Users.Where(u => u.IsActive).TagWith("TODO: describe this query").ToListAsync();
```

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA021: Entity Framework queries should be tagged with TagWith or TagWithCallSite for traceability
dotnet_diagnostic.DSA021.severity = error
```

## Code sample

```csharp
public class UserService
{
    private readonly MyDbContext _context;

    public UserService(MyDbContext context) { _context = context; }

    public async Task<List<User>> GetActiveUsers_NotOk()
    {
        // this WILL trigger the rule — no tag
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<List<User>> GetActiveUsers_Ok_CallSite()
    {
        // this WILL NOT trigger the rule — TagWithCallSite embeds file/line/method
        return await _context.Users
            .TagWithCallSite()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<List<User>> GetActiveUsers_Ok_CustomTag()
    {
        // this WILL NOT trigger the rule — TagWith provides a custom label
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .TagWith("UserService.GetActiveUsers: active users sorted by name")
            .ToListAsync();
    }
}
```

---

# DSA022

Hoist loop-invariant expression out of inner loop.

- **Category**: Performance
- **Severity**: Warning ⚠
- **Related rules**: none

## Description

This rule fires when an arithmetic or bitwise expression inside a loop body references only variables that are not modified within that loop iteration, making the expression loop-invariant. The expression is redundantly recomputed on every iteration, wasting CPU cycles on identical results.

While modern compilers perform Loop-Invariant Code Motion (LICM) as a JIT optimization, this transformation is not guaranteed across all .NET runtimes, JIT tiers, and platforms. Explicitly hoisting the computation to a variable declared before the loop ensures deterministic performance regardless of optimizer quality.

### Performance and Cache Locality

In tight numerical loops, redundant multiplications and additions inside the innermost loop can degrade throughput by orders of magnitude, particularly when the loop operates over large data sets where the redundant operations compound into millions of wasted cycles. Hoisting invariant sub-expressions also improves cache locality by reducing the instruction footprint of the hot loop body, allowing the CPU instruction cache to retain the performance-critical path more effectively.

### CWE-405 Mitigation: Asymmetric Resource Consumption

In the context of cybersecurity, this relates to **CWE-405 (Asymmetric Resource Consumption / Amplification)**: an attacker who can influence the iteration count of a loop containing unnecessarily repeated computations can amplify the cost of each request, turning a linear-time operation into a practical denial-of-service vector.

### IEC 62443 Certification: Resource Availability

In the context of **IEC 62443 certification**, computational efficiency in control loops directly supports **FR 7 (Resource Availability)** by ensuring that processing resources are not wasted on redundant computations that could be avoided with straightforward code restructuring.

## See also

- [MITRE, CWE-405: Asymmetric Resource Consumption (Amplification)](https://cwe.mitre.org/data/definitions/405.html)
- [IEC 62443-3-3: System security requirements and security levels](https://webstore.iec.ch/en/publication/7033) — FR 7 (Resource Availability)
- [Wikipedia: Loop-invariant code motion](https://en.wikipedia.org/wiki/Loop-invariant_code_motion)

## Matched patterns

```cs
// Simple invariant multiplication inside a for loop:
for (int i = 0; i < arr.Length; i++)
{
    arr[i] = a * b + i;  // 'a * b' is flagged — neither a nor b changes within the loop
}

// Nested loops — outer variable computation in inner loop:
for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        output[y * width + x] = 0;  // 'y * width' is flagged in the inner (x) loop
    }
}

// While loop with invariant expression:
while (i < 100)
{
    result = a + b;  // flagged
    i++;
}

// Do-while loop:
do
{
    result = a * b;  // flagged
    i++;
} while (i < 100);
```

## Not matched patterns

```cs
// Expression uses the loop variable:
for (int i = 0; i < n; i++)
{
    arr[i] = i * 2;  // NOT flagged — 'i' is modified each iteration
}

// Variable assigned inside the loop:
for (int i = 0; i < n; i++)
{
    x = i + 1;
    val = x * a;  // NOT flagged — 'x' is modified inside the loop
}

// Expression in loop condition or incrementor:
for (int i = 0; i < a * b; i++) { }  // NOT flagged — condition expressions are excluded

// Expression contains a method call:
for (int i = 0; i < n; i++)
{
    arr[i] = (int)(Math.Sin(a) + i);  // NOT flagged — method calls may have side effects
}

// Expression contains array or indexer access:
for (int i = 0; i < n; i++)
{
    val = data[0] + a;  // NOT flagged — array access may depend on mutable state
}

// ForEach iteration variable in expression:
foreach (var x in list)
{
    total = x * 2;  // NOT flagged — 'x' changes each iteration
}
```

## Fix / Mitigation

The code fix extracts the loop-invariant expression into a local variable declared immediately before the loop and replaces all occurrences within the loop body with the new variable.

```cs
// Before:
for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        output[y * width + x] = 0;
    }
}

// After:
for (int y = 0; y < height; y++)
{
    var hoisted_y_width = y * width;
    for (int x = 0; x < width; x++)
    {
        output[hoisted_y_width + x] = 0;
    }
}
```

## Rule configuration

In order to change the severity level of this rule, change/add this line in the `.editorconfig` file:

```
# DSA022: Hoist loop-invariant expression out of inner loop
dotnet_diagnostic.DSA022.severity = error
```

## Code sample

```csharp
public class ImageProcessor
{
    public void Process_NotOk(int outC, int outH, int outW, float[] conv, float[] output, int inW)
    {
        for (int oc = 0; oc < outC; oc++)
        {
            int oBase = oc * outH * outW;
            for (int py = 0; py < outH; py++)
            {
                int y0 = py * 2;
                for (int px = 0; px < outW; px++)
                {
                    // 'y0 * inW' and 'oBase + py * outW' are both
                    // loop-invariant within the px loop — DSA022 fires
                    int i00 = y0 * inW + px * 2;
                    output[oBase + py * outW + px] = conv[i00];
                }
            }
        }
    }

    public void Process_Ok(int outC, int outH, int outW, float[] conv, float[] output, int inW)
    {
        for (int oc = 0; oc < outC; oc++)
        {
            int oBase = oc * outH * outW;
            for (int py = 0; py < outH; py++)
            {
                int y0 = py * 2;
                var row0Start = y0 * inW;        // hoisted before the px loop
                var pyOffset = oBase + py * outW; // hoisted before the px loop
                for (int px = 0; px < outW; px++)
                {
                    int i00 = row0Start + px * 2;
                    output[pyOffset + px] = conv[i00];
                }
            }
        }
    }
}
```

---

# Installation

Just download and install the NuGet package  
[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

[https://www.nuget.org/packages/DogmaSolutions.Analyzers](https://www.nuget.org/packages/DogmaSolutions.Analyzers)