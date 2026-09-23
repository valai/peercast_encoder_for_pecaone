param([switch]$Force)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$cache = Join-Path $repo '.tools'
$archive = Join-Path $cache 'ffmpeg-9.0.2.zip'
$unpacked = Join-Path $cache 'ffmpeg-package'
$target = Join-Path $repo 'vendor\ffmpeg'
$url = 'https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-09-19-13-11/ffmpeg-n9.0.2-win64-gpl-9.0.zip'
$sha256 = '44083538105B4E64D439F9E67BD875BD264B4271239C808B2ACEA09773AD1AA3'
New-Item -ItemType Directory -Force $cache, $unpacked, $target | Out-Null
if ($Force -or -not (Test-Path $archive)) {
    Invoke-WebRequest -Uri $url -OutFile $archive -TimeoutSec 600
}
if ((Get-FileHash $archive -Algorithm SHA256).Hash -ne $sha256) {
    throw 'FFmpeg アーカイブの SHA-256 が一致しません。'
}
$source = Join-Path $unpacked 'ffmpeg-n9.0.2-win64-gpl-9.0'
if (-not (Test-Path (Join-Path $source 'bin\ffmpeg.exe'))) {
    tar -xf $archive -C $unpacked
    if ($LASTEXITCODE -ne 0) { throw 'FFmpeg の展開に失敗しました。' }
}
Copy-Item (Join-Path $source 'bin\ffmpeg.exe') (Join-Path $target 'ffmpeg.exe') -Force
Copy-Item (Join-Path $source 'bin\ffprobe.exe') (Join-Path $target 'ffprobe.exe') -Force
Copy-Item (Join-Path $source 'LICENSE.txt') (Join-Path $target 'LICENSE.txt') -Force
Write-Output "FFmpeg 9.0.2 準備完了: $target"
