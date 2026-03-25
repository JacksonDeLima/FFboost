param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$uiProject = Join-Path $root "FFBoost.UI\FFBoost.UI.csproj"
$setupProject = Join-Path $root "FFBoost.Setup\FFBoost.Setup.csproj"
$publishDir = Join-Path $root "dist\publish"
$installerDir = Join-Path $root "dist\installer"
$payloadDir = Join-Path $root "Installer\Payload"

if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

if (Test-Path $installerDir) {
    Remove-Item -Recurse -Force $installerDir
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $installerDir | Out-Null
New-Item -ItemType Directory -Force -Path $payloadDir | Out-Null

dotnet publish $uiProject -c $Configuration -o $publishDir

$filesToRemove = @(
    (Join-Path $publishDir "FFBoost.pdb"),
    (Join-Path $publishDir "FFBoost.Core.pdb"),
    (Join-Path $publishDir "ffboost.log")
)

foreach ($file in $filesToRemove) {
    if (Test-Path $file) {
        Remove-Item -Force $file
    }
}

Copy-Item -Force (Join-Path $publishDir "FFBoost.exe") (Join-Path $payloadDir "FFBoost.exe")
Copy-Item -Force (Join-Path $publishDir "config.json") (Join-Path $payloadDir "config.json")

dotnet publish $setupProject -c $Configuration -o $installerDir

$installerPdb = Join-Path $installerDir "FFBoost-Setup.pdb"
if (Test-Path $installerPdb) {
    Remove-Item -Force $installerPdb
}
