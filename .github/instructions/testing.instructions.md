---
description: 'Unit testing standards, test project setup, naming, build & run commands'
applyTo: '**/*Tests.cs, **/*Test.cs, **/*.Tests.csproj'
---

# Testing — Unit Tests, Build & Run

**New** business logic (services, ViewModels, helpers) must have unit tests. Existing code should be covered as it is refactored.

---

## Current State & Scope

The MagicCursor repository currently contains **no unit test project** (`MagicCursor.Tests`), analyzers, or automated test suites. The legacy overlay code in `MainPage.xaml.cs` relies on Win32 hooks and UI-coupled handlers that are not unit testable as-is.

- **Policy**:
  - Existing overlay code in `MainPage.xaml.cs` is exempt from retroactive unit tests until isolated or refactored.
  - When implementing **new features (voice mode, chat functionality)**, establish the test project and author unit tests for new services, ViewModels, and helpers.
  - Design new features against interfaces (`IVoiceService`, `IChatService`) to make them mockable and testable without hardware (mic) or cloud API dependencies.

---

## 1. Test Framework & Project Setup (When You Create the Test Project)

### Recommended Stack

| Component | Package | Purpose |
|---|---|---|
| Test Framework | `MSTest` | Test runner & assertions |
| Mocking | `Moq` | Mock dependencies & interfaces |
| UI Testing | `Microsoft.Windows.Apps.Test` | WinUI UI automation (optional) |

### Test Project Setup

When creating the test project alongside the main project:

```
<SolutionRoot>/
  MagicCursor/           ← Main app project
  MagicCursor.Tests/     ← Unit test project
```

Test project `.csproj` should reference the main project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Match TargetFramework to the main project's .csproj -->
    <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
    <UseWinUI>true</UseWinUI>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MSTest.TestAdapter" Version="*" />
    <PackageReference Include="MSTest.TestFramework" Version="*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="*" />
    <PackageReference Include="Moq" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MagicCursor\MagicCursor.csproj" />
  </ItemGroup>
</Project>
```

---

## 2. Test Writing Rules

### What to Test
- **All public methods** in ViewModels, Services, Helpers, and Models.
- **Edge cases** — null inputs, empty collections, boundary values.
- **Error paths** — exception handling, invalid state transitions.
- **Business logic** — calculations, transformations, state management.

### What NOT to Test (Directly)
- XAML layout / visual rendering (use UI tests for that).
- Framework internals (e.g., `InitializeComponent()`).
- Private methods — test them indirectly through public methods.

### Test Naming Convention

Use the pattern: `MethodName_Scenario_ExpectedResult`

```csharp
[TestMethod]
public void CalculateTotal_WithEmptyCart_ReturnsZero() { }

[TestMethod]
public void LoadDataAsync_WhenServiceThrows_SetsErrorState() { }

[TestMethod]
public async Task SaveAsync_WithValidInput_ReturnsTrue() { }
```

### Test Structure (AAA Pattern)

Every test follows **Arrange → Act → Assert**:

```csharp
[TestMethod]
public void Add_TwoPositiveNumbers_ReturnsSum()
{
    // Arrange
    var calculator = new Calculator();

    // Act
    int result = calculator.Add(2, 3);

    // Assert
    Assert.AreEqual(5, result);
}
```

### Voice & Chat Testing Guidance

- **Service Interfaces**: Never depend directly on concrete audio drivers or live cloud LLM sockets in ViewModels. Always define interfaces (`IVoiceService`, `IChatService`, `ISpeechRecognitionService`) and mock them using `Moq`.
- **Chat ViewModel Testing**:
  - Test initial state (empty message collection, not streaming, input blank).
  - Test command execution: `SendMessageCommand` appends user message immediately and flags `IsGenerating = true`.
  - Test streaming callbacks: incoming token chunks update the last assistant message.
  - Test error resilience: API exceptions trigger error messages without crashing the ViewModel.
- **Voice State Testing**:
  - Test state changes (`Idle` -> `Listening` -> `Recognized` -> `Error`).
  - Test hotkey / activation event handlers without physical microphone input.

### ViewModel & Mock Testing Example (Chat / Voice)

```csharp
[TestMethod]
public async Task SendMessageAsync_WhenInvoked_AppendsUserAndAssistantMessages()
{
    // Arrange
    var mockChat = new Mock<IChatService>();
    mockChat
        .Setup(s => s.StreamResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .Returns(new[] { "Hello", " world!" }.ToAsyncEnumerable());

    var viewModel = new ChatViewModel(mockChat.Object);
    viewModel.InputText = "Hi";

    // Act
    await viewModel.SendMessageCommand.ExecuteAsync(null);

    // Assert
    Assert.AreEqual(2, viewModel.Messages.Count); // User prompt + Assistant reply
    Assert.AreEqual("Hello world!", viewModel.Messages[1].Content);
    Assert.IsFalse(viewModel.IsGenerating);
}
```

---

## 3. Test Organization

### File Structure

Mirror the main project's folder structure (defined in [winui-best-practices](winui-best-practices.instructions.md)) in the test project. As the main project grows with subfolders under `ViewModels/`, `Services/`, `Views/`, etc., the test project must grow organically in the same way. This alignment enables on-demand test runs scoped to the area you changed:

```
<ProjectName>/                    <ProjectName>.Tests/
  Models/                           Models/
    User.cs                           UserTests.cs
  ViewModels/                       ViewModels/
    MainViewModelTests.cs             MainViewModelTests.cs
    Settings/                         Settings/
      ThemeViewModel.cs                 ThemeViewModelTests.cs
  Services/                         Services/
    DataService.cs                    DataServiceTests.cs
    Auth/                             Auth/
      AuthService.cs                    AuthServiceTests.cs
  Helpers/                          Helpers/
    StringHelper.cs                   StringHelperTests.cs
  Converters/                       Converters/
    BoolToVisibilityConverter.cs      BoolToVisibilityConverterTests.cs
```

### One Test Class per Class Under Test

```csharp
namespace <RootNamespace>.Tests.ViewModels;

[TestClass]
public class MainViewModelTests
{
    // All tests for MainViewModel go here
}
```

### Running Tests On-Demand

After a change, run only the tests related to the affected area instead of the full suite. Detect the platform first (matching the convention in `.github/agents/Agents.md`):

```powershell
# Run from the test project folder
cd <ProjectName>.Tests

# Detect platform once per session (AMD64 -> x64; ARM64/x86 unchanged)
$arch = $env:PROCESSOR_ARCHITECTURE
$Platform = if ($arch -eq 'AMD64') { 'x64' } else { $arch }

# Run tests for a specific class
dotnet test -c Debug -p:Platform=$Platform --filter "FullyQualifiedName~MainViewModelTests"

# Run a single test
dotnet test -c Debug -p:Platform=$Platform --filter "FullyQualifiedName~MainViewModelTests.LoadItemsAsync_OnSuccess_PopulatesItems"

# Run all tests in a namespace (e.g., all ViewModel tests)
dotnet test -c Debug -p:Platform=$Platform --filter "FullyQualifiedName~Tests.ViewModels"

# Run tests in a subfolder namespace (e.g., only Settings ViewModels)
dotnet test -c Debug -p:Platform=$Platform --filter "FullyQualifiedName~Tests.ViewModels.Settings"

# Run the full suite (for cross-cutting changes)
dotnet test -c Debug -p:Platform=$Platform
```
```

---

## 4. Test-Specific Commands

For general build and register commands, see **Build, Run & Deploy** in `.github/agents/Agents.md`.
For on-demand test filtering, see **Running Tests On-Demand** above.

Below are additional test commands:

### Build Only the Test Project

```powershell
cd <ProjectName>.Tests
$arch = $env:PROCESSOR_ARCHITECTURE
$Platform = if ($arch -eq 'AMD64') { 'x64' } else { $arch }
dotnet build -c Debug -p:Platform=$Platform
```

### Run Tests with Verbose Output

```powershell
dotnet test -c Debug -p:Platform=$Platform --verbosity normal
```

---

## 5. Agent Workflow for Tests

When you write or modify code, follow this sequence:

1. **Implement the feature or fix** in the main project.
2. **Write unit tests** for every new/changed public method in new services, ViewModels, and helpers. (Existing untouched legacy methods in `MainPage.xaml.cs` are exempt).
3. **Build** the solution and fix any compilation errors or warnings.
4. **Run tests** — `dotnet test -c Debug -p:Platform=$Platform` once the test project is present and ensure all pass.
5. **Review** — Confirm tests cover the happy path, edge cases, and error cases.

### When Modifying Existing Code

1. **Run existing tests first** (once a test suite is created) to establish a baseline.
2. Make the code change.
3. **Run tests again** — fix any failures.
4. **Add new tests** for any new behaviour or when refactoring legacy routines into testable services.

### Coverage Goals

- Aim for **80%+ code coverage** on new business logic (ViewModels, Services).
- 100% coverage of utility/helper methods.
- Legacy overlay code-behind (`MainPage.xaml.cs`) is exempt from unit test coverage until refactored into services.

---

## 6. Common Test Pitfalls

| Pitfall | Fix |
|---|---|
| Test depends on another test's state | Each test must be fully independent |
| Testing multiple things in one test | One assertion per logical concept |
| Tests pass but don't actually verify anything | Always have meaningful assertions |
| Mocking too much | Mock only external dependencies, not the class under test |
| Testing implementation details | Test behaviour and outcomes, not internal method calls |
| Async tests without `await` | Always `await` async methods and use `async Task` return type |

---

## Validation

- Build & run tests once `MagicCursor.Tests` is created.
- Verify all tests pass — zero failures.
- Verify naming follows `MethodName_Scenario_ExpectedResult` pattern.
- Verify AAA structure (Arrange/Act/Assert) in every test method.
- Confirm coverage on new ViewModels/Services (ChatViewModel, VoiceService).

### Verification Checklist

- [ ] New services and ViewModels implement interfaces for mockability
- [ ] Unit tests cover happy path, streaming/state events, and error paths
- [ ] Test project (`MagicCursor.Tests`) builds and executes cleanly
- [ ] Naming follows `MethodName_Scenario_ExpectedResult` pattern
- [ ] Legacy overlay code untouched by new features is cleanly separated

---

## Must Read & Research

> **Agent Rule:** Before writing or modifying tests, you **must** fetch and review the relevant references below using `fetch_webpage`. Apply what you learn — do not skip this step.

| # | Reference | When to consult |
|---|---|---|
| 1 | [Unit testing C# with MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest) | Setting up test project, writing first tests, MSTest attributes |
| 2 | [Unit testing best practices .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices) | Every time you write tests — naming, structure, AAA pattern |
| 3 | [Moq Quickstart](https://github.com/devlooped/moq/wiki/Quickstart) | Mocking interfaces, setting up `Returns`/`Throws`, verifying calls |
| 4 | [FluentAssertions Documentation](https://fluentassertions.com/introduction) | Writing expressive assertions (`Should().Be()`, collections, exceptions) |
| 5 | [dotnet test CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test) | Running tests from terminal, filtering, verbosity options |
| 6 | [Test Explorer in Visual Studio](https://learn.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer) | Debugging tests, viewing coverage, understanding test output |


