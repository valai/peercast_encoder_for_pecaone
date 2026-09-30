param(
    [string]$TargetDirectory = (Join-Path $PSScriptRoot 'ffmpeg'),
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
$requiredFiles = @('ffmpeg.exe', 'ffprobe.exe', 'LICENSE.txt')
if (-not $Force -and @($requiredFiles | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $TargetDirectory $_) -PathType Leaf)
}).Count -eq 0) {
    Write-Output 'FFmpeg は準備済みです。'
    return
}

$headers = @{ Accept = 'application/vnd.github+json'; 'User-Agent' = 'PecaOneConnect-FFmpeg-Setup' }
$release = Invoke-RestMethod -Uri 'https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/tags/latest' -Headers $headers -TimeoutSec 30
$assets = @($release.assets | Where-Object { $_.name -eq 'ffmpeg-n9.0-latest-win64-gpl-9.0.zip' })
if ($assets.Count -ne 1) { throw 'FFmpeg 9.0 系の配布ファイルが見つかりません。FIRST_RUN.md の手動導入手順を確認してください。' }
$asset = $assets[0]
$downloadUri = [Uri]$asset.browser_download_url
if ($downloadUri.Scheme -ne 'https' -or $downloadUri.Host -ne 'github.com' -or
    -not $downloadUri.AbsolutePath.StartsWith('/BtbN/FFmpeg-Builds/releases/download/')) {
    throw 'FFmpeg の取得先が配布元の GitHub ではありません。'
}
if ($asset.digest -notmatch '^sha256:([0-9a-fA-F]{64})$') {
    throw '配布元の SHA-256 が取得できません。検証できないファイルは導入しません。'
}
$expectedHash = $Matches[1]
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/')
$scratchDirectory = [IO.Path]::GetFullPath((Join-Path $temporaryRoot ('PecaOneConnect-ffmpeg-' + [Guid]::NewGuid().ToString('N'))))
if ([IO.Directory]::GetParent($scratchDirectory).FullName -ne $temporaryRoot) { throw '作業フォルダのパスが不正です。' }
New-Item -ItemType Directory -Path $scratchDirectory | Out-Null
try {
    $archive = Join-Path $scratchDirectory 'ffmpeg.zip'
    Write-Output '配布元から FFmpeg を取得しています。'
    Invoke-WebRequest -Uri $downloadUri.AbsoluteUri -OutFile $archive -TimeoutSec 600
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expectedHash) {
        throw 'FFmpeg の SHA-256 が一致しません。配布元の更新中の場合は、もう一度お試しください。'
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $unpacked = Join-Path $scratchDirectory 'unpacked'
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, $unpacked)
    $packages = @(Get-ChildItem -LiteralPath $unpacked -Directory | Where-Object {
        Test-Path -LiteralPath (Join-Path $_.FullName 'bin\ffmpeg.exe') -PathType Leaf
    })
    if ($packages.Count -ne 1) { throw 'FFmpeg のアーカイブ構成を確認できません。' }
    $package = $packages[0].FullName
    $filesToCopy = @(
        @{ Source = (Join-Path $package 'bin\ffmpeg.exe'); Name = 'ffmpeg.exe' },
        @{ Source = (Join-Path $package 'bin\ffprobe.exe'); Name = 'ffprobe.exe' },
        @{ Source = (Join-Path $package 'LICENSE.txt'); Name = 'LICENSE.txt' }
    )
    foreach ($file in $filesToCopy) {
        if (-not (Test-Path -LiteralPath $file.Source -PathType Leaf)) { throw ('FFmpeg の必要ファイルがありません: ' + $file.Name) }
    }
    New-Item -ItemType Directory -Path $TargetDirectory -Force | Out-Null
    foreach ($file in $filesToCopy) {
        Copy-Item -LiteralPath $file.Source -Destination (Join-Path $TargetDirectory $file.Name) -Force
    }
    @{ provider = 'BtbN/FFmpeg-Builds'; asset = $asset.name; sha256 = $expectedHash;
       downloadedAt = [DateTimeOffset]::UtcNow.ToString('o'); downloadUrl = $downloadUri.AbsoluteUri } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $TargetDirectory 'ORIGIN.json') -Encoding utf8
    Write-Output ('FFmpeg の準備が完了しました: ' + [IO.Path]::GetFullPath($TargetDirectory))
} finally {
    if (Test-Path -LiteralPath $scratchDirectory) {
        $resolvedScratch = (Resolve-Path -LiteralPath $scratchDirectory).Path
        $scratchItem = Get-Item -LiteralPath $scratchDirectory
        if ($resolvedScratch -ne $scratchDirectory -or
            $scratchItem.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)) {
            throw '作業フォルダのパスが変わったため、自動削除を中止しました。'
        }
        Remove-Item -LiteralPath $scratchDirectory -Recurse -Force
    }
}
