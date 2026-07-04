<#
.SYNOPSIS
Verifies the four release zips produced by release.yml (workflow_dispatch or tag run).

.DESCRIPTION
Run after `gh run download <run-id> -D ci-test`:

    .\scripts\Verify-ReleaseArtifacts.ps1 -Dir ci-test

Per zip it checks:
  - .sha256 sidecar matches the actual hash
  - required files present (app exe, updater, xray engine, wintun, geo data)
  - wasdk variants bundle the Windows App SDK runtime (Microsoft.ui.xaml.dll),
    plain variants do not
  - the app exe requests the update asset of its OWN variant (scans for the
    "-wasdk.zip" suffix baked in by the WASDK_SELF_CONTAINED define) — a
    mismatch here would cross-grade users between variants on auto-update
  - PE machine type of every executable matches the RID (x64=8664, arm64=AA64),
    including the bundled engine and the Rust updater

Exits 1 if anything fails. Windows PowerShell 5.1 compatible.
#>
param(
    # Directory containing the downloaded artifacts (searched recursively).
    [string]$Dir = 'ci-test'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$script:failures = 0
function Check([bool]$ok, [string]$what) {
    if ($ok) { Write-Host "  PASS  $what" -ForegroundColor Green }
    else     { Write-Host "  FAIL  $what" -ForegroundColor Red; $script:failures++ }
}

function Get-PeMachine([byte[]]$bytes) {
    $peOffset = [BitConverter]::ToInt32($bytes, 60)
    return '{0:X4}' -f [BitConverter]::ToUInt16($bytes, $peOffset + 4)
}

# Entry names inside the zip use whatever separator Compress-Archive emitted;
# normalize to '/' before comparing.
function Find-Entry($archive, [string]$name) {
    return $archive.Entries | Where-Object {
        $_.FullName.Replace('\', '/') -ieq $name
    } | Select-Object -First 1
}

function Read-EntryBytes($entry) {
    $ms = New-Object System.IO.MemoryStream
    $s = $entry.Open()
    try { $s.CopyTo($ms) } finally { $s.Dispose() }
    return $ms.ToArray()
}

$machineByRid = @{ 'win-x64' = '8664'; 'win-arm64' = 'AA64' }
$requiredEntries = @(
    'XrayUI-dev.exe',
    'XrayUI.Updater.exe',
    'Assets/engine/xray.exe',
    'Assets/engine/wintun.dll',
    'Assets/rules/geoip.dat',
    'Assets/rules/geosite.dat'
)
$peCheckedEntries = @(
    'XrayUI-dev.exe',
    'XrayUI.Updater.exe',
    'Assets/engine/xray.exe',
    'Assets/engine/wintun.dll'
)

foreach ($rid in 'win-x64', 'win-arm64') {
    foreach ($variant in '', '-wasdk') {
        $zipName = "XrayUI-$rid$variant.zip"
        Write-Host "`n== $zipName ==" -ForegroundColor Cyan

        $zip = Get-ChildItem $Dir -Recurse -Filter $zipName | Select-Object -First 1
        if ($null -eq $zip) { Check $false "zip found"; continue }
        Check $true "zip found ($([math]::Round($zip.Length / 1MB, 1)) MB)"

        # sha256 sidecar
        $sha = Get-ChildItem $Dir -Recurse -Filter "$zipName.sha256" | Select-Object -First 1
        if ($null -eq $sha) {
            Check $false ".sha256 found"
        } else {
            $expected = ([IO.File]::ReadAllText($sha.FullName).Trim() -split '\s+')[0]
            $actual = (Get-FileHash $zip.FullName -Algorithm SHA256).Hash
            Check ($actual -ieq $expected) "sha256 matches"
        }

        $archive = [IO.Compression.ZipFile]::OpenRead($zip.FullName)
        try {
            foreach ($name in $requiredEntries) {
                Check ($null -ne (Find-Entry $archive $name)) "contains $name"
            }

            # Variant payload: runtime bundled iff wasdk.
            $hasRuntime = $null -ne (Find-Entry $archive 'Microsoft.ui.xaml.dll')
            $isWasdk = $variant -eq '-wasdk'
            Check ($hasRuntime -eq $isWasdk) $(if ($isWasdk) { 'bundles WinAppSDK runtime' } else { 'does NOT bundle WinAppSDK runtime' })

            # Variant-aware updater channel: the exe must ask for its own asset name.
            $appEntry = Find-Entry $archive 'XrayUI-dev.exe'
            if ($null -ne $appEntry) {
                $appBytes = Read-EntryBytes $appEntry
                $hasSuffix = [Text.Encoding]::Unicode.GetString($appBytes).Contains('-wasdk.zip')
                Check ($hasSuffix -eq $isWasdk) $(if ($isWasdk) { "exe requests -wasdk update asset" } else { "exe requests plain update asset" })

                Check ((Get-PeMachine $appBytes) -eq $machineByRid[$rid]) "XrayUI-dev.exe machine = $($machineByRid[$rid])"
            }

            foreach ($name in $peCheckedEntries | Where-Object { $_ -ne 'XrayUI-dev.exe' }) {
                $entry = Find-Entry $archive $name
                if ($null -ne $entry) {
                    $machine = Get-PeMachine (Read-EntryBytes $entry)
                    Check ($machine -eq $machineByRid[$rid]) "$name machine = $($machineByRid[$rid]) (got $machine)"
                }
            }
        } finally {
            $archive.Dispose()
        }
    }
}

Write-Host ''
if ($script:failures -gt 0) {
    Write-Host "$script:failures check(s) FAILED" -ForegroundColor Red
    exit 1
}
Write-Host 'All checks passed.' -ForegroundColor Green
