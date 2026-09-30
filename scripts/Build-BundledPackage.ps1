param(
    [string]$FfmpegDirectory = (Join-Path $PSScriptRoot '..\artifacts\ffmpeg-win-x64'),
    [string]$AppArchive = (Join-Path $PSScriptRoot '..\publish\PecaOneConnect-v1.0.0-win-x64.zip')
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$publishedAppHash = '6ffd7c4856cd3614d121b6a2c9697034c5cdab34d5ca37562f9dc3b425b50075'
if ((Get-FileHash -LiteralPath $AppArchive -Algorithm SHA256).Hash -ne $publishedAppHash) {
    throw 'The original v1.0.0 application archive does not match the published release.'
}
foreach ($required in @('ffmpeg.exe', 'ffprobe.exe', 'LICENSE.txt', 'BUILDINFO.txt', 'SHA256SUMS.txt', 'README.md',
    'licenses\x264-COPYING.txt', 'licenses\COPYING.GPLv3', 'licenses\mingw-w64-common-copyright.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $FfmpegDirectory $required) -PathType Leaf)) {
        throw ('FFmpeg package is incomplete: ' + $required)
    }
}
foreach ($line in Get-Content -LiteralPath (Join-Path $FfmpegDirectory 'SHA256SUMS.txt')) {
    if ($line -notmatch '^([0-9a-f]{64})  (ffmpeg\.exe|ffprobe\.exe)$') { throw 'Invalid FFmpeg checksum manifest.' }
    $hash = $Matches[1]
    $file = Join-Path $FfmpegDirectory $Matches[2]
    if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $hash) { throw 'FFmpeg checksum mismatch.' }
}
$sourceArchive = Join-Path $repo 'artifacts\FFmpeg-9.0.2-x264-source.zip'
if (-not (Test-Path -LiteralPath $sourceArchive -PathType Leaf)) { throw 'Corresponding FFmpeg source is missing.' }
$staging = Join-Path $repo ('publish\portable-staging-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory([IO.Path]::GetFullPath($AppArchive), $staging)
$installer = Join-Path $staging 'Install-FFmpeg.ps1'
if (Test-Path -LiteralPath $installer -PathType Leaf) { Remove-Item -LiteralPath $installer }
Copy-Item -LiteralPath $FfmpegDirectory -Destination (Join-Path $staging 'ffmpeg') -Recurse
foreach ($document in @('README.md', 'THIRD_PARTY_NOTICES.md', 'docs\FIRST_RUN.md', 'docs\DISTRIBUTION.md')) {
    Copy-Item -LiteralPath (Join-Path $repo $document) -Destination (Join-Path $staging (Split-Path $document -Leaf)) -Force
}
$name = 'PecaOneConnect-v1.0.0-portable-win-x64.zip'
$archive = Join-Path $repo ('publish\' + $name)
if (Test-Path -LiteralPath $archive) { throw 'A portable archive already exists; preserve it before rebuilding.' }
[IO.Compression.ZipFile]::CreateFromDirectory($staging, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
Copy-Item -LiteralPath $sourceArchive -Destination (Join-Path $repo 'publish\FFmpeg-9.0.2-x264-source.zip') -Force
$assets = @($name, 'FFmpeg-9.0.2-x264-source.zip')
$checksums = foreach ($asset in $assets) {
    (Get-FileHash -LiteralPath (Join-Path $repo ('publish\' + $asset)) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $asset
}
$checksums | Set-Content -LiteralPath (Join-Path $repo 'publish\SHA256SUMS-portable.txt') -Encoding ascii
Write-Output ('Created ' + $archive)
