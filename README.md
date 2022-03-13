# Dogma Solutions Roslyn Analyzers
[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

A set of Roslyn Analyzer aimed to enforce some design good practices and code quality (QA) rules.
# Rules
This section describes the rules included into this package.

Every rule is accompanied by the following information and clues:
- **Category** → identify the area of interest of the rule, and can have one of the following values: _Design / Naming / Style / Usage / Performance / Security_ 
- **Severity** → state the default severity level of the rule. The severity level can be changed by editing the _.editorconfig_ file used by the project/solution. Possible values are enumerated by the [DiagnosticSeverity enum](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnosticseverity)
- **Description** → a short description about the rule aim.
- **Motivation and fix** → a detailed explanation of the detected issue, and a brief description on how to change your code in order to solve it.
- **See also** → a list of similar/related rules.

       
## DSA001
- **Category** → Design
- **Severity** → Warning
- **Description** → [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a LINQ query expression.
- **Motivation and fix** → A [WebApi controller method](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) is using [Entity Framework DbContext](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontext) to directly manipulate data through a LINQ query expression. WebApi controllers should not contain data-manipulation business logics. Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.
- **See also** → DSA002

## DSA002
- **Category** → Design
- **Severity** → Warning
- **Description** → [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a LINQ fluent query.
- **Motivation and fix** → A [WebApi controller method](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) is using [Entity Framework DbSet](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbset-1) to directly manipulate data through a LINQ fluent query. WebApi controllers should not contain data-manipulation business logics. Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.
- **See also** → DSA001
                                               

# Installation
- NuGet package (recommended) → [https://www.nuget.org/packages/DogmaSolutions.Analyzers](https://www.nuget.org/packages/DogmaSolutions.Analyzers)