<#
.SYNOPSIS
  Builds Notify as MSIX packages (x64 + ARM64) and an .msixbundle for the Microsoft Store.

.DESCRIPTION
  The Store re-signs packages after certification, so the output is unsigned. To install a local
  test build you'd need to sign it yourself (see README "Testing an MSIX locally").

.EXAMPLE
  pwsh packaging/build-msix.ps1 -Version 2.0.1.0 `
       -IdentityName "12345Steven.Notify" -Publisher "CN=ABCDEF12-..." -PublisherDisplayName "Steven Connelly"
#>
param(
    [string]$Version = '2.0.0.0',                       # Store needs four parts, last part 0
    [string]$IdentityName = 'Notify.Dev',               # Partner Center > Product identity > Package/Identity/Name
    [string]$Publisher = 'CN=NotifyDev',                # Partner Center > Product identity > Package/Identity/Publisher
    [string]$PublisherDisplayName = 'Steven Connelly',  # Partner Center > Product identity > PublisherDisplayName
    [string[]]$Arch = @('x64', 'arm64'),
    [string]$OutDir = 'publish/msix'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Set-Location $repo

if ($Version -notmatch '^\d+\.\d+\.\d+\.0$') { throw "Version must look like 2.0.1.0 (the Store needs the last part to be 0): '$Version'" }

# --- makeappx.exe from the Windows SDK BuildTools NuGet package ---
dotnet restore packaging/sdk-tools/SdkTools.csproj --verbosity quiet
if ($LASTEXITCODE) { throw 'Could not restore Microsoft.Windows.SDK.BuildTools' }
$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $HOME '.nuget/packages' }
$makeappx = Get-ChildItem (Join-Path $nugetRoot 'microsoft.windows.sdk.buildtools') -Recurse -Filter makeappx.exe |
    Where-Object { $_.FullName -match '\\x64\\' } | Sort-Object FullName | Select-Object -Last 1
if (-not $makeappx) { throw 'makeappx.exe not found in the BuildTools package' }
Write-Host "Using $($makeappx.FullName)"

$out = Join-Path $repo $OutDir
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
$packages = Join-Path $out 'packages'
New-Item -ItemType Directory -Force $packages | Out-Null
$template = Get-Content (Join-Path $PSScriptRoot 'Package.appxmanifest') -Raw
$semver = ($Version -split '\.')[0..2] -join '.'

foreach ($a in $Arch) {
    $layout = Join-Path $out "layout-$a"
    Write-Host "== Publishing $a"
    dotnet publish Notify/Notify.csproj -c Release -r "win-$a" --self-contained true `
        -p:PublishSingleFile=false -p:DebugType=none -p:Version=$semver -o $layout --verbosity quiet
    if ($LASTEXITCODE) { throw "dotnet publish failed for $a" }

    Copy-Item (Join-Path $PSScriptRoot 'Assets') (Join-Path $layout 'Assets') -Recurse
    Remove-Item (Join-Path $layout 'Assets/StoreListing300.png') -ErrorAction SilentlyContinue  # listing art, not a package asset

    $manifest = $template.Replace('{{IdentityName}}', $IdentityName).Replace('{{Publisher}}', $Publisher). `
        Replace('{{PublisherDisplayName}}', [System.Security.SecurityElement]::Escape($PublisherDisplayName)). `
        Replace('{{Version}}', $Version).Replace('{{Arch}}', $a)
    Set-Content (Join-Path $layout 'AppxManifest.xml') $manifest -Encoding UTF8

    $msix = Join-Path $packages "Notify_$($Version)_$a.msix"
    & $makeappx.FullName pack /d $layout /p $msix /o | Out-Host   # validates the manifest against the schema
    if ($LASTEXITCODE) { throw "makeappx pack failed for $a" }
}

$bundle = Join-Path $out "Notify_$Version.msixbundle"
& $makeappx.FullName bundle /d $packages /p $bundle /bv $Version /o | Out-Host
if ($LASTEXITCODE) { throw 'makeappx bundle failed' }

Get-ChildItem $out -File -Recurse -Include *.msix, *.msixbundle |
    Select-Object Name, @{ n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } } | Format-Table -AutoSize | Out-Host
Write-Host "Store upload file: $bundle"
