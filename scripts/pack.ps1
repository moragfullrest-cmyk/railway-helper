param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "./artifacts"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    Write-Host "Restoring..." -ForegroundColor Cyan
    dotnet restore RailwayHelper.sln

    Write-Host "Building..." -ForegroundColor Cyan
    dotnet build RailwayHelper.sln -c $Configuration --no-restore

    Write-Host "Testing..." -ForegroundColor Cyan
    dotnet test RailwayHelper.sln -c $Configuration --no-build

    if (Test-Path $OutputDir) {
        Remove-Item $OutputDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $OutputDir | Out-Null

    Write-Host "Packing..." -ForegroundColor Cyan
    dotnet pack src/RailwayHelper/RailwayHelper.csproj -c $Configuration --no-build -o $OutputDir

    Write-Host "Done. Packages in $OutputDir" -ForegroundColor Green
    Get-ChildItem $OutputDir
}
finally {
    Pop-Location
}
