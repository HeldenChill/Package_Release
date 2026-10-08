#Requires -Version 5.1
<#
.SYNOPSIS
  Compares Unity .cs files under asmdef roots with <Compile Include> entries in generated *.csproj files.

.DESCRIPTION
  Detects IDE-only errors: scripts on disk missing from csproj, or stale csproj entries for deleted files.
  Does not modify csproj by default (Unity regenerates those files).

.PARAMETER ProjectRoot
  Repository root containing *.csproj and Assets/ (default: current directory).

.PARAMETER Csproj
  Check only this csproj file name (e.g. MyGame.csproj). Default: all matching asmdef csprojs.

.PARAMETER Json
  Emit machine-readable summary.

.EXAMPLE
  pwsh -File .claude/skills/pkg-framework-unity-csproj-sync/scripts/unity-csproj-sync-check.ps1

.EXAMPLE
  pwsh -File .claude/skills/pkg-framework-unity-csproj-sync/scripts/unity-csproj-sync-check.ps1 -Csproj MyGame.csproj
#>
param(
    [string]$ProjectRoot = (Get-Location).Path,
    [string]$Csproj = "",
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RelativePath([string]$BasePath, [string]$FullPath) {
    $baseUri = New-Object System.Uri(($BasePath.TrimEnd('\') + '\'))
    $fullUri = New-Object System.Uri($FullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($fullUri).ToString().Replace('/', '\'))
}

function Normalize-ProjectPath([string]$Path) {
    return ($Path -replace '/', '\').Trim().ToLowerInvariant()
}

function Get-AsmdefName([string]$AsmdefPath) {
    $json = Get-Content -LiteralPath $AsmdefPath -Raw | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($json.name)) {
        throw "asmdef missing name: $AsmdefPath"
    }
    return [string]$json.name
}

function Get-NestedAsmdefRoots([string]$RootDir, [string]$SelfAsmdefPath) {
    $selfFull = (Resolve-Path -LiteralPath $SelfAsmdefPath).Path
    $nested = @()
    Get-ChildItem -LiteralPath $RootDir -Filter "*.asmdef" -Recurse -File | ForEach-Object {
        if ($_.FullName -eq $selfFull) { return }
        $nested += $_.Directory.FullName
    }
    return $nested | Sort-Object { $_.Length } -Descending
}

function Test-UnderExcludedRoot([string]$FilePath, [string[]]$ExcludedRoots) {
    foreach ($ex in $ExcludedRoots) {
        if ($FilePath.StartsWith($ex, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Get-AsmdefSourceFiles([string]$AsmdefPath) {
    $rootDir = (Resolve-Path -LiteralPath (Split-Path -Parent $AsmdefPath)).Path
    $excluded = Get-NestedAsmdefRoots -RootDir $rootDir -SelfAsmdefPath $AsmdefPath
    $files = @()
    Get-ChildItem -LiteralPath $rootDir -Filter "*.cs" -Recurse -File | ForEach-Object {
        if (Test-UnderExcludedRoot -FilePath $_.FullName -ExcludedRoots $excluded) { return }
        $rel = Get-RelativePath -BasePath $ProjectRoot -FullPath $_.FullName
        $files += (Normalize-ProjectPath $rel)
    }
    return $files | Sort-Object -Unique
}

function Get-CsprojCompileIncludes([string]$CsprojPath) {
    [xml]$xml = Get-Content -LiteralPath $CsprojPath
    $includes = @()
    foreach ($node in $xml.SelectNodes('//*[local-name()="Compile"]')) {
        $attr = $node.Attributes['Include']
        if ($null -eq $attr -or [string]::IsNullOrWhiteSpace($attr.Value)) { continue }
        $includes += (Normalize-ProjectPath $attr.Value)
    }
    return $includes | Sort-Object -Unique
}

$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$assetsDir = Join-Path $ProjectRoot "Assets"
if (-not (Test-Path -LiteralPath $assetsDir)) {
    throw "Assets folder not found under: $ProjectRoot"
}

$asmdefFiles = Get-ChildItem -LiteralPath $assetsDir -Filter "*.asmdef" -Recurse -File
$reports = @()

foreach ($asmdef in $asmdefFiles) {
    $assemblyName = Get-AsmdefName -AsmdefPath $asmdef.FullName
    $csprojName = "$assemblyName.csproj"
    if ($Csproj -and ($Csproj -ne $csprojName)) { continue }

    $csprojPath = Join-Path $ProjectRoot $csprojName
    if (-not (Test-Path -LiteralPath $csprojPath)) { continue }

    $onDisk = @(Get-AsmdefSourceFiles -AsmdefPath $asmdef.FullName)
    $inCsproj = @(Get-CsprojCompileIncludes -CsprojPath $csprojPath)

    $missing = @($onDisk | Where-Object { $_ -notin $inCsproj })
    $stale = @($inCsproj | Where-Object { $_ -notin $onDisk })

    $reports += [PSCustomObject]@{
        Assembly   = $assemblyName
        Csproj     = $csprojName
        Asmdef     = Get-RelativePath -BasePath $ProjectRoot -FullPath $asmdef.FullName
        OnDisk     = $onDisk.Count
        InCsproj   = $inCsproj.Count
        Missing    = $missing
        Stale      = $stale
        InSync     = ($missing.Count -eq 0 -and $stale.Count -eq 0)
    }
}

if ($Json) {
    $reports | ConvertTo-Json -Depth 6
    exit 0
}

$anyOutOfSync = $false
foreach ($r in $reports) {
    if ($r.InSync) {
        Write-Host "[OK] $($r.Assembly) ($($r.OnDisk) scripts)" -ForegroundColor Green
        continue
    }
    $anyOutOfSync = $true
    Write-Host "[OUT OF SYNC] $($r.Assembly) -> $($r.Csproj)" -ForegroundColor Yellow
    Write-Host "  asmdef: $($r.Asmdef)"
    Write-Host "  on disk: $($r.OnDisk)  in csproj: $($r.InCsproj)"
    if ($r.Missing.Count -gt 0) {
        Write-Host "  Missing from csproj ($($r.Missing.Count)):" -ForegroundColor Red
        $r.Missing | ForEach-Object { Write-Host "    + $_" }
    }
    if ($r.Stale.Count -gt 0) {
        Write-Host "  Stale in csproj ($($r.Stale.Count)):" -ForegroundColor DarkYellow
        $r.Stale | ForEach-Object { Write-Host "    - $_" }
    }
    Write-Host ""
}

if (-not $anyOutOfSync) {
    Write-Host "All checked asmdef/csproj pairs are in sync." -ForegroundColor Green
    exit 0
}

Write-Host "Sync: open Unity Editor -> Edit -> Preferences -> External Tools -> Regenerate project files"
Write-Host "      (or Assets -> Refresh; or the project's own IDE-sync menu if it has one)"
Write-Host "Then in Cursor: restart C# / OmniSharp language server if squiggles remain."
exit 1
