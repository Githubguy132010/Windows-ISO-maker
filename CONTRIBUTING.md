# Contributing to Windows ISO Maker

First off, thanks for taking the time to contribute! 👍

## Development Setup

1. Install prerequisites:
   - Visual Studio 2022 or later with the following workloads:
     - .NET Desktop Development
     - Universal Windows Platform development
   - Windows 10 SDK version 19041 or later
   - PowerShell 7.3 or later
   - Git

2. Clone and build:
   ```powershell
   git clone https://github.com/yourusername/Windows-ISO-Maker.git
   cd Windows-ISO-Maker
   dotnet restore
   ```

## Development Workflow

### Branch Naming Convention

- `feature/*` - New features
- `bugfix/*` - Bug fixes
- `docs/*` - Documentation changes
- `test/*` - Test improvements
- `refactor/*` - Code refactoring

### Commit Messages

Follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

- `feat: add new driver injection feature`
- `fix: resolve ISO mounting issue`
- `docs: update README with new screenshots`
- `test: add unit tests for IsoService`
- `refactor: improve error handling`

### Testing

Run tests before submitting a PR:
```powershell
dotnet test
```

### Creating a Release

1. Update version in `Windows-ISO-Maker.csproj`
2. Run the release script:
   ```powershell
   .\Scripts\create-release.ps1 -Version "X.Y.Z" -Push
   ```

## Code Style

- Follow Microsoft's [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use the provided `.editorconfig` settings
- Write XML documentation comments for public APIs
- Include unit tests for new features

## Pull Request Process

1. Update documentation
2. Add or update unit tests
3. Ensure CI passes
4. Request review from maintainers
5. Address review comments

## Resources

- [WPF-UI Documentation](https://wpfui.lepo.co/documentation/)
- [Windows ADK Documentation](https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install)
- [DISM API Reference](https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism/dism-api-reference)