param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publishDirectory = Join-Path $root "artifacts\publish\win-x64"
$projectPath = Join-Path $root "ApexTracker.UI\ApexTracker.UI.csproj"
$scriptPath = Join-Path $PSScriptRoot "ApexTracker.iss"
$isccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDirectory `
    /p:PublishSingleFile=false

$isccPath = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $isccPath)
{
    throw "Inno Setup 6 was not found. Install it, then run this script again."
}

& $isccPath $scriptPath
