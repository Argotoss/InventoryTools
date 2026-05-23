param(
    [string]$RimWorldPath = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld",
    [string]$HarmonyPath = "C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies\0Harmony.dll"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$managedPath = Join-Path $RimWorldPath "RimWorldWin64_Data\Managed"
$outputPath = Join-Path $root "1.6\Assemblies"
$outputFile = Join-Path $outputPath "InventoryTools.dll"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (!(Test-Path $csc)) {
    throw "C# compiler not found at $csc"
}

if (!(Test-Path $managedPath)) {
    throw "RimWorld managed assemblies not found at $managedPath"
}

if (!(Test-Path $HarmonyPath)) {
    throw "Harmony assembly not found at $HarmonyPath"
}

New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$sources = Get-ChildItem -Path (Join-Path $PSScriptRoot "InventoryTools") -Filter "*.cs" -Recurse | ForEach-Object { $_.FullName }

& $csc `
    /noconfig `
    /nostdlib+ `
    /target:library `
    /optimize+ `
    /debug- `
    /out:$outputFile `
    /reference:"$HarmonyPath" `
    /reference:"$(Join-Path $managedPath 'mscorlib.dll')" `
    /reference:"$(Join-Path $managedPath 'netstandard.dll')" `
    /reference:"$(Join-Path $managedPath 'System.dll')" `
    /reference:"$(Join-Path $managedPath 'System.Core.dll')" `
    /reference:"$(Join-Path $managedPath 'Assembly-CSharp.dll')" `
    /reference:"$(Join-Path $managedPath 'UnityEngine.dll')" `
    /reference:"$(Join-Path $managedPath 'UnityEngine.CoreModule.dll')" `
    $sources

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Built $outputFile"
