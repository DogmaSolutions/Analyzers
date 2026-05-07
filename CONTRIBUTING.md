# Contributing to Dogma Solutions Roslyn Analyzers

Thank you for your interest in contributing! This document explains how to get involved.

## How to Contribute

### Reporting Bugs

If you find a false positive, a missed diagnostic, or any other bug, please [open an issue](https://github.com/DogmaSolutions/Analyzers/issues/new?template=bug_report.yml) with:

- The analyzer rule id (e.g. DSA003).
- A minimal code snippet that reproduces the problem.
- The expected vs. actual behavior.
- Your .NET SDK version (`dotnet --version`).

### Suggesting New Rules or Enhancements

Have an idea for a new analyzer or an improvement to an existing one? [Open a feature request](https://github.com/DogmaSolutions/Analyzers/issues/new?template=feature_request.yml) describing:

- The problem or code smell you want to detect.
- Example code that should (or should not) trigger the diagnostic.
- Which category the rule fits (Design, Security, Performance, Code Smells, Bug, Best Practice).

### Submitting Pull Requests

1. **Fork and clone** the repository.
2. **Create a branch** from `main` with a descriptive name (e.g. `fix/dsa003-false-positive` or `feature/dsa025-new-rule`).
3. **Make your changes** following the guidelines below.
4. **Run the tests** to make sure nothing is broken.
5. **Open a pull request** against `main` with a clear description of the change.

## Development Setup

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (latest stable version)
- An IDE with Roslyn support (Visual Studio, JetBrains Rider, or VS Code with C# Dev Kit)

### Building

```bash
dotnet build DogmaSolutions.Analyzers.sln
```

### Running Tests

```bash
dotnet test DogmaSolutions.Analyzers.sln
```

## Coding Guidelines

- Follow the conventions already present in the codebase; the `.editorconfig` and `DogmaSolutions.ruleset` enforce most style rules automatically.
- Every new analyzer must include:
  - A diagnostic descriptor with a meaningful message and help link.
  - A code fix provider, if a deterministic fix is possible.
  - Unit tests covering both positive (should trigger) and negative (should not trigger) cases.
- Keep commits focused: one logical change per commit.

## Adding a New Analyzer Rule

1. Pick the next available `DSAxxx` id.
2. Create the analyzer class in `DogmaSolutions.Analyzers/`.
3. If applicable, create a code fix provider.
4. Add tests in `DogmaSolutions.Analyzers.Test/`.
5. Document the rule in `README.md` following the existing format (add it to both the rules table and the detailed section).

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to [info@dogmasolutions.com](mailto:info@dogmasolutions.com).

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
