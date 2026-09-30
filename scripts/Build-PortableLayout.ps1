param(
    [string]$AppArchive = (Join-Path $PSScriptRoot '..\publish\PecaOneConnect-v1.0.0-portable-win-x64.zip'),
    [string]$LauncherDirectory = (Join-Path $PSScriptRoot '..\artifacts\launcher'),
    [string]$UpdatedAppDirectory
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$publishedHash = '6e2e78e3acdecc193828cf80b3b47ade3d8ae7976f032ecc46833a32b70d71ec'
if ((Get-FileHash -LiteralPath $AppArchive -Algorithm SHA256).Hash -ne $publishedHash) { throw 'The published portable archive does not match its checksum.' }
$launcher = Join-Path $LauncherDirectory 'PecaOneConnect.exe'
if ((Get-Item -LiteralPath $launcher).VersionInfo.ProductName -ne 'ぺかわん コネクト') { throw 'The launcher product name is incorrect.' }
$revision = if ($UpdatedAppDirectory) { 'r3' } else { 'r2' }
$archive = Join-Path $repo ('publish\PecaOneConnect-v1.0.0-portable-' + $revision + '-win-x64.zip')
if (Test-Path -LiteralPath $archive) { throw 'The archive already exists; preserve it before rebuilding.' }
$staging = Join-Path $repo ('publish\layout-staging-' + [Guid]::NewGuid().ToString('N'))
$app = Join-Path $staging 'app'
New-Item -ItemType Directory -Path $app -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory([IO.Path]::GetFullPath($AppArchive), $app)
if ($UpdatedAppDirectory) {
    $updatedFiles = @('PecaOneRelay.exe', 'PecaOneRelay.dll', 'PecaOneRelay.pdb', 'PecaOneRelay.deps.json', 'PecaOneRelay.runtimeconfig.json')
    foreach ($file in $updatedFiles) {
        $updated = Join-Path $UpdatedAppDirectory $file
        if (-not (Test-Path -LiteralPath $updated -PathType Leaf)) { throw ('Updated application file missing: ' + $file) }
        Copy-Item -LiteralPath $updated -Destination (Join-Path $app $file) -Force
    }
    foreach ($file in Get-ChildItem -LiteralPath $UpdatedAppDirectory -Recurse -File | Where-Object { $_.Extension -in @('.exe', '.dll') -and $_.Name -notin $updatedFiles }) {
        $relative = [IO.Path]::GetRelativePath([IO.Path]::GetFullPath($UpdatedAppDirectory), $file.FullName)
        $original = Join-Path $app $relative
        if (-not (Test-Path -LiteralPath $original -PathType Leaf) -or
            (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash) {
            throw ('A dependency changed; prepare a new release instead of an icon revision: ' + $relative)
        }
    }
}
Copy-Item -LiteralPath $launcher -Destination (Join-Path $staging 'ぺかわん コネクト.exe')
Copy-Item -LiteralPath (Join-Path $LauncherDirectory 'BUILDINFO.txt') -Destination (Join-Path $app 'LAUNCHER-BUILDINFO.txt')
$guide = Get-Content -LiteralPath (Join-Path $repo 'docs\START_HERE.txt') -Raw
[IO.File]::WriteAllText((Join-Path $staging 'はじめに.txt'), $guide, [Text.UTF8Encoding]::new($true))
foreach ($document in @('README.md', 'THIRD_PARTY_NOTICES.md', 'docs\FIRST_RUN.md', 'docs\DISTRIBUTION.md')) {
    Copy-Item -LiteralPath (Join-Path $repo $document) -Destination (Join-Path $app (Split-Path $document -Leaf)) -Force
}
$entries = @(Get-ChildItem -LiteralPath $staging | ForEach-Object Name | Sort-Object)
if (($entries -join '|') -ne ((@('app', 'はじめに.txt', 'ぺかわん コネクト.exe') | Sort-Object) -join '|')) { throw 'Unexpected root entries.' }
[IO.Compression.ZipFile]::CreateFromDirectory($staging, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
Write-Output ('Created ' + $archive)
