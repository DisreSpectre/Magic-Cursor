---
description: 'Static analysis, StyleCop, EditorConfig, naming conventions, cleanup'
applyTo: '**/*.cs, **/*.editorconfig, **/stylecop.json'
---

# Code Quality

## Current state

No StyleCop, analyzers, or `.editorconfig` yet. Introduce gradually — don't mass-refactor untouched files. Existing exceptions in `MainPage.xaml.cs` (missing `Async` suffix, unused fields, multiple types per file, legacy `this.`) can be fixed opportunistically when touched.

## Rules for new code

- Naming: PascalCase for classes/methods/properties/constants; `I` prefix interfaces; camelCase parameters/locals; `_camelCase` private fields; `Is/Has/Can` booleans; `Async` suffix on async methods.
- File-scoped namespaces. No `this.` qualification.
- One primary type per file (new code).
- File order: usings (System first) → namespace → type → constants → static fields → instance fields → constructors → properties → public methods → private methods → event handlers.
- Remove unused usings, dead code, unused fields, empty methods. Simplify with pattern matching, null-coalescing, modern C# features.

## Analyzers (when introduced)

Follow CA* and IDE* rules on new/modified files. `TreatWarningsAsErrors=false` until existing warnings are addressed. StyleCop `stylecop.json` should use `companyName: "MagicCursor"`, `usingDirectivesPlacement: outsideNamespace`, `systemUsingDirectivesFirst: true`, `newlineAtEndOfFile: require`.

## Validation

- Clean build — zero new warnings in modified files.
- No unused usings or dead code in new files.
- New async methods end with `Async`.

## Must Read & Research

| # | Reference | When |
|---|---|---|
| 1 | [.NET Code Analysis](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview) | Analyzer config |
| 2 | [Code Style Rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/) | IDE* warnings |
| 3 | [Quality Rules (CA*)](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/) | CA* warnings |
| 4 | [StyleCop.Analyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) | Package/config |
| 5 | [StyleCop Rules](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/DOCUMENTATION.md) | SA* violations |
| 6 | [EditorConfig Reference](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-style-rule-options) | `.editorconfig` |
| 7 | [.NET Naming Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names) | Naming |
