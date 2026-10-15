param(
    [string]$Version = '1.1.0',
    [string]$AppArchive = (Join-Path $PSScriptRoot '..\publish\PecaOneConnect-v1.0.0-portable-r3-win-x64.zip'),
    [string]$UpdatedAppDirectory = (Join-Path $PSScriptRoot '..\artifacts\updated-app'),
    [string]$LauncherDirectory,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\publish')
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$publishedHash = '79492ffe4dcfb43209b38d54388e05a64c150e0a84de9364f7c57924e425ddad'
$archiveName = 'PecaOneConnect-v' + $Version + '-portable-win-x64.zip'
$archive = Join-Path $OutputDirectory $archiveName

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must use the numeric major.minor.patch format.' }
if ((Get-FileHash -LiteralPath $AppArchive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $publishedHash) {
    throw 'The published v1.0.0 portable archive does not match its verified SHA-256.'
}
if (Test-Path -LiteralPath $archive) { throw ('The release archive already exists; preserve it before rebuilding: ' + $archive) }

$updatedFiles = @('PecaOneRelay.exe', 'PecaOneRelay.dll', 'PecaOneRelay.pdb', 'PecaOneRelay.deps.json', 'PecaOneRelay.runtimeconfig.json')
foreach ($file in $updatedFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $UpdatedAppDirectory $file) -PathType Leaf)) {
        throw ('Updated application file missing: ' + $file)
    }
}

$stage = Join-Path $OutputDirectory ('release-staging-v' + $Version + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory([IO.Path]::GetFullPath($AppArchive), $stage)

$app = Join-Path $stage 'app'
if (-not (Test-Path -LiteralPath $app -PathType Container)) { throw 'The verified portable package has no app directory.' }
foreach ($file in $updatedFiles) {
    Copy-Item -LiteralPath (Join-Path $UpdatedAppDirectory $file) -Destination (Join-Path $app $file) -Force
}

foreach ($file in Get-ChildItem -LiteralPath $UpdatedAppDirectory -Recurse -File | Where-Object { $_.Extension -in @('.exe', '.dll') -and $_.Name -notin @('PecaOneRelay.exe', 'PecaOneRelay.dll') }) {
    $relative = [IO.Path]::GetRelativePath([IO.Path]::GetFullPath($UpdatedAppDirectory), $file.FullName)
    $original = Join-Path $app $relative
    if (-not (Test-Path -LiteralPath $original -PathType Leaf) -or
        (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash) {
        throw ('A runtime dependency changed; prepare a dependency update instead of reusing the v1.0.0 package: ' + $relative)
    }
}

if ($LauncherDirectory) {
    foreach ($file in @('PecaOneConnect.exe', 'BUILDINFO.txt')) {
        $source = Join-Path $LauncherDirectory $file
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw ('Launcher output missing: ' + $file) }
    }
    Copy-Item -LiteralPath (Join-Path $LauncherDirectory 'PecaOneConnect.exe') -Destination (Join-Path $stage 'ぺかわん コネクト.exe') -Force
    Copy-Item -LiteralPath (Join-Path $LauncherDirectory 'BUILDINFO.txt') -Destination (Join-Path $app 'LAUNCHER-BUILDINFO.txt') -Force
}

foreach ($document in @('README.md', 'THIRD_PARTY_NOTICES.md', 'docs\FIRST_RUN.md', 'docs\DISTRIBUTION.md')) {
    Copy-Item -LiteralPath (Join-Path $repo $document) -Destination (Join-Path $app (Split-Path $document -Leaf)) -Force
}
$guide = Get-Content -LiteralPath (Join-Path $repo 'docs\START_HERE.txt') -Raw
[IO.File]::WriteAllText((Join-Path $stage 'はじめに.txt'), $guide, [Text.UTF8Encoding]::new($true))

$entries = @(Get-ChildItem -LiteralPath $stage | ForEach-Object Name | Sort-Object)
if (($entries -join '|') -ne ((@('app', 'はじめに.txt', 'ぺかわん コネクト.exe') | Sort-Object) -join '|')) {
    throw 'The package root is not the expected three entries.'
}
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
Write-Output ('Created ' + $archive)
