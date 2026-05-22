# Dogma Solutions Roslyn Analyzers

[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

A set of C# Roslyn analyzers that catch bugs, design flaws, and security pitfalls at compile time -- before they reach code review or production.

The package currently ships 20+ rules across six categories (Design, Security, Performance, Code Smells, Bug, Best Practice); most include an automatic code fix.  
Rules range from straightforward code-smell detection (e.g. `DateTime.Now` instead of `DateTime.UtcNow`) to cross-method semantic analysis (e.g. Entity Framework queries missing `TagWith`, check-then-act race conditions on concurrent collections, or loop-invariant expressions that should be hoisted).

Install via NuGet and every rule is enforced automatically during compilation, with severity levels configurable through `.editorconfig`.

---

# Versioning criteria
The NuGet package follows the conventions of [Semantic Versioning 2.0.0](https://semver.org/).  
Cit.:
```
Given a version number MAJOR.MINOR.PATCH, increment the:
1. MAJOR version when you make incompatible API changes
2. MINOR version when you add functionality in a backward compatible manner
3. PATCH version when you make backward compatible bug fixes

Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.
```
---

# Installation

Just download and install the NuGet package  
[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

[https://www.nuget.org/packages/DogmaSolutions.Analyzers](https://www.nuget.org/packages/DogmaSolutions.Analyzers)

---

# Rules structure

This section describes the rules included in this package.

Every rule is accompanied by the following information and clues:

- **Category** → identify the area of interest of the rule, and can have one of the following values: _Design / Naming / Style / Usage / Performance / Security_
- **Severity** → state the default severity level of the rule. The severity level can be changed by editing the _.editorconfig_ file used by the project/solution. Possible values are enumerated by
  the [DiagnosticSeverity enum](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnosticseverity)
- **Description, motivations and fixes** → a detailed explanation of the detected issue, and a brief description on how to change your code in order to solve it.
- **See also** → a list of similar/related rules, or related knowledge base

# Rules list

| Id | Category | Description | Default severity | Is enabled | Code fix |
|----|----------|-------------|------------------|------------|----------|
| [DSA001](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA001.md) | Design | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression**. | ⚠ Warning | ✅ | ❌ |
| [DSA002](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA002.md) | Design | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ fluent query**. | ⚠ Warning | ✅ | ❌ |
| [DSA003](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA003.md) | Code Smells | Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty` | ⚠ Warning | ✅ | ✅ |
| [DSA004](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA004.md) | Code Smells | Use `DateTime.UtcNow` instead of `DateTime.Now` | ⚠ Warning | ✅ | ✅ |
| [DSA005](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA005.md) | Code Smells | Potential non-deterministic point-in-time execution | ⛔ Error | ✅ | ✅ |
| [DSA006](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA006.md) | Code Smells | General exceptions should not be thrown by user code | ⛔ Error | ✅ | ❌ |
| [DSA007](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA007.md) | Code Smells | When initializing a lazy field, use a robust locking pattern, i.e. the "if-lock-if" (aka "double checked locking") | ⚠ Warning | ✅ | ❌ |
| [DSA008](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA008.md) | Bug | The Required Attribute has no impact on a not-nullable DateTime | ⛔ Error | ✅ | ✅ |
| [DSA009](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA009.md) | Bug | The Required Attribute has no impact on a not-nullable DateTimeOffset | ⛔ Error | ✅ | ✅ |
| [DSA011](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA011.md) | Design | Avoid lazily initialized, self-contained, static singleton properties | ⚠ Warning | ✅ | ❌ |
| [DSA012](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA012.md) | Design | Avoid the "if not exists, then insert" check-then-act antipattern on database types (TOCTOU) | ⚠ Warning | ✅ | ❌ |
| [DSA013](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA013.md) | Security | Minimal API endpoints should have an explicit authorization configuration | ⚠ Warning | ✅ | ✅ |
| [DSA014](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA014.md) | Security | Minimal API endpoints on route groups should have an explicit authorization configuration | ⚠ Warning | ✅ | ✅ |
| [DSA015](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA015.md) | Security | Minimal API endpoints on parameterized route builders should have an explicit authorization configuration | ⚠ Warning | ✅ | ✅ |
| [DSA016](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA016.md) | Code Smells | Avoid repeated invocation of the same enumeration method with identical arguments | ⚠ Warning | ✅ | ✅ |
| [DSA017](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA017.md) | Design | Use the collection's atomic operation instead of the check-then-act pattern | ⚠ Warning | ✅ | ✅ |
| [DSA018](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA018.md) | Design | Protect the check-then-act pattern with a lock or use a collection with built-in duplicate handling | ⚠ Warning | ✅ | ❌ |
| [DSA019](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA019.md) | Code Smells | Avoid repeated deeply nested member access chains | ⚠ Warning | ✅ | ✅ |
| [DSA020](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA020.md) | Code Smells | Remove redundant async/await on `Task.FromResult` | ⚠ Warning | ✅ | ✅ |
| [DSA021](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA021.md) | Best Practice | Entity Framework queries should be tagged with TagWith or TagWithCallSite for traceability | ⚠ Warning | ✅ | ✅ |
| [DSA022](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA022.md) | Performance | Hoist loop-invariant expression out of inner loop | ⚠ Warning | ✅ | ✅ |
| [DSA023](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA023.md) | Best Practice | Use `Path.Combine` instead of string concatenation to build file system paths | ⚠ Warning | ✅ | ✅ |
| [DSA024](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA024.md) | Best Practice | Use `Path.Combine` instead of string concatenation for path-like parameters | ⚠ Warning | ✅ | ✅ |
| [DSA025](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA025.md) | Performance | Use structured logging template instead of interpolated string | ⚠ Warning | ✅ | ✅ |
| [DSA026](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA026.md) | Bug | Use nearest scope CancellationToken | ⚠ Warning | ✅ | ✅ |
| [DSA027](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA027.md) | Performance | Replace string concatenation in loops with `StringBuilder` | ⚠ Warning | ✅ | ✅ |
| [DSA028](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA028.md) | Performance | Prefer `ToArray()` over `ToList()` when return type is a read-only interface | ⚠ Warning | ✅ | ✅ |
| [DSA029](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA029.md) | Bug | The Required Attribute has no impact on a not-nullable value type | ⚠ Warning | ✅ | ✅ |

---

# Contributing

Contributions are welcome! Please read the [Contributing Guidelines](CONTRIBUTING.md) before submitting a pull request.

# Security

To report a security vulnerability, please follow the instructions in [SECURITY.md](SECURITY.md). Do not open a public issue for security reports.

# Code of Conduct

This project follows the [Contributor Covenant v2.1](https://www.contributor-covenant.org/version/2/1/code_of_conduct/). See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) for details.

# License

This project is licensed under the [MIT License](LICENSE).
