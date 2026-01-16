# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TestHarness.Analyzers is a Roslyn analyzer library that detects code patterns blocking "seams" in legacy code, based on Michael Feathers' "Working Effectively with Legacy Code." It identifies 18 anti-patterns (SEAM001-SEAM018) across 5 categories that hinder testability.

**Key Links:**
- Repository: https://github.com/iirorahkonen/TestHarness.Analyzers
- Rule Documentation: `docs/rules/SEAMxxx.md`
- NuGet Package: TestHarness.Analyzers

## Build and Test Commands

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run a single test by name
dotnet test --filter "FullyQualifiedName~DirectInstantiationAnalyzerTests.DirectInstantiation_List_ShouldNotReportDiagnostic"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Pack the analyzer as NuGet package
dotnet pack src/TestHarness.Analyzers/TestHarness.Analyzers.csproj

# Build with specific version (used in CI)
dotnet build --configuration Release /p:Version=0.1.0
```

## Architecture

### Project Structure

```
TestHarness.Analyzers/
├── src/
│   └── TestHarness.Analyzers/           # Main analyzer library (netstandard2.0)
│       ├── Analyzers/                   # Diagnostic implementations by category
│       │   ├── DirectDependencies/      # SEAM001-003
│       │   ├── StaticDependencies/      # SEAM004-008
│       │   ├── InheritanceBlockers/     # SEAM009-011
│       │   ├── GlobalState/             # SEAM012-014
│       │   └── Infrastructure/          # SEAM015-018
│       ├── CodeFixes/                   # Code fix providers (parallel structure)
│       │   ├── DirectDependencies/
│       │   ├── StaticDependencies/
│       │   ├── InheritanceBlockers/
│       │   ├── GlobalState/
│       │   └── Infrastructure/
│       ├── DiagnosticIds.cs             # All rule ID constants
│       ├── DiagnosticDescriptors.cs     # Rule definitions with metadata
│       ├── AnalyzerConfigOptions.cs     # EditorConfig support for exclusions
│       ├── AnalyzerReleases.Shipped.md  # Released rules
│       └── AnalyzerReleases.Unshipped.md # Unreleased rules
├── tests/
│   ├── TestHarness.Analyzers.Tests/     # xUnit tests (net8.0)
│   │   ├── AnalyzerTests/               # Tests per analyzer
│   │   └── Verifiers/                   # Test infrastructure
│   └── TestHarness.Analyzers.Samples/   # Sample code with violations
├── docs/
│   └── rules/                           # Markdown documentation per rule
└── .github/
    └── workflows/ci.yml                 # GitHub Actions CI/CD
```

### Complete Diagnostic Rules Reference

| Rule ID | Name | Category | Default Severity | Has Code Fix |
|---------|------|----------|------------------|--------------|
| SEAM001 | DirectInstantiationAnalyzer | DirectDependencies | Info | Yes |
| SEAM002 | ConcreteConstructorParameterAnalyzer | DirectDependencies | Disabled | No |
| SEAM003 | ServiceLocatorAnalyzer | DirectDependencies | Warning | Yes |
| SEAM004 | StaticMethodCallAnalyzer | StaticDependencies | Disabled | No |
| SEAM005 | DateTimeNowAnalyzer | StaticDependencies | Info | Yes |
| SEAM006 | GuidNewGuidAnalyzer | StaticDependencies | Info | Yes |
| SEAM007 | EnvironmentVariableAnalyzer | StaticDependencies | Info | No |
| SEAM008 | StaticPropertyAccessAnalyzer | StaticDependencies | Disabled | No |
| SEAM009 | SealedClassAnalyzer | InheritanceBlockers | Disabled | Yes |
| SEAM010 | NonVirtualMethodAnalyzer | InheritanceBlockers | Disabled | Yes |
| SEAM011 | ComplexPrivateMethodAnalyzer | InheritanceBlockers | Disabled | No |
| SEAM012 | SingletonPatternAnalyzer | GlobalState | Warning | Yes |
| SEAM013 | StaticMutableFieldAnalyzer | GlobalState | Warning | No |
| SEAM014 | AmbientContextAnalyzer | GlobalState | Warning | No |
| SEAM015 | FileSystemAccessAnalyzer | Infrastructure | Info | No |
| SEAM016 | HttpClientCreationAnalyzer | Infrastructure | Warning | Yes |
| SEAM017 | DatabaseAccessAnalyzer | Infrastructure | Info | No |
| SEAM018 | ProcessStartAnalyzer | Infrastructure | Info | No |

### Key Patterns

**Analyzer Implementation:**
```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.MyRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        // Analysis logic here
    }
}
```

**Code Fix Implementation:**
```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MyCodeFix))]
[Shared]
public sealed class MyCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.MyRule);

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // Register fix action
    }
}
```

**Test Pattern:**
```csharp
[Fact]
public async Task TestName()
{
    const string source = """
        // Test code here
        """;

    // No expected diagnostics = passes if no warnings
    await CSharpAnalyzerVerifier<AnalyzerType>.VerifyAnalyzerAsync(source);

    // With expected diagnostic
    var expected = CSharpAnalyzerVerifier<AnalyzerType>.Diagnostic("SEAM001")
        .WithSpan(lineStart, columnStart, lineEnd, columnEnd)
        .WithArguments("TypeName");
    await CSharpAnalyzerVerifier<AnalyzerType>.VerifyAnalyzerAsync(source, expected);
}
```

**Code Fix Test Pattern:**
```csharp
[Fact]
public async Task TestCodeFix()
{
    const string source = """
        // Code with diagnostic
        """;

    const string fixedSource = """
        // Code after fix applied
        """;

    var expected = CSharpCodeFixVerifier<AnalyzerType, CodeFixType>.Diagnostic("SEAM001")
        .WithSpan(line, col, line, endCol)
        .WithArguments("arg");
    await CSharpCodeFixVerifier<AnalyzerType, CodeFixType>.VerifyCodeFixAsync(source, expected, fixedSource);
}
```

## Configuration

### EditorConfig Options

```ini
# Disable a rule entirely
dotnet_diagnostic.SEAM001.severity = none

# Enable a disabled-by-default rule
dotnet_diagnostic.SEAM009.severity = warning

# Exclude specific types from analysis
dotnet_code_quality.SEAM001.excluded_types = T:MyNamespace.AllowedFactory

# Exclude specific methods from analysis
dotnet_code_quality.SEAM004.excluded_methods = M:System.Console.WriteLine

# Exclude entire namespaces from analysis
dotnet_code_quality.SEAM009.excluded_namespaces = MyNamespace.Internal

# Configure complexity threshold for SEAM011 (default: 50)
dotnet_code_quality.SEAM011.complexity_threshold = 30
```

### Symbol Format for Exclusions

- **Types**: `T:Namespace.TypeName` (e.g., `T:MyApp.Services.Logger`)
- **Methods**: `M:Namespace.TypeName.MethodName` (e.g., `M:System.Console.WriteLine`)
- **Namespaces**: `Namespace.SubNamespace` (e.g., `MyApp.Internal`)

Multiple values can be separated by commas or semicolons.

## Adding a New Analyzer

1. **Add diagnostic ID** to `DiagnosticIds.cs`:
   ```csharp
   public const string MyNewRule = "SEAM019";
   ```

2. **Add diagnostic descriptor** to `DiagnosticDescriptors.cs`:
   ```csharp
   public static readonly DiagnosticDescriptor MyNewRule = new(
       DiagnosticIds.MyNewRule,
       title: "Short title",
       messageFormat: "Message with '{0}' placeholder",
       category: "Category",
       defaultSeverity: DiagnosticSeverity.Info,
       isEnabledByDefault: true,
       description: "Longer description.",
       helpLinkUri: HelpLinkBase + "SEAM019.md");
   ```

3. **Create analyzer class** in appropriate `Analyzers/` subfolder

4. **Create code fix** (optional) in parallel `CodeFixes/` subfolder

5. **Add tests** in `tests/TestHarness.Analyzers.Tests/AnalyzerTests/`

6. **Add documentation** in `docs/rules/SEAM019.md`

7. **Update release tracking** in `AnalyzerReleases.Unshipped.md`:
   ```
   SEAM019 | Category | Severity | Description of the rule
   ```

## Analyzer Release Tracking

| File | When to Update |
|------|----------------|
| `AnalyzerReleases.Unshipped.md` | When adding, modifying, or removing diagnostic rules |
| `AnalyzerReleases.Shipped.md` | At release time only |

**Adding/Modifying Rules (Unshipped.md):**
- Add new rules to the `### New Rules` table
- Format: `Rule ID | Category | Severity | Notes`
- Severity: `Info`, `Warning`, `Error`, or `Disabled`

**At Release Time:**
1. Move content from `Unshipped.md` to `Shipped.md`
2. Add a version header: `## Release X.Y.Z`
3. Clear `Unshipped.md` (keep only the header comments)

## Code Style

- All analyzers must be `sealed`
- Enable concurrent execution: `context.EnableConcurrentExecution()`
- Configure generated code analysis: `context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`
- Private methods after public ones
- Use raw string literals (`"""`) for test source code
- All code fixes must support batch fixing via `WellKnownFixAllProviders.BatchFixer`
- ImplicitUsings is disabled in the main project; use explicit usings

## CI/CD Pipeline

The project uses GitHub Actions (`.github/workflows/ci.yml`):

**Triggers:**
- Push to `main` branch
- Pull requests to `main` branch

**Steps:**
1. Checkout with full history (required for GitVersion)
2. Install and execute GitVersion 6.x
3. Setup .NET 8.0
4. Restore dependencies
5. Build with Release configuration
6. Run tests
7. (main only) Pack NuGet package with GitVersion-derived version
8. (main only) Upload artifact (30-day retention)
9. (main only) Publish to NuGet.org

## Branching Strategy

This project uses GitVersion for semantic versioning:

| Branch Pattern | Purpose | Version Label |
|----------------|---------|---------------|
| `main` | Production releases | Stable version |
| `feature/*` | New features | `-alpha` |
| `hotfix/*` | Urgent fixes | `-beta` |
| `pull-request/*` | PRs | `-pr` |

### Workflow
1. Create feature branches from `main` using `feature/description` naming
2. Open PR targeting `main`
3. CI runs on all PRs and main branch pushes
4. Merge to `main` triggers versioned build and NuGet package

### Version Bumping
- **Default**: Patch increment (0.1.0 → 0.1.1)
- **Minor bump**: Include `+semver: minor` in commit message (0.1.0 → 0.2.0)
- **Major bump**: Include `+semver: major` in commit message (0.1.0 → 1.0.0)

## Dependencies

**Main Project (netstandard2.0):**
- Microsoft.CodeAnalysis.CSharp 4.8.0
- Microsoft.CodeAnalysis.CSharp.Workspaces 4.8.0
- Microsoft.CodeAnalysis.Analyzers 3.3.4

**Test Project (net8.0):**
- xUnit 2.6.6
- Microsoft.CodeAnalysis.CSharp.Analyzer.Testing 1.1.2
- Microsoft.CodeAnalysis.CSharp.CodeFix.Testing 1.1.2

## Current Test Coverage

Tests exist for these analyzers:
- DirectInstantiationAnalyzer (SEAM001)
- DateTimeNowAnalyzer (SEAM005)
- GuidNewGuidAnalyzer (SEAM006)
- SealedClassAnalyzer (SEAM009)
- SingletonPatternAnalyzer (SEAM012)

The remaining analyzers have working implementations but limited test coverage.
