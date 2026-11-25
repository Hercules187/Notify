<#
.SYNOPSIS
  Builds the three download zips for a GitHub release, plus SHA256SUMS.txt:
    Notify-<ver>-win-x64.zip                 self-contained single exe (Intel/AMD)
    Notify-<ver>-win-arm64.zip               self-contained single exe (ARM)
    Notify-<ver>-win-x64-needs-dotnet10.zip  small exe, needs the .NET 10 Desktop Runtime
#>
param(
    [string]$Version = '2.0.0',
    [string]$OutDir = 'publish/release'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Set-Location $repo
$out = Join-Path $repo $OutDir
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $out | Out-Null

$variants = @(
    @{ Rid = 'win-x64';   SelfContained = $true;  Zip = "Notify-$Version-win-x64.zip" },
    @{ Rid = 'win-arm64'; SelfContained = $true;  Zip = "Notify-$Version-win-arm64.zip" },
    @{ Rid = 'win-x64';   SelfContained = $false; Zip = "Notify-$Version-win-x64-needs-dotnet10.zip" }
)
foreach ($v in $variants) {
    $stage = Join-Path $out ("stage-" + [IO.Path]::GetFileNameWithoutExtension($v.Zip))
    $sc = if ($v.SelfContained) { 'true' } else { 'false' }
    Write-Host "== $($v.Zip)"
    dotnet publish Notify/Notify.csproj -c Release -r $v.Rid --self-contained $sc `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=$sc `
        -p:DebugType=none -p:Version=$Version -o $stage --verbosity quiet
    if ($LASTEXITCODE) { throw "dotnet publish failed for $($v.Zip)" }
    Compress-Archive -Path (Join-Path $stage 'Notify.exe'), (Join-Path $stage 'License.txt') -DestinationPath (Join-Path $out $v.Zip) -Force
    Remove-Item $stage -Recurse -Force
}

$sums = Get-ChildItem $out -Filter *.zip | Sort-Object Name | ForEach-Object {
    "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(), $_.Name
}
[IO.File]::WriteAllText((Join-Path $out 'SHA256SUMS.txt'), (($sums -join "`n") + "`n"))
Get-ChildItem $out | Select-Object Name, @{ n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } } | Format-Table -AutoSize | Out-Host
