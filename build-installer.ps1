<#
.SYNOPSIS
    Builds ScalyTails and packages it into a Windows installer.

.DESCRIPTION
    Step 1 - publishes ScalyTails as a self-contained win-x64 app (no .NET required on target machine).
    Step 2 - compiles installer\ScalyTails.iss with Inno Setup into dist\ScalyTailsSetup.exe.

.REQUIREMENTS
    - .NET 8 SDK  (https://dotnet.microsoft.com/download/dotnet/8.0)
    - Inno Setup 6 (https://jrsoftware.org/isinfo.php)

.EXAMPLE
    .\build-installer.ps1
#>

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host ""
Write-Host "  ScalyTails - Installer Builder" -ForegroundColor Cyan
Write-Host "  ================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Publish

Write-Host "[1/2]  Publishing (self-contained, win-x64)..." -ForegroundColor Yellow

$publishDir = Join-Path $root "publish"

if (Test-Path $publishDir) {
    Write-Host "       Cleaning old publish output..."
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish "$root\ScalyTails.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output "$publishDir"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  ERROR: dotnet publish failed (exit code $LASTEXITCODE)." -ForegroundColor Red
    exit 1
}

Write-Host "       Published to: publish\" -ForegroundColor DarkGray

# Step 2: Compile installer

Write-Host "[2/2]  Compiling installer with Inno Setup..." -ForegroundColor Yellow

$distDir = Join-Path $root "dist"
if (-not (Test-Path $distDir)) {
    New-Item $distDir -ItemType Directory | Out-Null
}

$isccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "ISCC.exe"
)

$iscc = $isccCandidates |
    Where-Object { Test-Path $_ -ErrorAction SilentlyContinue } |
    Select-Object -First 1

if (-not $iscc) {
    Write-Host ""
    Write-Host "  ERROR: Inno Setup 6 not found." -ForegroundColor Red
    Write-Host "  Download it from: https://jrsoftware.org/isinfo.php" -ForegroundColor Red
    Write-Host ""
    exit 1
}

& $iscc "$root\installer\ScalyTails.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "  ERROR: Inno Setup compilation failed (exit code $LASTEXITCODE)." -ForegroundColor Red
    exit 1
}

# Done

$outFile = Join-Path $distDir "ScalyTailsSetup.exe"
$size    = [math]::Round((Get-Item $outFile).Length / 1MB, 1)

Write-Host ""
Write-Host "  Done!  Installer: dist\ScalyTailsSetup.exe  ($size MB)" -ForegroundColor Green
Write-Host ""
