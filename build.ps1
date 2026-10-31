[CmdletBinding()]
param(
    [switch]$SkipDeps
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot
$runtimeDir = Join-Path $repoRoot "vendor\runtime"
$libreDll = Join-Path $runtimeDir "LibreHardwareMonitorLib.dll"
$pawnInstaller = Join-Path $runtimeDir "PawnIO_setup.exe"

if (-not $SkipDeps -and ((-not (Test-Path $libreDll)) -or (-not (Test-Path $pawnInstaller)))) {
    & (Join-Path $repoRoot "scripts\fetch-deps.ps1")
}
if (-not (Test-Path $libreDll)) {
    throw "Missing LibreHardwareMonitorLib.dll. Run scripts\fetch-deps.ps1 first."
}
if (-not (Test-Path $pawnInstaller)) {
    throw "Missing PawnIO_setup.exe. Run scripts\fetch-deps.ps1 first."
}

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw ".NET Framework C# compiler not found. Install .NET Framework 4.8 Developer Pack."
}

$distRoot = Join-Path $repoRoot "dist"
$stage = Join-Path $distRoot ("HardwareMonitorCenter-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
New-Item -ItemType Directory -Force -Path $stage | Out-Null
$exe = Join-Path $stage "HardwareMonitorCenter.exe"

$sourceFiles = @(
    (Join-Path $repoRoot "src\CleanTempCompare.cs"),
    (Join-Path $repoRoot "src\AdvancedUI.cs"),
    (Join-Path $repoRoot "src\SensorKnowledge.cs")
)
$references = @(
    "System.dll",
    "System.Core.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "System.Runtime.Serialization.dll",
    $libreDll
) -join ","

$compilerArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:x64",
    "/optimize+",
    "/codepage:65001",
    "/utf8output",
    "/win32icon:$repoRoot\assets\app.ico",
    "/win32manifest:$repoRoot\src\app.manifest",
    "/out:$exe",
    "/reference:$references"
) + $sourceFiles

& $csc @compilerArgs
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed with exit code $LASTEXITCODE."
}

Copy-Item -Path (Join-Path $runtimeDir "*.dll") -Destination $stage -Force
Copy-Item -LiteralPath $pawnInstaller -Destination $stage -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "src\app.config") -Destination (Join-Path $stage "HardwareMonitorCenter.exe.config") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination $stage -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "docs\USAGE.md") -Destination (Join-Path $stage "使用说明.md") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "LICENSE") -Destination $stage -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "NOTICE.md") -Destination $stage -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "THIRD-PARTY-LICENSE-MPL-2.0.txt") -Destination $stage -Force

$zip = Join-Path $distRoot "HardwareMonitorCenter-v2.3-Windows-x64.zip"
if (Test-Path $zip) {
    Remove-Item -LiteralPath $zip -Force
}
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zip -CompressionLevel Optimal

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash
Write-Host ""
Write-Host "Build complete:"
Write-Host "  Executable: $exe"
Write-Host "  Archive:    $zip"
Write-Host "  SHA256:     $hash"
