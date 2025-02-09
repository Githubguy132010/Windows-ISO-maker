param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [switch]$Push
)

# Validate version format
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "Version must be in format X.Y.Z (e.g. 1.0.0)"
    exit 1
}

# Update version in csproj
$csproj = Get-Content "Windows-ISO-Maker.csproj" -Raw
$newCsproj = $csproj -replace '(<Version>)(.*?)(</Version>)', "`$1$Version`$3"
Set-Content "Windows-ISO-Maker.csproj" $newCsproj -NoNewline

# Create and push git tag
git add Windows-ISO-Maker.csproj
git commit -m "Release v$Version"
git tag -a "v$Version" -m "Release v$Version"

if ($Push) {
    git push
    git push origin "v$Version"
    Write-Host "Version v$Version has been committed and pushed"
    Write-Host "GitHub Actions will now build and create the release"
} else {
    Write-Host "Version v$Version has been committed locally"
    Write-Host "Run with -Push to push the changes and trigger the release"
}