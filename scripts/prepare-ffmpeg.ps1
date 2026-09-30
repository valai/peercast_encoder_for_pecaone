param([switch]$Force)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$target = Join-Path $repo 'vendor\ffmpeg'
& (Join-Path $PSScriptRoot 'Install-FFmpeg.ps1') -TargetDirectory $target -Force:$Force
