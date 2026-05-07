# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in this project, please report it responsibly.

**Do not open a public issue.** Instead, send an email to [info@dogmasolutions.com](mailto:info@dogmasolutions.com) with:

- A description of the vulnerability.
- Steps to reproduce or a proof of concept.
- The affected analyzer rule(s), if applicable.

We will acknowledge your report within 5 business days and work with you to understand and address the issue before any public disclosure.

## Scope

This policy covers the Roslyn analyzer source code in this repository and the published [NuGet package](https://www.nuget.org/packages/DogmaSolutions.Analyzers/). Since analyzers run at compile time inside the .NET compiler pipeline, any vulnerability that could lead to arbitrary code execution or information disclosure during compilation is considered high severity.
