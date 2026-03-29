# Contributing to BrowserAptor

Thank you for your interest in contributing to BrowserAptor! Whether you're fixing a bug,
adding a new feature, improving documentation, or writing tests, your contribution is
welcome and appreciated.

---

## Code of Conduct

This project adheres to a standard contributor code of conduct. We expect all participants
to be respectful and constructive. Please report unacceptable behaviour by opening a
[GitHub issue](https://github.com/sassdawe/browseraptor/issues) with the `conduct` label.

---

## Prerequisites

Before you begin, make sure you have the following installed:

| Tool | Version | Notes |
|------|---------|-------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10) | 10.0.x | Required to build and test |
| [Visual Studio 2022](https://visualstudio.microsoft.com/) **or** [VS Code](https://code.visualstudio.com/) | VS 2022 17.12+ / any recent VS Code | VS 2022 with the *.NET desktop development* workload is recommended for the WPF project; VS Code with the **C# Dev Kit** extension also works |
| [Git](https://git-scm.com/) | 2.x+ | For cloning and branching |
| Windows 10 / 11 (optional for Core tests) | — | The WPF project (`BrowserAptor`) only builds and runs on Windows. The `BrowserAptor.Core` library and its tests are cross-platform. |

> **Tip:** [Windows Terminal](https://aka.ms/terminal) is a great shell host for running
> .NET CLI commands on Windows.

---

## Setting Up Your Development Environment

### 1. Fork the Repository

Click **Fork** on the [GitHub repository page](https://github.com/sassdawe/browseraptor)
to create your own copy.

### 2. Clone Your Fork

```bash
git clone https://github.com/<your-username>/browseraptor.git
cd browseraptor
```

### 3. Add the Upstream Remote

```bash
git remote add upstream https://github.com/sassdawe/browseraptor.git
git fetch upstream
```

### 4. Restore Dependencies

```bash
dotnet restore BrowserAptor.slnx
```

### 5. Build the Solution

```bash
dotnet build BrowserAptor.slnx
```

> On non-Windows systems, the `BrowserAptor` WPF project will be skipped automatically
> because it targets `net10.0-windows`. The `BrowserAptor.Core` library and tests will
> still build and run normally.

---

## Development Workflow

### Branching

Always work on a feature branch rather than directly on `main`:

```bash
git checkout -b feature/my-feature-name
# or
git checkout -b fix/short-description-of-bug
```

Use descriptive, lowercase, hyphen-separated branch names:

- `feature/add-librewolf-detection`
- `fix/firefox-profile-parsing-on-windows-11`
- `docs/update-installation-guide`

### Build

```bash
dotnet build BrowserAptor.slnx
```

### Run Tests

```bash
# Run all tests
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj

# Run with verbose output
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj --logger "console;verbosity=normal"

# Run a specific test class
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj --filter "FullyQualifiedName~FirefoxProfileParsingTests"
```

### Publish a Local Build

```powershell
dotnet publish src/BrowserAptor/BrowserAptor.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o publish/
```

### Verify Before Submitting

Before opening a pull request, ensure:

1. `dotnet build BrowserAptor.slnx` succeeds with **no warnings or errors**.
2. `dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj` passes with **all tests green**.
3. Your changes are covered by new or existing tests.

---

## Coding Conventions

BrowserAptor follows standard C# conventions. Please adhere to the following guidelines:

### C# Style

- Use **C# 12+** language features where appropriate.
- Use `var` when the type is obvious from the right-hand side; use explicit types otherwise.
- Prefer **expression-bodied members** for simple one-liners.
- Use **file-scoped namespaces** (e.g., `namespace BrowserAptor.Services;`).
- Prefer `string.IsNullOrEmpty` / `string.IsNullOrWhiteSpace` over null checks on strings.
- Async methods must be named with the `Async` suffix.
- Do not use magic strings—extract constants or use `nameof`.

### XML Documentation

- All `public` and `internal` members should have XML doc comments (`/// <summary>`).
- Describe the *what* and *why*, not just restating the method name.

### File Organisation

- One class / interface per file.
- File name must match the type name exactly.
- Place new services in `src/BrowserAptor.Core/Services/`.
- Place new models in `src/BrowserAptor.Core/Models/`.
- Place Windows-specific code in `src/BrowserAptor/`.

### Tests

- Test class names end with `Tests` (e.g., `FirefoxProfileParsingTests`).
- Test method names follow `MethodName_Condition_ExpectedResult` (e.g.,
  `BuildArguments_ChromiumProfile_ReturnsCorrectFlags`).
- Use xUnit `[Fact]` for single-case tests and `[Theory]` + `[InlineData]` for
  parameterised tests.
- Do not leave test output in the repository (no `Console.WriteLine` in tests).

---

## Submitting a Pull Request

1. **Push your branch** to your fork:

   ```bash
   git push origin feature/my-feature-name
   ```

2. **Open a pull request** against `main` on the upstream repository.

3. **Fill in the PR description** using the template:
   - Summary of the change and the motivation.
   - Link the related issue (e.g., `Fixes #42` or `Closes #17`).
   - List of notable implementation choices.
   - Testing steps or test coverage notes.

4. **Respond to code review feedback** promptly. Mark conversations as *Resolved* only
   after addressing them.

5. Once approved, a maintainer will **squash-merge** your PR.

### PR Checklist

- [ ] The branch is up to date with `main` (`git rebase upstream/main`).
- [ ] `dotnet build BrowserAptor.slnx` passes with no warnings.
- [ ] `dotnet test` passes.
- [ ] New or updated tests cover the change.
- [ ] XML doc comments added/updated for all changed public API.
- [ ] Relevant documentation (README, docs/) updated if necessary.

---

## Reporting Bugs

Use the [Bug report issue template](https://github.com/sassdawe/browseraptor/issues/new?template=bug_report.md)
and fill in all sections. Include:

- Steps to reproduce.
- Expected vs actual behaviour.
- BrowserAptor version (from the executable title bar or `--version`).
- OS version and .NET runtime version.

---

## Requesting Features

Use the [Feature request issue template](https://github.com/sassdawe/browseraptor/issues/new?template=feature_request.md).
Describe the problem your request solves, not just the desired UI.

---

## Platform Notes

| Project | Target | Notes |
|---------|--------|-------|
| `BrowserAptor.Core` | `net10.0` | Cross-platform. CI runs tests on Linux and Windows. |
| `BrowserAptor` (WPF) | `net10.0-windows` | Windows only. Requires Windows 10/11 to run. Build skipped on non-Windows agents. |
| `BrowserAptor.Tests` | `net10.0` | Cross-platform. Runs on Linux/Mac/Windows in CI. |

When adding code that touches Windows registry or WPF, keep it inside the `BrowserAptor`
WPF project or guard it with `[SupportedOSPlatform("windows")]`. Core library code must
remain platform-agnostic.

---

## Questions

If you have questions that are not answered here, feel free to open a
[discussion](https://github.com/sassdawe/browseraptor/discussions) or an issue labelled
`question`.

Thank you again for contributing! 🎉
