[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$repoRoot = Split-Path -Parent $PSScriptRoot
$runtimeDir = Join-Path $repoRoot "vendor\runtime"
$cacheRoot = Join-Path $repoRoot "work\dependencies"
$libreDll = Join-Path $runtimeDir "LibreHardwareMonitorLib.dll"
$pawnInstaller = Join-Path $runtimeDir "PawnIO_setup.exe"

if (-not $Force -and (Test-Path $libreDll) -and (Test-Path $pawnInstaller)) {
    Write-Host "Runtime dependencies already exist. Use -Force to download again."
    exit 0
}

New-Item -ItemType Directory -Force -Path $runtimeDir, $cacheRoot | Out-Null
$runDir = Join-Path $cacheRoot ("download-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

$releaseZip = Join-Path $runDir "LibreHardwareMonitor.zip"
$releaseExtract = Join-Path $runDir "release"
$pawnTemp = Join-Path $runDir "PawnIO_setup.exe"

$releaseAssetUrl = "https://api.github.com/repos/LibreHardwareMonitor/LibreHardwareMonitor/releases/assets/355828308"
$releaseExpectedHash = "086D9F1B5A99E643EDC2CFAAAC16051685B551E4C5AC0B32A57C58C0E529C001"

Write-Host "Downloading LibreHardwareMonitor 0.9.6 from the official release..."
Invoke-WebRequest -Uri $releaseAssetUrl -Headers @{
    Accept = "application/octet-stream"
    "User-Agent" = "hardware-monitoring-center-build"
} -OutFile $releaseZip

$releaseHash = (Get-FileHash $releaseZip -Algorithm SHA256).Hash
if ($releaseHash -ne $releaseExpectedHash) {
    throw "LibreHardwareMonitor archive hash mismatch. Expected $releaseExpectedHash, got $releaseHash."
}

Expand-Archive -LiteralPath $releaseZip -DestinationPath $releaseExtract -Force
Copy-Item -Path (Join-Path $releaseExtract "*.dll") -Destination $runtimeDir -Force

$pawnUrl = "https://api.github.com/repos/LibreHardwareMonitor/LibreHardwareMonitor/contents/LibreHardwareMonitor/Resources/PawnIO_setup.exe?ref=v0.9.6"
$pawnExpectedHash = "A3A46226C5E2824F4CDD42BE0EECBABFC672C86F7889710F5AB1E6AD385B47A0"

Write-Host "Downloading the official PawnIO setup bundled by LibreHardwareMonitor..."
Invoke-WebRequest -Uri $pawnUrl -Headers @{
    Accept = "application/vnd.github.raw+json"
    "User-Agent" = "hardware-monitoring-center-build"
} -OutFile $pawnTemp

$pawnHash = (Get-FileHash $pawnTemp -Algorithm SHA256).Hash
if ($pawnHash -ne $pawnExpectedHash) {
    throw "PawnIO setup hash mismatch. Expected $pawnExpectedHash, got $pawnHash."
}
Copy-Item -LiteralPath $pawnTemp -Destination $pawnInstaller -Force

Write-Host "Dependencies verified:"
Write-Host "  LibreHardwareMonitor.zip SHA256 $releaseHash"
Write-Host "  PawnIO_setup.exe         SHA256 $pawnHash"
Write-Host "Runtime directory: $runtimeDir"
