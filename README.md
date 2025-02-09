# Windows ISO Maker

A modern Windows application for customizing Windows ISO files with drivers and custom settings. Built with WPF and Fluent Design.

![Windows ISO Maker Screenshot](Screenshots/main.png)

## Features

- Modern Fluent Design UI with Windows 11 style
- Automatic dark/light theme support
- Driver injection into Windows ISO files
- Windows Recovery Environment (WinRE) customization
- Progress tracking with detailed status updates
- Automatic tool management (PowerShell, ADK tools)

## Requirements

- Windows 10 or later
- .NET 7.0 Runtime (will be downloaded automatically if needed)
- Administrator privileges (required for ISO mounting and DISM operations)

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. Extract `Windows-ISO-Maker-vX.X.X.zip`
3. Run `Windows-ISO-Maker.exe`

The application will automatically download and set up required tools on first run.

## Usage

1. Click "Select Windows ISO" to choose your base Windows ISO file
2. (Optional) Click "Add Drivers" to select a folder containing drivers to inject
3. Configure advanced options if needed:
   - Enable Windows Recovery Environment
   - Keep original ISO options
   - Add custom boot logo
4. Click "Create Custom ISO" to start the customization process

The customized ISO will be created in the same directory as the source ISO with "Custom_" prefix.

## Building from Source

### Prerequisites

- Visual Studio 2022 or later
- .NET 7.0 SDK
- Git

### Build Steps

1. Clone the repository:
```bash
git clone https://github.com/yourusername/Windows-ISO-Maker.git
```

2. Open `Windows-ISO-maker.sln` in Visual Studio

3. Build the solution:
```bash
dotnet restore
dotnet build
```

4. Run the application:
```bash
dotnet run
```

### Creating a Release

Use the provided PowerShell script to create a new release:

```powershell
.\Scripts\create-release.ps1 -Version "1.0.0" -Push
```

This will:
- Update the version number
- Create a git tag
- Push changes to trigger the release workflow

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, development workflow, and pull request process.

Quick start:
1. Fork the repository
2. Create a new branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

We use [Conventional Commits](https://www.conventionalcommits.org/) for commit messages.

## License

Distributed under the MIT License. See `LICENSE` for more information.

## Acknowledgments

- [WPF-UI](https://github.com/lepoco/wpfui) for the modern Fluent Design controls
- Windows ADK for the ISO creation tools
- PowerShell 7 for system automation