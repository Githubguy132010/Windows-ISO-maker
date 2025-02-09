# Prepare tools for Windows ISO Maker
$ErrorActionPreference = "Stop"

# Create directories
$toolsDir = Join-Path $PSScriptRoot "..\Resources"
$tempDir = Join-Path $env:TEMP "WindowsISOMakerTools"
New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

# Download and extract PowerShell
Write-Host "Downloading PowerShell..."
$pwshUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.3.6/PowerShell-7.3.6-win-x64.zip"
$pwshZip = Join-Path $tempDir "pwsh.zip"
Invoke-WebRequest -Uri $pwshUrl -OutFile $pwshZip
Expand-Archive -Path $pwshZip -DestinationPath (Join-Path $tempDir "pwsh") -Force

# Create minimal PowerShell package
Write-Host "Creating minimal PowerShell package..."
$pwshMinimal = Join-Path $tempDir "pwsh-minimal"
New-Item -ItemType Directory -Force -Path $pwshMinimal | Out-Null
$requiredFiles = @(
    "pwsh.exe",
    "pwsh.dll",
    "System.Management.Automation.dll",
    "Microsoft.PowerShell.Commands.Management.dll",
    "Microsoft.PowerShell.Commands.Utility.dll"
)

foreach ($file in $requiredFiles) {
    Copy-Item -Path (Join-Path $tempDir "pwsh" $file) -Destination (Join-Path $pwshMinimal $file)
}

# Package minimal PowerShell
Compress-Archive -Path "$pwshMinimal\*" -DestinationPath (Join-Path $toolsDir "pwsh.zip") -Force

# Extract oscdimg from Windows ADK if available
Write-Host "Looking for Windows ADK..."
$adkPath = "C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg"

if (Test-Path $adkPath) {
    Write-Host "Found Windows ADK, packaging oscdimg..."
    $oscdimgDir = Join-Path $tempDir "oscdimg"
    New-Item -ItemType Directory -Force -Path $oscdimgDir | Out-Null
    Copy-Item -Path "$adkPath\oscdimg.exe" -Destination $oscdimgDir
    Copy-Item -Path "$adkPath\*.dll" -Destination $oscdimgDir
    Compress-Archive -Path "$oscdimgDir\*" -DestinationPath (Join-Path $toolsDir "oscdimg.zip") -Force
} else {
    Write-Host "Windows ADK not found. Please install Windows ADK and run this script again."
    Write-Host "You can download Windows ADK from: https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install"
    exit 1
}

# Clean up
Remove-Item -Recurse -Force $tempDir

Write-Host "Tools preparation completed successfully!"
Write-Host "The following files have been created:"
Write-Host "- $toolsDir\pwsh.zip"
Write-Host "- $toolsDir\oscdimg.zip"