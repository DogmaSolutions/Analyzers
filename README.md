# Dogma Solutions Roslyn Analyzers
[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

A set of Roslyn Analyzer aimed to enforce some design good practices and code quality (QA) rules.

# Rules structure
This section describes the rules included into this package.

Every rule is accompanied by the following information and clues:
- **Category** → identify the area of interest of the rule, and can have one of the following values: _Design / Naming / Style / Usage / Performance / Security_ 
- **Severity** → state the default severity level of the rule. The severity level can be changed by editing the _.editorconfig_ file used by the project/solution. Possible values are enumerated by the [DiagnosticSeverity enum](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnosticseverity)
- **Description** → a short description about the rule aim.
- **Motivation and fix** → a detailed explanation of the detected issue, and a brief description on how to change your code in order to solve it.
- **See also** → a list of similar/related rules.


# Rules list
| Id                | Category    |Description|Severity|Is enabled|Code fix|
|-------------------|-------------|-----------|:------:|:--------:|:------:|
| [DSA001](#dsa001) | Design      |[WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression**.|⚠|✅|❌|
| [DSA002](#dsa002) | Design      |[WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ fluent query**.|⚠|✅|❌|
| [DSA003](#dsa003) | Code Smells |Usually, business logics distinguish between "string with content", and "string NULL or without meaningfull content". Thus, statistically speaking, almost every call to `string.IsNullOrEmpty` could or should be replaced by a call to `string.IsNullOrWhiteSpace`, because in the large majority of cases, a string composed by only spaces, tabs, and return chars is not considered valid because it doesn't have "meaningfull content". In most cases, `string.IsNullOrEmpty` is used by mistake, or has been written when `string.IsNullOrWhiteSpace` was not available. Don't use `string.IsNullOrEmpty`. Use `string.IsNullOrWhiteSpace` instead.|⚠|✅|❌|

       
# DSA001 -  Don't use Entity Framework to launch LINQ queries in a WebApi controller
- **Category**: Design
- **Severity**: Warning ⚠
- **Description**: [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression**.
- **Motivation and fix**: A [WebApi controller method](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) is using [Entity Framework DbContext](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext) to directly manipulate data through a LINQ query expression. WebApi controllers should not contain data-manipulation business logics. Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.
- **Knowledge base**: this is a typical violation of the ["Single Responsibility" rule](https://en.wikipedia.org/wiki/Single-responsibility_principle) of the ["SOLID" principles](https://en.wikipedia.org/wiki/SOLID). In order to fix the problem, the code could be modified in order to rely on the ["Indirection pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Indirection) and maximize the ["Low coupling evalutative pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Low_coupling) of the ["GRASP"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)) principles.
- **See also**: [DSA002](#dsa002)

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
 public IEnumerable<MyEntity> GetAll0()
 {
     // this WILL trigger an error
     var query = from entities in DbContext.MyEntities where entities.Id > 0 select entities;
     return query.ToList(); 
 }

 [HttpPost]
 public IEnumerable<long> GetAll1()
 {
     // this WILL NOT trigger an error
     var query = DbContext.MyEntities.Where(entities => entities.Id > 0).Select(entities=>entities.Id);
     return query.ToList(); 
 }
}
```

# DSA002 - Don't use an Entity Framework `DbSet` to launch queries in a WebApi controller
- **Category**: Design
- **Severity**: Warning ⚠
- **Description**: [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ fluent query**.
- **Motivation and fix**: A [WebApi controller method](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) is using [Entity Framework DbSet](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbset-1) to directly manipulate data through a LINQ fluent query. WebApi controllers should not contain data-manipulation business logics. Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.
- **Knowledge base**: this is a typical violation of the ["Single Responsibility" rule](https://en.wikipedia.org/wiki/Single-responsibility_principle) of the ["SOLID" principles](https://en.wikipedia.org/wiki/SOLID). In order to fix the problem, the code could be modified in order to rely on the ["Indirection pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Indirection) and maximize the ["Low coupling evalutative pattern"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)#Low_coupling) of the ["GRASP"](https://en.wikipedia.org/wiki/GRASP_(object-oriented_design)) principles.
- **See also**: [DSA001](#dsa001)

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
     // this WILL NOT trigger an error
     var query = from entities in DbContext.MyEntities where entities.Id > 0 select entities;
     return query.ToList(); 
 }

 [HttpPost]
 public IEnumerable<long> GetAll1()
 {
     // this WILL trigger an error
     var query = DbContext.MyEntities.Where(entities => entities.Id > 0).Select(entities=>entities.Id);
     return query.ToList(); 
 }
}
```



## DSA003 - Don't use `String.IsNullOrEmpty`. Use `IsNullOrWhiteSpace` instead
- **Category** → Code smells
- **Severity** → Warning ⚠
- **Description** → Usually, business logics distinguish between "string with content", and "string NULL or without meaningfull content". Thus, statistically speaking, almost every call to `string.IsNullOrEmpty` could or should be replaced by a call to `string.IsNullOrWhiteSpace`, because in the large majority of cases, a string composed by only spaces, tabs, and return chars is not considered valid because it doesn't have "meaningfull content". In most cases, `string.IsNullOrEmpty` is used by mistake, or has been written when `string.IsNullOrWhiteSpace` was not available. Don't use `string.IsNullOrEmpty`. Use `string.IsNullOrWhiteSpace` instead.
- **Motivation and fix** → Don't use `string.IsNullOrEmpty`. Use `string.IsNullOrWhiteSpace` instead.




# Installation
- NuGet package (recommended) → [https://www.nuget.org/packages/DogmaSolutions.Analyzers](https://www.nuget.org/packages/DogmaSolutions.Analyzers)
