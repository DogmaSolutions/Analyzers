# Dogma Solutions Roslyn Analyzers and Code Fixers

[![DogmaSolutions.Analyzers on NuGet](https://img.shields.io/nuget/v/DogmaSolutions.Analyzers.svg)](https://www.nuget.org/packages/DogmaSolutions.Analyzers/)

A set of C# Roslyn analyzers, code fixers, and refactorings that catch bugs, design flaws, and security pitfalls at compile time -- before they reach code review or production.

The package currently ships many rules across six categories:
- Design
- Security
- Performance
- Code Smell
- Bug
- Best Practice
 
Most analyzers include an automatic code fix.
All of them include tools for adding code-review comments.
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

- **Category** → identify the area of interest of the rule, and can have one of the following values: _Design / Code Smell / Best Practice / Bug / Security / Performance_
- **Severity** → state the default severity level of the rule (`error`, `warning`, `suggestion`, ...). It can be changed per project/solution, see [Configuring the rules](#configuring-the-rules)
- **Description, motivations and fixes** → a detailed explanation of the detected issue, and a brief description on how to change your code in order to solve it.
- **See also** → a list of similar/related rules, or related knowledge base

# Configuring the rules

Every rule is configured through the `.editorconfig` file used by the project/solution, with the standard Roslyn syntax:

```ini
# severity of a rule
dotnet_diagnostic.DSA012.severity = warning

# every rule of the package at once
dotnet_analyzer_diagnostic.category-Design.severity = suggestion
```

The accepted severities are the standard `.editorconfig` ones: `error`, `warning`, `suggestion`, `silent`, `none` (the rule is disabled) and `default` (the severity chosen by the rule's author). They are case-insensitive.

Some rules have additional options, written in the same section with the form `dotnet_diagnostic.<RuleId>.<option_name> = <value>`, and documented in the page of the rule. Lists are comma separated.
An option that is missing, blank, malformed (e.g. `abc` where a number is expected) or out of range is ignored, and the documented default is used instead: the analysis never fails because of the configuration. If a setting seems to have no effect, check its spelling against the page of the rule.

A single occurrence can be silenced with `#pragma warning disable DSAxxx` or `[SuppressMessage]`.

# Rules list

| Id | Category | Description | Default severity | Is enabled | Refactoring available | Code-review tools available |
|----|----------|-------------|------------------|------------|----------------------|----------------------|
| [DSA001](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA001.md) | Design | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ query expression**. | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA002](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA002.md) | Design | [WebApi controller methods](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase) should not contain data-manipulation business logics through a **LINQ fluent query**. | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA003](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA003.md) | Code Smell | Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty` | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA004](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA004.md) | Code Smell | Use `DateTime.UtcNow` instead of `DateTime.Now` | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA005](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA005.md) | Code Smell | Potential non-deterministic point-in-time execution | ⛔ Error | ✅ | ✅ | ✅ |
| [DSA006](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA006.md) | Code Smell | General exceptions should not be thrown by user code | ⛔ Error | ✅ | ❌ | ✅ |
| [DSA007](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA007.md) | Code Smell | When initializing a lazy field, use a robust locking pattern, i.e. the "if-lock-if" (aka "double checked locking") | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA008](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA008.md) | Bug | The `RequiredAttribute` has no impact on a not-nullable `DateTime` | ⛔ Error | ✅ | ✅ | ✅ |
| [DSA009](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA009.md) | Bug | The `RequiredAttribute` has no impact on a not-nullable `DateTimeOffset` | ⛔ Error | ✅ | ✅ | ✅ |
| [DSA010](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA010.md) | Code Smell | A lock around a single field access does not protect your logic | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA011](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA011.md) | Design | Avoid lazily initialized, self-contained, static singleton properties | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA012](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA012.md) | Design | Avoid the "if not exists, then insert" check-then-act antipattern on database types (TOCTOU) | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA013](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA013.md) | Security | Minimal API endpoints should have an explicit authorization configuration | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA014](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA014.md) | Security | Minimal API endpoints on route groups should have an explicit authorization configuration | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA015](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA015.md) | Security | Minimal API endpoints on parameterized route builders should have an explicit authorization configuration | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA016](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA016.md) | Code Smell | Avoid repeated invocation of the same enumeration method with identical arguments | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA017](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA017.md) | Design | Use the collection's atomic operation instead of the check-then-act pattern | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA018](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA018.md) | Design | Protect the check-then-act pattern with a lock or use a collection with built-in duplicate handling | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA019](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA019.md) | Code Smell | Avoid repeated deeply nested member access chains | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA020](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA020.md) | Code Smell | Remove redundant async/await on `Task.FromResult` | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA021](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA021.md) | Best Practice | Entity Framework queries should be tagged with `TagWith` or `TagWithCallSite` for traceability | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA022](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA022.md) | Performance | Hoist loop-invariant expression out of inner loop | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA023](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA023.md) | Best Practice | Use `Path.Combine` instead of string concatenation to build file system paths | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA024](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA024.md) | Best Practice | Use `Path.Combine` instead of string concatenation for path-like parameters | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA025](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA025.md) | Performance | Use structured logging template instead of interpolated string | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA026](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA026.md) | Bug | Use nearest scope `CancellationToken` | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA027](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA027.md) | Performance | Replace string concatenation in loops with `StringBuilder` | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA028](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA028.md) | Performance | Prefer `ToArray()` over `ToList()` when return type is a read-only interface | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA029](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA029.md) | Bug | The `RequiredAttribute` has no impact on a not-nullable value type | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA030](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA030.md) | Best Practice | Entity Framework queries should explicitly specify a change tracking strategy | 💡 Suggestion | ✅ | ✅ | ✅ |
| [DSA031](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA031.md) | Performance | Use `AsNoTracking` for Entity Framework queries that do not require change tracking | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA032](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA032.md) | Code Smell | Avoid duplicated string literals in the same method body | 💡 Suggestion | ✅ | ✅ | ✅ |
| [DSA033](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA033.md) | Code Smell | File exceeds maximum line count | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA034](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA034.md) | Code Smell | Single-type file exceeds maximum line count | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA035](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA035.md) | Performance | Hoist loop-invariant reflection call out of loop | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA036](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA036.md) | Performance | Extract Regex with constant pattern to a static readonly field | ⚠ Warning | ✅ | ✅ | ✅ |
| [DSA037](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA037.md) | Bug | A `[ThreadStatic]` `Random` field must not have a field initializer (only the first thread is initialized) | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA038](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA038.md) | Bug | Avoid seeding `Random` with a time-based or constant value | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA039](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA039.md) | Security | Avoid modulo bias when reducing cryptographic random bytes into a range (use `RandomNumberGenerator.GetInt32`) | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA040](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA040.md) | Security | Do not use `System.Random` for security-sensitive values (token/salt/nonce/key); use `RandomNumberGenerator` | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA041](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA041.md) | Code Smell | Avoid switching over an enum with many members (`max_enum_members`, default 4); prefer the Strategy pattern | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA042](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA042.md) | Code Smell | Avoid repeating a segment in a namespace (e.g. `My.Project.Models.Models`) | ⚠ Warning | ✅ | ❌ | ✅ |
| [DSA043](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA043.md) | Best Practice | A complex class (cyclomatic complexity, lines, ... configurable) should inject or use a logger (`Microsoft.Extensions.Logging.ILogger`) | 💡 Suggestion | ✅ | ❌ | ✅ |
| [DSA044](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA044.md) | Best Practice | Use the available logger (Microsoft.Extensions.Logging, Serilog, NLog, log4net, ...) instead of `Console.Write*` or `Debug.Write*` | 💡 Suggestion | ✅ | ✅ | ✅ |
| [DSA045](https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA045.md) | Best Practice | A pure data carrier (DTO: properties only, no logic, no explicit constructor, base types included) should be excluded from the code coverage with `[ExcludeFromCodeCoverage]` | ⚠ Warning | ✅ | ✅ | ✅ |

# Refactorings list

In addition to analyzer rules, this package ships code refactorings that appear in the IDE lightbulb/screwdriver menu independently of any diagnostic.

| Id | Description |
|----|-------------|
| [DSR001](docs/refactorings/DSR001.md) | Insert `.TagWithCallSite()` or `.TagWith("...")` before any EF query terminal method |
| [DSR002](docs/refactorings/DSR002.md) | Extract any string literal to a local constant or class field constant |
| [DSR003](docs/refactorings/DSR003.md) | Split file with multiple top-level types into one file per type |
| [DSR004](docs/refactorings/DSR004.md) | Split a single-type file into partial files by visibility or by topic |

---

# Recommended settings

The rules of the package are configured through the `.editorconfig` file (see [Configuring the rules](#configuring-the-rules)), and the right level of strictness depends on the project: a twenty-year-old code base cannot be brought to the standards of a greenfield service overnight, and a code base written by AI agents needs far stricter guardrails than one written by a seasoned team.

The following sections propose three ready-to-use profiles. Each one is a complete `.editorconfig` fragment that lists **every rule** of the package with its severity, together with **every option of the rules that have any**, written with its default value, so that you can copy the profile, remove what you do not need, and tune the values on the fly. The options are identical in the three profiles; what changes is the severity of the rules.

Paste the fragment in the `.editorconfig` of your solution, under a `[*.cs]` section:

```ini
[*.cs]
# ...the profile you chose
```

A profile is a starting point, not a certification: a rule can always be silenced where it does not apply (`none`, or a `#pragma warning disable`), and a rule that reports too many violations on a legacy code base can be moved one step down for a while and tightened back later.

| Profile | Severities | For |
|:--------|:-----------|:----|
| [Lenient / Entry-level](#lenient--entry-level) | the defaults of the rules, with `error` downgraded to `warning` | very old projects, entry-level quality assurance |
| [Good / Professional](#good--professional) | the defaults of the rules | modern projects, good quality assurance |
| [SSDLC and/or AI-Driven development](#ssdlc-secure-software-development-lifecycle-andor-ai-driven-development) | `error` everywhere, except `DSA030`, `DSA033` and `DSA034` (`warning`) | top level quality assurance, secure and AI-driven development |


## Lenient / Entry-level

For very old projects and entry-level quality assurance, with very few cybersecurity constraints and robustness guarantees.

Nothing breaks the build: every rule is reported at its default severity, except the ones that are errors by default, which are downgraded to `warning`. The code base is nudged towards better practices, without ever blocking a build, a hotfix or a team that is still learning the rules. A good fit to start adopting the package on a legacy solution: fix the warnings over time, then move to the next profile.

```ini
# DSA001: WebApi controller methods should not contain data-manipulation business logics through a **LINQ query expression**.
dotnet_diagnostic.DSA001.severity = warning

# DSA002: WebApi controller methods should not contain data-manipulation business logics through a **LINQ fluent query**.
dotnet_diagnostic.DSA002.severity = warning

# DSA003: Use String.IsNullOrWhiteSpace instead of String.IsNullOrEmpty
dotnet_diagnostic.DSA003.severity = warning

# DSA004: Use DateTime.UtcNow instead of DateTime.Now
dotnet_diagnostic.DSA004.severity = warning

# DSA005: Potential non-deterministic point-in-time execution
dotnet_diagnostic.DSA005.severity = warning

# DSA006: General exceptions should not be thrown by user code
dotnet_diagnostic.DSA006.severity = warning

# DSA007: When initializing a lazy field, use a robust locking pattern, i.e. the "if-lock-if" (aka "double checked locking")
dotnet_diagnostic.DSA007.severity = warning

# DSA008: The RequiredAttribute has no impact on a not-nullable DateTime
dotnet_diagnostic.DSA008.severity = warning

# DSA009: The RequiredAttribute has no impact on a not-nullable DateTimeOffset
dotnet_diagnostic.DSA009.severity = warning

# DSA010: A lock around a single field access does not protect your logic
dotnet_diagnostic.DSA010.severity = warning
dotnet_diagnostic.DSA010.report_reads = true
dotnet_diagnostic.DSA010.report_writes = true
dotnet_diagnostic.DSA010.report_shared_locks = true
dotnet_diagnostic.DSA010.report_lock_on_this = false
dotnet_diagnostic.DSA010.report_tearable_types = true
dotnet_diagnostic.DSA010.report_volatile_fields = true
dotnet_diagnostic.DSA010.report_readonly_fields = true
dotnet_diagnostic.DSA010.skip_signaled_locks = true
dotnet_diagnostic.DSA010.body_shape = sole_statement
dotnet_diagnostic.DSA010.member_kinds = accessor, method
dotnet_diagnostic.DSA010.excluded_type_name_patterns =
dotnet_diagnostic.DSA010.excluded_namespace_patterns =
dotnet_diagnostic.DSA010.excluded_base_types =
dotnet_diagnostic.DSA010.excluded_member_name_patterns =
dotnet_diagnostic.DSA010.excluded_field_name_patterns =
dotnet_diagnostic.DSA010.excluded_lock_name_patterns =
dotnet_diagnostic.DSA010.excluded_field_types =
dotnet_diagnostic.DSA010.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs

# DSA011: Avoid lazily initialized, self-contained, static singleton properties
dotnet_diagnostic.DSA011.severity = warning

# DSA012: Avoid the "if not exists, then insert" check-then-act antipattern on database types (TOCTOU)
dotnet_diagnostic.DSA012.severity = warning

# DSA013: Minimal API endpoints should have an explicit authorization configuration
dotnet_diagnostic.DSA013.severity = warning

# DSA014: Minimal API endpoints on route groups should have an explicit authorization configuration
dotnet_diagnostic.DSA014.severity = warning

# DSA015: Minimal API endpoints on parameterized route builders should have an explicit authorization configuration
dotnet_diagnostic.DSA015.severity = warning

# DSA016: Avoid repeated invocation of the same enumeration method with identical arguments
dotnet_diagnostic.DSA016.severity = warning

# DSA017: Use the collection's atomic operation instead of the check-then-act pattern
dotnet_diagnostic.DSA017.severity = warning

# DSA018: Protect the check-then-act pattern with a lock or use a collection with built-in duplicate handling
dotnet_diagnostic.DSA018.severity = warning
dotnet_diagnostic.DSA018.excluded_members = JsonSerializerOptions.Converters, JsonSerializerSettings.Converters

# DSA019: Avoid repeated deeply nested member access chains
dotnet_diagnostic.DSA019.severity = warning
dotnet_diagnostic.DSA019.max_repeated_dereferenciation_depth = 3
dotnet_diagnostic.DSA019.excluded_prefixes = Is, Has, Does, Contains, Throws, Assert.That
dotnet_diagnostic.DSA019.ignored_intermediate_members = TagWithCallSite, TagWith, AsNoTracking, AsNoTrackingWithIdentityResolution, AsTracking, AsSplitQuery, AsSingleQuery, IgnoreAutoIncludes, IgnoreQueryFilters, ConfigureAwait

# DSA020: Remove redundant async/await on Task.FromResult
dotnet_diagnostic.DSA020.severity = warning

# DSA021: Entity Framework queries should be tagged with TagWith or TagWithCallSite for traceability
dotnet_diagnostic.DSA021.severity = warning

# DSA022: Hoist loop-invariant expression out of inner loop
dotnet_diagnostic.DSA022.severity = warning

# DSA023: Use Path.Combine instead of string concatenation to build file system paths
dotnet_diagnostic.DSA023.severity = warning

# DSA024: Use Path.Combine instead of string concatenation for path-like parameters
dotnet_diagnostic.DSA024.severity = warning
dotnet_diagnostic.DSA024.exact_parameter_names = path
dotnet_diagnostic.DSA024.prefix_suffix_parameter_names = filePath, fileName, directoryPath, directoryName, folderPath, folderName, fileFullPath, directoryFullPath, fileFullName, directoryFullName, xmlFile, xmlFilePath, xmlFileName, jsonFile, jsonFileName, jsonFilePath

# DSA025: Use structured logging template instead of interpolated string
dotnet_diagnostic.DSA025.severity = warning

# DSA026: Use nearest scope CancellationToken
dotnet_diagnostic.DSA026.severity = warning

# DSA027: Replace string concatenation in loops with StringBuilder
dotnet_diagnostic.DSA027.severity = warning

# DSA028: Prefer ToArray() over ToList() when return type is a read-only interface
dotnet_diagnostic.DSA028.severity = warning

# DSA029: The RequiredAttribute has no impact on a not-nullable value type
dotnet_diagnostic.DSA029.severity = warning

# DSA030: Entity Framework queries should explicitly specify a change tracking strategy
dotnet_diagnostic.DSA030.severity = suggestion

# DSA031: Use AsNoTracking for Entity Framework queries that do not require change tracking
dotnet_diagnostic.DSA031.severity = warning

# DSA032: Avoid duplicated string literals in the same method body
dotnet_diagnostic.DSA032.severity = suggestion
dotnet_diagnostic.DSA032.max_duplications = 2
dotnet_diagnostic.DSA032.min_string_length = 5

# DSA033: File exceeds maximum line count
dotnet_diagnostic.DSA033.severity = warning
dotnet_diagnostic.DSA033.max_lines = 500
dotnet_diagnostic.DSA033.count_blank_lines = false
dotnet_diagnostic.DSA033.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs
dotnet_diagnostic.DSA033.excluded_base_types = Microsoft.EntityFrameworkCore.Migrations.Migration, Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot, System.Data.Entity.Migrations.DbMigration, Microsoft.EntityFrameworkCore.DbContext

# DSA034: Single-type file exceeds maximum line count
dotnet_diagnostic.DSA034.severity = warning
dotnet_diagnostic.DSA034.max_lines = 500
dotnet_diagnostic.DSA034.count_blank_lines = false
dotnet_diagnostic.DSA034.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs
dotnet_diagnostic.DSA034.excluded_base_types = Microsoft.EntityFrameworkCore.Migrations.Migration, Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot, System.Data.Entity.Migrations.DbMigration, Microsoft.EntityFrameworkCore.DbContext
dotnet_diagnostic.DSA034.max_topics = 5
dotnet_diagnostic.DSA034.excluded_topic_words = The, Get, Set, Save, Load, Print, Async, Sync, Open, Close, File, Add, Remove, Delete, Create, Update, Find, Check, Is, Has, Can, Try, On, Handle, Process, Execute, Run, Do, Make, To, From, With, For, By, In, Of, And, Or, Not, Begin, End, Start, Stop, Init, Initialize, Read, Write, Send, Receive, Input, Output, Invoke, Call, Raise, Fire, Trigger, Emit, Parse, Format, Convert, Transform, Map, Validate, Verify, Ensure, Assert, Log, Trace, Debug, Info, Warn, Error, Enable, Disable, Register, Unregister, Subscribe, Unsubscribe, Attach, Detach, Lock, Unlock, Acquire, Release, Enter, Exit, Return, Yield, Test, Mock, Stub, Fake, Spy, Before, After, Pre, Post, Internal, Default, Current, New, Old, First, Last, Next, Previous, All, Any, Each, Count, Index, Item, Value, Key, Name, Type, Data, Result, Status, State, Context, Null, Empty, True, False

# DSA035: Hoist loop-invariant reflection call out of loop
dotnet_diagnostic.DSA035.severity = warning

# DSA036: Extract Regex with constant pattern to a static readonly field
dotnet_diagnostic.DSA036.severity = warning

# DSA037: A [ThreadStatic] Random field must not have a field initializer (only the first thread is initialized)
dotnet_diagnostic.DSA037.severity = warning

# DSA038: Avoid seeding Random with a time-based or constant value
dotnet_diagnostic.DSA038.severity = warning

# DSA039: Avoid modulo bias when reducing cryptographic random bytes into a range (use RandomNumberGenerator.GetInt32)
dotnet_diagnostic.DSA039.severity = warning

# DSA040: Do not use System.Random for security-sensitive values (token/salt/nonce/key); use RandomNumberGenerator
dotnet_diagnostic.DSA040.severity = warning

# DSA041: Avoid switching over an enum with many members (max_enum_members, default 4); prefer the Strategy pattern
dotnet_diagnostic.DSA041.severity = warning
dotnet_diagnostic.DSA041.max_enum_members = 4

# DSA042: Avoid repeating a segment in a namespace (e.g. My.Project.Models.Models)
dotnet_diagnostic.DSA042.severity = warning

# DSA043: A complex class (cyclomatic complexity, lines, ... configurable) should inject or use a logger (Microsoft.Extensions.Logging.ILogger)
dotnet_diagnostic.DSA043.severity = suggestion
dotnet_diagnostic.DSA043.min_cyclomatic_complexity = 30
dotnet_diagnostic.DSA043.min_method_complexity = 15
dotnet_diagnostic.DSA043.min_lines = 200
dotnet_diagnostic.DSA043.min_methods = 0
dotnet_diagnostic.DSA043.min_constructor_parameters = 0
dotnet_diagnostic.DSA043.require_all_thresholds = false
dotnet_diagnostic.DSA043.count_blank_lines = false
dotnet_diagnostic.DSA043.require_activity = true
dotnet_diagnostic.DSA043.activity_kinds = catch, await, call
dotnet_diagnostic.DSA043.activity_namespaces = System.Net.Http, System.Net.Sockets, System.Net.Mail, System.IO, System.Data, Microsoft.Data, Microsoft.EntityFrameworkCore, Dapper, Npgsql, MySql, Oracle, MongoDB, StackExchange.Redis, Azure, Amazon, RabbitMQ, Confluent.Kafka, MassTransit
dotnet_diagnostic.DSA043.logger_namespaces = Microsoft.Extensions.Logging
dotnet_diagnostic.DSA043.logger_types =
dotnet_diagnostic.DSA043.accept_inherited_logger = true
dotnet_diagnostic.DSA043.accept_logger_parameters = true
dotnet_diagnostic.DSA043.included_type_kinds = class
dotnet_diagnostic.DSA043.include_static_classes = false
dotnet_diagnostic.DSA043.include_abstract_classes = false
dotnet_diagnostic.DSA043.include_nested_types = false
dotnet_diagnostic.DSA043.excluded_base_types = System.Exception, System.Attribute
dotnet_diagnostic.DSA043.excluded_type_name_patterns = *Dto, *Request, *Response, *Options, *Settings, *Config, *Configuration, *Constants, *Extensions, *Mapper, *Mapping, *Profile, *Validator, *Specification, *Comparer, *Converter
dotnet_diagnostic.DSA043.excluded_namespace_patterns = *.Migrations
dotnet_diagnostic.DSA043.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs

# DSA044: Use the available logger (Microsoft.Extensions.Logging, Serilog, NLog, log4net, ...) instead of Console.Write* or Debug.Write*
dotnet_diagnostic.DSA044.severity = suggestion
dotnet_diagnostic.DSA044.flagged_types = System.Console, System.Diagnostics.Debug
dotnet_diagnostic.DSA044.flagged_members = Write*
dotnet_diagnostic.DSA044.include_console_streams = true
dotnet_diagnostic.DSA044.logger_namespaces =
dotnet_diagnostic.DSA044.logger_types = Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.ILoggerFactory, Serilog.ILogger, NLog.ILogger, log4net.ILog, Castle.Core.Logging.ILogger, Common.Logging.ILog, NServiceBus.Logging.ILog, Akka.Event.ILoggingAdapter, System.Diagnostics.TraceSource
dotnet_diagnostic.DSA044.accept_inherited_logger = true
dotnet_diagnostic.DSA044.accept_logger_parameters = true
dotnet_diagnostic.DSA044.require_accessible_logger = true
dotnet_diagnostic.DSA044.accept_static_logger_calls = true
dotnet_diagnostic.DSA044.static_logger_types = Serilog.Log
dotnet_diagnostic.DSA044.skip_interactive_members = true
dotnet_diagnostic.DSA044.excluded_type_name_patterns = Program
dotnet_diagnostic.DSA044.excluded_member_name_patterns = Main
dotnet_diagnostic.DSA044.excluded_namespace_patterns =
dotnet_diagnostic.DSA044.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs

# DSA045: A pure data carrier (DTO: properties only, no logic, no explicit constructor, base types included) should be excluded from the code coverage with [ExcludeFromCodeCoverage]
dotnet_diagnostic.DSA045.severity = warning
dotnet_diagnostic.DSA045.included_type_kinds = class, struct, record, record_struct
dotnet_diagnostic.DSA045.include_abstract_classes = true
dotnet_diagnostic.DSA045.include_nested_types = true
dotnet_diagnostic.DSA045.min_data_members = 1
dotnet_diagnostic.DSA045.allow_fields = true
dotnet_diagnostic.DSA045.allow_property_initializers = true
dotnet_diagnostic.DSA045.allow_backing_field_properties = true
dotnet_diagnostic.DSA045.allow_computed_properties = false
dotnet_diagnostic.DSA045.allow_parameterless_constructor = false
dotnet_diagnostic.DSA045.allow_nested_types = false
dotnet_diagnostic.DSA045.inspect_base_types = true
dotnet_diagnostic.DSA045.exclusion_attributes = System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute
dotnet_diagnostic.DSA045.ignored_attributes = System.CodeDom.Compiler.GeneratedCodeAttribute, System.Runtime.CompilerServices.CompilerGeneratedAttribute
dotnet_diagnostic.DSA045.skip_test_assemblies = true
dotnet_diagnostic.DSA045.test_assembly_names = xunit.core, xunit.v3.core, nunit.framework, Microsoft.VisualStudio.TestPlatform.TestFramework, MSTest.TestFramework, TUnit.Core
dotnet_diagnostic.DSA045.excluded_type_name_patterns =
dotnet_diagnostic.DSA045.excluded_namespace_patterns =
dotnet_diagnostic.DSA045.excluded_base_types =
dotnet_diagnostic.DSA045.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs
dotnet_diagnostic.DSA045.code_fix_justification =
```

## Good / Professional

For modern projects, with good quality assurance and decent cybersecurity constraints and robustness guarantees.

This is the profile that the authors of the rules recommend by default: every rule keeps the severity it was designed with. The defects that are almost always real bugs or security flaws (for example `DSA005`, `DSA006`, `DSA008`, `DSA009`) fail the build, the design, code-smell and performance findings are warnings that a reviewer is expected to look at, and the stylistic or informative ones are suggestions.

```ini
# DSA001: WebApi controller methods should not contain data-manipulation business logics through a **LINQ query expression**.
dotnet_diagnostic.DSA001.severity = warning

# DSA002: WebApi controller methods should not contain data-manipulation business logics through a **LINQ fluent query**.
dotnet_diagnostic.DSA002.severity = warning

# DSA003: Use String.IsNullOrWhiteSpace instead of String.IsNullOrEmpty
dotnet_diagnostic.DSA003.severity = warning

# DSA004: Use DateTime.UtcNow instead of DateTime.Now
dotnet_diagnostic.DSA004.severity = warning

# DSA005: Potential non-deterministic point-in-time execution
dotnet_diagnostic.DSA005.severity = error

# DSA006: General exceptions should not be thrown by user code
dotnet_diagnostic.DSA006.severity = error

# DSA007: When initializing a lazy field, use a robust locking pattern, i.e. the "if-lock-if" (aka "double checked locking")
dotnet_diagnostic.DSA007.severity = warning

# DSA008: The RequiredAttribute has no impact on a not-nullable DateTime
dotnet_diagnostic.DSA008.severity = error

# DSA009: The RequiredAttribute has no impact on a not-nullable DateTimeOffset
dotnet_diagnostic.DSA009.severity = error

# DSA010: A lock around a single field access does not protect your logic
dotnet_diagnostic.DSA010.severity = warning
dotnet_diagnostic.DSA010.report_reads = true
dotnet_diagnostic.DSA010.report_writes = true
dotnet_diagnostic.DSA010.report_shared_locks = true
dotnet_diagnostic.DSA010.report_lock_on_this = false
dotnet_diagnostic.DSA010.report_tearable_types = true
dotnet_diagnostic.DSA010.report_volatile_fields = true
dotnet_diagnostic.DSA010.report_readonly_fields = true
dotnet_diagnostic.DSA010.skip_signaled_locks = true
dotnet_diagnostic.DSA010.body_shape = sole_statement
dotnet_diagnostic.DSA010.member_kinds = accessor, method
dotnet_diagnostic.DSA010.excluded_type_name_patterns =
dotnet_diagnostic.DSA010.excluded_namespace_patterns =
dotnet_diagnostic.DSA010.excluded_base_types =
dotnet_diagnostic.DSA010.excluded_member_name_patterns =
dotnet_diagnostic.DSA010.excluded_field_name_patterns =
dotnet_diagnostic.DSA010.excluded_lock_name_patterns =
dotnet_diagnostic.DSA010.excluded_field_types =
dotnet_diagnostic.DSA010.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs

# DSA011: Avoid lazily initialized, self-contained, static singleton properties
dotnet_diagnostic.DSA011.severity = warning

# DSA012: Avoid the "if not exists, then insert" check-then-act antipattern on database types (TOCTOU)
dotnet_diagnostic.DSA012.severity = warning

# DSA013: Minimal API endpoints should have an explicit authorization configuration
dotnet_diagnostic.DSA013.severity = warning

# DSA014: Minimal API endpoints on route groups should have an explicit authorization configuration
dotnet_diagnostic.DSA014.severity = warning

# DSA015: Minimal API endpoints on parameterized route builders should have an explicit authorization configuration
dotnet_diagnostic.DSA015.severity = warning

# DSA016: Avoid repeated invocation of the same enumeration method with identical arguments
dotnet_diagnostic.DSA016.severity = warning

# DSA017: Use the collection's atomic operation instead of the check-then-act pattern
dotnet_diagnostic.DSA017.severity = warning

# DSA018: Protect the check-then-act pattern with a lock or use a collection with built-in duplicate handling
dotnet_diagnostic.DSA018.severity = warning
dotnet_diagnostic.DSA018.excluded_members = JsonSerializerOptions.Converters, JsonSerializerSettings.Converters

# DSA019: Avoid repeated deeply nested member access chains
dotnet_diagnostic.DSA019.severity = warning
dotnet_diagnostic.DSA019.max_repeated_dereferenciation_depth = 3
dotnet_diagnostic.DSA019.excluded_prefixes = Is, Has, Does, Contains, Throws, Assert.That
dotnet_diagnostic.DSA019.ignored_intermediate_members = TagWithCallSite, TagWith, AsNoTracking, AsNoTrackingWithIdentityResolution, AsTracking, AsSplitQuery, AsSingleQuery, IgnoreAutoIncludes, IgnoreQueryFilters, ConfigureAwait

# DSA020: Remove redundant async/await on Task.FromResult
dotnet_diagnostic.DSA020.severity = warning

# DSA021: Entity Framework queries should be tagged with TagWith or TagWithCallSite for traceability
dotnet_diagnostic.DSA021.severity = warning

# DSA022: Hoist loop-invariant expression out of inner loop
dotnet_diagnostic.DSA022.severity = warning

# DSA023: Use Path.Combine instead of string concatenation to build file system paths
dotnet_diagnostic.DSA023.severity = warning

# DSA024: Use Path.Combine instead of string concatenation for path-like parameters
dotnet_diagnostic.DSA024.severity = warning
dotnet_diagnostic.DSA024.exact_parameter_names = path
dotnet_diagnostic.DSA024.prefix_suffix_parameter_names = filePath, fileName, directoryPath, directoryName, folderPath, folderName, fileFullPath, directoryFullPath, fileFullName, directoryFullName, xmlFile, xmlFilePath, xmlFileName, jsonFile, jsonFileName, jsonFilePath

# DSA025: Use structured logging template instead of interpolated string
dotnet_diagnostic.DSA025.severity = warning

# DSA026: Use nearest scope CancellationToken
dotnet_diagnostic.DSA026.severity = warning

# DSA027: Replace string concatenation in loops with StringBuilder
dotnet_diagnostic.DSA027.severity = warning

# DSA028: Prefer ToArray() over ToList() when return type is a read-only interface
dotnet_diagnostic.DSA028.severity = warning

# DSA029: The RequiredAttribute has no impact on a not-nullable value type
dotnet_diagnostic.DSA029.severity = warning

# DSA030: Entity Framework queries should explicitly specify a change tracking strategy
dotnet_diagnostic.DSA030.severity = suggestion

# DSA031: Use AsNoTracking for Entity Framework queries that do not require change tracking
dotnet_diagnostic.DSA031.severity = warning

# DSA032: Avoid duplicated string literals in the same method body
dotnet_diagnostic.DSA032.severity = suggestion
dotnet_diagnostic.DSA032.max_duplications = 2
dotnet_diagnostic.DSA032.min_string_length = 5

# DSA033: File exceeds maximum line count
dotnet_diagnostic.DSA033.severity = warning
dotnet_diagnostic.DSA033.max_lines = 500
dotnet_diagnostic.DSA033.count_blank_lines = false
dotnet_diagnostic.DSA033.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs
dotnet_diagnostic.DSA033.excluded_base_types = Microsoft.EntityFrameworkCore.Migrations.Migration, Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot, System.Data.Entity.Migrations.DbMigration, Microsoft.EntityFrameworkCore.DbContext

# DSA034: Single-type file exceeds maximum line count
dotnet_diagnostic.DSA034.severity = warning
dotnet_diagnostic.DSA034.max_lines = 500
dotnet_diagnostic.DSA034.count_blank_lines = false
dotnet_diagnostic.DSA034.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs
dotnet_diagnostic.DSA034.excluded_base_types = Microsoft.EntityFrameworkCore.Migrations.Migration, Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot, System.Data.Entity.Migrations.DbMigration, Microsoft.EntityFrameworkCore.DbContext
dotnet_diagnostic.DSA034.max_topics = 5
dotnet_diagnostic.DSA034.excluded_topic_words = The, Get, Set, Save, Load, Print, Async, Sync, Open, Close, File, Add, Remove, Delete, Create, Update, Find, Check, Is, Has, Can, Try, On, Handle, Process, Execute, Run, Do, Make, To, From, With, For, By, In, Of, And, Or, Not, Begin, End, Start, Stop, Init, Initialize, Read, Write, Send, Receive, Input, Output, Invoke, Call, Raise, Fire, Trigger, Emit, Parse, Format, Convert, Transform, Map, Validate, Verify, Ensure, Assert, Log, Trace, Debug, Info, Warn, Error, Enable, Disable, Register, Unregister, Subscribe, Unsubscribe, Attach, Detach, Lock, Unlock, Acquire, Release, Enter, Exit, Return, Yield, Test, Mock, Stub, Fake, Spy, Before, After, Pre, Post, Internal, Default, Current, New, Old, First, Last, Next, Previous, All, Any, Each, Count, Index, Item, Value, Key, Name, Type, Data, Result, Status, State, Context, Null, Empty, True, False

# DSA035: Hoist loop-invariant reflection call out of loop
dotnet_diagnostic.DSA035.severity = warning

# DSA036: Extract Regex with constant pattern to a static readonly field
dotnet_diagnostic.DSA036.severity = warning

# DSA037: A [ThreadStatic] Random field must not have a field initializer (only the first thread is initialized)
dotnet_diagnostic.DSA037.severity = warning

# DSA038: Avoid seeding Random with a time-based or constant value
dotnet_diagnostic.DSA038.severity = warning

# DSA039: Avoid modulo bias when reducing cryptographic random bytes into a range (use RandomNumberGenerator.GetInt32)
dotnet_diagnostic.DSA039.severity = warning

# DSA040: Do not use System.Random for security-sensitive values (token/salt/nonce/key); use RandomNumberGenerator
dotnet_diagnostic.DSA040.severity = warning

# DSA041: Avoid switching over an enum with many members (max_enum_members, default 4); prefer the Strategy pattern
dotnet_diagnostic.DSA041.severity = warning
dotnet_diagnostic.DSA041.max_enum_members = 4

# DSA042: Avoid repeating a segment in a namespace (e.g. My.Project.Models.Models)
dotnet_diagnostic.DSA042.severity = warning

# DSA043: A complex class (cyclomatic complexity, lines, ... configurable) should inject or use a logger (Microsoft.Extensions.Logging.ILogger)
dotnet_diagnostic.DSA043.severity = suggestion
dotnet_diagnostic.DSA043.min_cyclomatic_complexity = 30
dotnet_diagnostic.DSA043.min_method_complexity = 15
dotnet_diagnostic.DSA043.min_lines = 200
dotnet_diagnostic.DSA043.min_methods = 0
dotnet_diagnostic.DSA043.min_constructor_parameters = 0
dotnet_diagnostic.DSA043.require_all_thresholds = false
dotnet_diagnostic.DSA043.count_blank_lines = false
dotnet_diagnostic.DSA043.require_activity = true
dotnet_diagnostic.DSA043.activity_kinds = catch, await, call
dotnet_diagnostic.DSA043.activity_namespaces = System.Net.Http, System.Net.Sockets, System.Net.Mail, System.IO, System.Data, Microsoft.Data, Microsoft.EntityFrameworkCore, Dapper, Npgsql, MySql, Oracle, MongoDB, StackExchange.Redis, Azure, Amazon, RabbitMQ, Confluent.Kafka, MassTransit
dotnet_diagnostic.DSA043.logger_namespaces = Microsoft.Extensions.Logging
dotnet_diagnostic.DSA043.logger_types =
dotnet_diagnostic.DSA043.accept_inherited_logger = true
dotnet_diagnostic.DSA043.accept_logger_parameters = true
dotnet_diagnostic.DSA043.included_type_kinds = class
dotnet_diagnostic.DSA043.include_static_classes = false
dotnet_diagnostic.DSA043.include_abstract_classes = false
dotnet_diagnostic.DSA043.include_nested_types = false
dotnet_diagnostic.DSA043.excluded_base_types = System.Exception, System.Attribute
dotnet_diagnostic.DSA043.excluded_type_name_patterns = *Dto, *Request, *Response, *Options, *Settings, *Config, *Configuration, *Constants, *Extensions, *Mapper, *Mapping, *Profile, *Validator, *Specification, *Comparer, *Converter
dotnet_diagnostic.DSA043.excluded_namespace_patterns = *.Migrations
dotnet_diagnostic.DSA043.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs

# DSA044: Use the available logger (Microsoft.Extensions.Logging, Serilog, NLog, log4net, ...) instead of Console.Write* or Debug.Write*
dotnet_diagnostic.DSA044.severity = suggestion
dotnet_diagnostic.DSA044.flagged_types = System.Console, System.Diagnostics.Debug
dotnet_diagnostic.DSA044.flagged_members = Write*
dotnet_diagnostic.DSA044.include_console_streams = true
dotnet_diagnostic.DSA044.logger_namespaces =
dotnet_diagnostic.DSA044.logger_types = Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.ILoggerFactory, Serilog.ILogger, NLog.ILogger, log4net.ILog, Castle.Core.Logging.ILogger, Common.Logging.ILog, NServiceBus.Logging.ILog, Akka.Event.ILoggingAdapter, System.Diagnostics.TraceSource
dotnet_diagnostic.DSA044.accept_inherited_logger = true
dotnet_diagnostic.DSA044.accept_logger_parameters = true
dotnet_diagnostic.DSA044.require_accessible_logger = true
dotnet_diagnostic.DSA044.accept_static_logger_calls = true
dotnet_diagnostic.DSA044.static_logger_types = Serilog.Log
dotnet_diagnostic.DSA044.skip_interactive_members = true
dotnet_diagnostic.DSA044.excluded_type_name_patterns = Program
dotnet_diagnostic.DSA044.excluded_member_name_patterns = Main
dotnet_diagnostic.DSA044.excluded_namespace_patterns =
dotnet_diagnostic.DSA044.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs

# DSA045: A pure data carrier (DTO: properties only, no logic, no explicit constructor, base types included) should be excluded from the code coverage with [ExcludeFromCodeCoverage]
dotnet_diagnostic.DSA045.severity = warning
dotnet_diagnostic.DSA045.included_type_kinds = class, struct, record, record_struct
dotnet_diagnostic.DSA045.include_abstract_classes = true
dotnet_diagnostic.DSA045.include_nested_types = true
dotnet_diagnostic.DSA045.min_data_members = 1
dotnet_diagnostic.DSA045.allow_fields = true
dotnet_diagnostic.DSA045.allow_property_initializers = true
dotnet_diagnostic.DSA045.allow_backing_field_properties = true
dotnet_diagnostic.DSA045.allow_computed_properties = false
dotnet_diagnostic.DSA045.allow_parameterless_constructor = false
dotnet_diagnostic.DSA045.allow_nested_types = false
dotnet_diagnostic.DSA045.inspect_base_types = true
dotnet_diagnostic.DSA045.exclusion_attributes = System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute
dotnet_diagnostic.DSA045.ignored_attributes = System.CodeDom.Compiler.GeneratedCodeAttribute, System.Runtime.CompilerServices.CompilerGeneratedAttribute
dotnet_diagnostic.DSA045.skip_test_assemblies = true
dotnet_diagnostic.DSA045.test_assembly_names = xunit.core, xunit.v3.core, nunit.framework, Microsoft.VisualStudio.TestPlatform.TestFramework, MSTest.TestFramework, TUnit.Core
dotnet_diagnostic.DSA045.excluded_type_name_patterns =
dotnet_diagnostic.DSA045.excluded_namespace_patterns =
dotnet_diagnostic.DSA045.excluded_base_types =
dotnet_diagnostic.DSA045.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs
dotnet_diagnostic.DSA045.code_fix_justification =
```

## SSDLC (Secure Software Development Lifecycle) and/or AI-Driven development

For modern projects with top level quality assurance, the best cybersecurity constraints and the highest possible robustness guarantees. Particularly recommended for AI/agentic-driven development, where the quality, correctness and robustness of the code **must** be highly deterministic and guaranteed by strict constraints.

Every rule is an `error`: a violation fails the build, so that neither a person in a hurry nor an AI agent can merge code that breaks a rule, and the feedback loop is immediate, objective and machine-readable. The only exceptions are `DSA030`, `DSA033` and `DSA034`, which stay at `warning`: they are advisory by nature (a recommendation that depends on the context, or a size threshold that each project tunes), so they are reported but do not fail the build.

```ini
# DSA001: WebApi controller methods should not contain data-manipulation business logics through a **LINQ query expression**.
dotnet_diagnostic.DSA001.severity = error

# DSA002: WebApi controller methods should not contain data-manipulation business logics through a **LINQ fluent query**.
dotnet_diagnostic.DSA002.severity = error

# DSA003: Use String.IsNullOrWhiteSpace instead of String.IsNullOrEmpty
dotnet_diagnostic.DSA003.severity = error

# DSA004: Use DateTime.UtcNow instead of DateTime.Now
dotnet_diagnostic.DSA004.severity = error

# DSA005: Potential non-deterministic point-in-time execution
dotnet_diagnostic.DSA005.severity = error

# DSA006: General exceptions should not be thrown by user code
dotnet_diagnostic.DSA006.severity = error

# DSA007: When initializing a lazy field, use a robust locking pattern, i.e. the "if-lock-if" (aka "double checked locking")
dotnet_diagnostic.DSA007.severity = error

# DSA008: The RequiredAttribute has no impact on a not-nullable DateTime
dotnet_diagnostic.DSA008.severity = error

# DSA009: The RequiredAttribute has no impact on a not-nullable DateTimeOffset
dotnet_diagnostic.DSA009.severity = error

# DSA010: A lock around a single field access does not protect your logic
dotnet_diagnostic.DSA010.severity = error
dotnet_diagnostic.DSA010.report_reads = true
dotnet_diagnostic.DSA010.report_writes = true
dotnet_diagnostic.DSA010.report_shared_locks = true
dotnet_diagnostic.DSA010.report_lock_on_this = false
dotnet_diagnostic.DSA010.report_tearable_types = true
dotnet_diagnostic.DSA010.report_volatile_fields = true
dotnet_diagnostic.DSA010.report_readonly_fields = true
dotnet_diagnostic.DSA010.skip_signaled_locks = true
dotnet_diagnostic.DSA010.body_shape = sole_statement
dotnet_diagnostic.DSA010.member_kinds = accessor, method
dotnet_diagnostic.DSA010.excluded_type_name_patterns =
dotnet_diagnostic.DSA010.excluded_namespace_patterns =
dotnet_diagnostic.DSA010.excluded_base_types =
dotnet_diagnostic.DSA010.excluded_member_name_patterns =
dotnet_diagnostic.DSA010.excluded_field_name_patterns =
dotnet_diagnostic.DSA010.excluded_lock_name_patterns =
dotnet_diagnostic.DSA010.excluded_field_types =
dotnet_diagnostic.DSA010.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs

# DSA011: Avoid lazily initialized, self-contained, static singleton properties
dotnet_diagnostic.DSA011.severity = error

# DSA012: Avoid the "if not exists, then insert" check-then-act antipattern on database types (TOCTOU)
dotnet_diagnostic.DSA012.severity = error

# DSA013: Minimal API endpoints should have an explicit authorization configuration
dotnet_diagnostic.DSA013.severity = error

# DSA014: Minimal API endpoints on route groups should have an explicit authorization configuration
dotnet_diagnostic.DSA014.severity = error

# DSA015: Minimal API endpoints on parameterized route builders should have an explicit authorization configuration
dotnet_diagnostic.DSA015.severity = error

# DSA016: Avoid repeated invocation of the same enumeration method with identical arguments
dotnet_diagnostic.DSA016.severity = error

# DSA017: Use the collection's atomic operation instead of the check-then-act pattern
dotnet_diagnostic.DSA017.severity = error

# DSA018: Protect the check-then-act pattern with a lock or use a collection with built-in duplicate handling
dotnet_diagnostic.DSA018.severity = error
dotnet_diagnostic.DSA018.excluded_members = JsonSerializerOptions.Converters, JsonSerializerSettings.Converters

# DSA019: Avoid repeated deeply nested member access chains
dotnet_diagnostic.DSA019.severity = error
dotnet_diagnostic.DSA019.max_repeated_dereferenciation_depth = 3
dotnet_diagnostic.DSA019.excluded_prefixes = Is, Has, Does, Contains, Throws, Assert.That
dotnet_diagnostic.DSA019.ignored_intermediate_members = TagWithCallSite, TagWith, AsNoTracking, AsNoTrackingWithIdentityResolution, AsTracking, AsSplitQuery, AsSingleQuery, IgnoreAutoIncludes, IgnoreQueryFilters, ConfigureAwait

# DSA020: Remove redundant async/await on Task.FromResult
dotnet_diagnostic.DSA020.severity = error

# DSA021: Entity Framework queries should be tagged with TagWith or TagWithCallSite for traceability
dotnet_diagnostic.DSA021.severity = error

# DSA022: Hoist loop-invariant expression out of inner loop
dotnet_diagnostic.DSA022.severity = error

# DSA023: Use Path.Combine instead of string concatenation to build file system paths
dotnet_diagnostic.DSA023.severity = error

# DSA024: Use Path.Combine instead of string concatenation for path-like parameters
dotnet_diagnostic.DSA024.severity = error
dotnet_diagnostic.DSA024.exact_parameter_names = path
dotnet_diagnostic.DSA024.prefix_suffix_parameter_names = filePath, fileName, directoryPath, directoryName, folderPath, folderName, fileFullPath, directoryFullPath, fileFullName, directoryFullName, xmlFile, xmlFilePath, xmlFileName, jsonFile, jsonFileName, jsonFilePath

# DSA025: Use structured logging template instead of interpolated string
dotnet_diagnostic.DSA025.severity = error

# DSA026: Use nearest scope CancellationToken
dotnet_diagnostic.DSA026.severity = error

# DSA027: Replace string concatenation in loops with StringBuilder
dotnet_diagnostic.DSA027.severity = error

# DSA028: Prefer ToArray() over ToList() when return type is a read-only interface
dotnet_diagnostic.DSA028.severity = error

# DSA029: The RequiredAttribute has no impact on a not-nullable value type
dotnet_diagnostic.DSA029.severity = error

# DSA030: Entity Framework queries should explicitly specify a change tracking strategy
dotnet_diagnostic.DSA030.severity = warning

# DSA031: Use AsNoTracking for Entity Framework queries that do not require change tracking
dotnet_diagnostic.DSA031.severity = error

# DSA032: Avoid duplicated string literals in the same method body
dotnet_diagnostic.DSA032.severity = error
dotnet_diagnostic.DSA032.max_duplications = 2
dotnet_diagnostic.DSA032.min_string_length = 5

# DSA033: File exceeds maximum line count
dotnet_diagnostic.DSA033.severity = warning
dotnet_diagnostic.DSA033.max_lines = 500
dotnet_diagnostic.DSA033.count_blank_lines = false
dotnet_diagnostic.DSA033.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs
dotnet_diagnostic.DSA033.excluded_base_types = Microsoft.EntityFrameworkCore.Migrations.Migration, Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot, System.Data.Entity.Migrations.DbMigration, Microsoft.EntityFrameworkCore.DbContext

# DSA034: Single-type file exceeds maximum line count
dotnet_diagnostic.DSA034.severity = warning
dotnet_diagnostic.DSA034.max_lines = 500
dotnet_diagnostic.DSA034.count_blank_lines = false
dotnet_diagnostic.DSA034.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs
dotnet_diagnostic.DSA034.excluded_base_types = Microsoft.EntityFrameworkCore.Migrations.Migration, Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot, System.Data.Entity.Migrations.DbMigration, Microsoft.EntityFrameworkCore.DbContext
dotnet_diagnostic.DSA034.max_topics = 5
dotnet_diagnostic.DSA034.excluded_topic_words = The, Get, Set, Save, Load, Print, Async, Sync, Open, Close, File, Add, Remove, Delete, Create, Update, Find, Check, Is, Has, Can, Try, On, Handle, Process, Execute, Run, Do, Make, To, From, With, For, By, In, Of, And, Or, Not, Begin, End, Start, Stop, Init, Initialize, Read, Write, Send, Receive, Input, Output, Invoke, Call, Raise, Fire, Trigger, Emit, Parse, Format, Convert, Transform, Map, Validate, Verify, Ensure, Assert, Log, Trace, Debug, Info, Warn, Error, Enable, Disable, Register, Unregister, Subscribe, Unsubscribe, Attach, Detach, Lock, Unlock, Acquire, Release, Enter, Exit, Return, Yield, Test, Mock, Stub, Fake, Spy, Before, After, Pre, Post, Internal, Default, Current, New, Old, First, Last, Next, Previous, All, Any, Each, Count, Index, Item, Value, Key, Name, Type, Data, Result, Status, State, Context, Null, Empty, True, False

# DSA035: Hoist loop-invariant reflection call out of loop
dotnet_diagnostic.DSA035.severity = error

# DSA036: Extract Regex with constant pattern to a static readonly field
dotnet_diagnostic.DSA036.severity = error

# DSA037: A [ThreadStatic] Random field must not have a field initializer (only the first thread is initialized)
dotnet_diagnostic.DSA037.severity = error

# DSA038: Avoid seeding Random with a time-based or constant value
dotnet_diagnostic.DSA038.severity = error

# DSA039: Avoid modulo bias when reducing cryptographic random bytes into a range (use RandomNumberGenerator.GetInt32)
dotnet_diagnostic.DSA039.severity = error

# DSA040: Do not use System.Random for security-sensitive values (token/salt/nonce/key); use RandomNumberGenerator
dotnet_diagnostic.DSA040.severity = error

# DSA041: Avoid switching over an enum with many members (max_enum_members, default 4); prefer the Strategy pattern
dotnet_diagnostic.DSA041.severity = error
dotnet_diagnostic.DSA041.max_enum_members = 4

# DSA042: Avoid repeating a segment in a namespace (e.g. My.Project.Models.Models)
dotnet_diagnostic.DSA042.severity = error

# DSA043: A complex class (cyclomatic complexity, lines, ... configurable) should inject or use a logger (Microsoft.Extensions.Logging.ILogger)
dotnet_diagnostic.DSA043.severity = error
dotnet_diagnostic.DSA043.min_cyclomatic_complexity = 30
dotnet_diagnostic.DSA043.min_method_complexity = 15
dotnet_diagnostic.DSA043.min_lines = 200
dotnet_diagnostic.DSA043.min_methods = 0
dotnet_diagnostic.DSA043.min_constructor_parameters = 0
dotnet_diagnostic.DSA043.require_all_thresholds = false
dotnet_diagnostic.DSA043.count_blank_lines = false
dotnet_diagnostic.DSA043.require_activity = true
dotnet_diagnostic.DSA043.activity_kinds = catch, await, call
dotnet_diagnostic.DSA043.activity_namespaces = System.Net.Http, System.Net.Sockets, System.Net.Mail, System.IO, System.Data, Microsoft.Data, Microsoft.EntityFrameworkCore, Dapper, Npgsql, MySql, Oracle, MongoDB, StackExchange.Redis, Azure, Amazon, RabbitMQ, Confluent.Kafka, MassTransit
dotnet_diagnostic.DSA043.logger_namespaces = Microsoft.Extensions.Logging
dotnet_diagnostic.DSA043.logger_types =
dotnet_diagnostic.DSA043.accept_inherited_logger = true
dotnet_diagnostic.DSA043.accept_logger_parameters = true
dotnet_diagnostic.DSA043.included_type_kinds = class
dotnet_diagnostic.DSA043.include_static_classes = false
dotnet_diagnostic.DSA043.include_abstract_classes = false
dotnet_diagnostic.DSA043.include_nested_types = false
dotnet_diagnostic.DSA043.excluded_base_types = System.Exception, System.Attribute
dotnet_diagnostic.DSA043.excluded_type_name_patterns = *Dto, *Request, *Response, *Options, *Settings, *Config, *Configuration, *Constants, *Extensions, *Mapper, *Mapping, *Profile, *Validator, *Specification, *Comparer, *Converter
dotnet_diagnostic.DSA043.excluded_namespace_patterns = *.Migrations
dotnet_diagnostic.DSA043.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs

# DSA044: Use the available logger (Microsoft.Extensions.Logging, Serilog, NLog, log4net, ...) instead of Console.Write* or Debug.Write*
dotnet_diagnostic.DSA044.severity = error
dotnet_diagnostic.DSA044.flagged_types = System.Console, System.Diagnostics.Debug
dotnet_diagnostic.DSA044.flagged_members = Write*
dotnet_diagnostic.DSA044.include_console_streams = true
dotnet_diagnostic.DSA044.logger_namespaces =
dotnet_diagnostic.DSA044.logger_types = Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.ILoggerFactory, Serilog.ILogger, NLog.ILogger, log4net.ILog, Castle.Core.Logging.ILogger, Common.Logging.ILog, NServiceBus.Logging.ILog, Akka.Event.ILoggingAdapter, System.Diagnostics.TraceSource
dotnet_diagnostic.DSA044.accept_inherited_logger = true
dotnet_diagnostic.DSA044.accept_logger_parameters = true
dotnet_diagnostic.DSA044.require_accessible_logger = true
dotnet_diagnostic.DSA044.accept_static_logger_calls = true
dotnet_diagnostic.DSA044.static_logger_types = Serilog.Log
dotnet_diagnostic.DSA044.skip_interactive_members = true
dotnet_diagnostic.DSA044.excluded_type_name_patterns = Program
dotnet_diagnostic.DSA044.excluded_member_name_patterns = Main
dotnet_diagnostic.DSA044.excluded_namespace_patterns =
dotnet_diagnostic.DSA044.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs

# DSA045: A pure data carrier (DTO: properties only, no logic, no explicit constructor, base types included) should be excluded from the code coverage with [ExcludeFromCodeCoverage]
dotnet_diagnostic.DSA045.severity = error
dotnet_diagnostic.DSA045.included_type_kinds = class, struct, record, record_struct
dotnet_diagnostic.DSA045.include_abstract_classes = true
dotnet_diagnostic.DSA045.include_nested_types = true
dotnet_diagnostic.DSA045.min_data_members = 1
dotnet_diagnostic.DSA045.allow_fields = true
dotnet_diagnostic.DSA045.allow_property_initializers = true
dotnet_diagnostic.DSA045.allow_backing_field_properties = true
dotnet_diagnostic.DSA045.allow_computed_properties = false
dotnet_diagnostic.DSA045.allow_parameterless_constructor = false
dotnet_diagnostic.DSA045.allow_nested_types = false
dotnet_diagnostic.DSA045.inspect_base_types = true
dotnet_diagnostic.DSA045.exclusion_attributes = System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute
dotnet_diagnostic.DSA045.ignored_attributes = System.CodeDom.Compiler.GeneratedCodeAttribute, System.Runtime.CompilerServices.CompilerGeneratedAttribute
dotnet_diagnostic.DSA045.skip_test_assemblies = true
dotnet_diagnostic.DSA045.test_assembly_names = xunit.core, xunit.v3.core, nunit.framework, Microsoft.VisualStudio.TestPlatform.TestFramework, MSTest.TestFramework, TUnit.Core
dotnet_diagnostic.DSA045.excluded_type_name_patterns =
dotnet_diagnostic.DSA045.excluded_namespace_patterns =
dotnet_diagnostic.DSA045.excluded_base_types =
dotnet_diagnostic.DSA045.excluded_file_patterns = *.Designer.cs, *.generated.cs, *.g.cs, *.g.i.cs
dotnet_diagnostic.DSA045.code_fix_justification =
```

---

# Contributing

Contributions are welcome! Please read the [Contributing Guidelines](CONTRIBUTING.md) before submitting a pull request.

# Security

To report a security vulnerability, please follow the instructions in [SECURITY.md](SECURITY.md). Do not open a public issue for security reports.

# Code of Conduct

This project follows the [Contributor Covenant v2.1](https://www.contributor-covenant.org/version/2/1/code_of_conduct/). See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) for details.

# License

This project is licensed under the [MIT License](LICENSE).