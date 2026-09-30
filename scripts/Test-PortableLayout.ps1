param(
    [string]$Archive = (Join-Path $PSScriptRoot '..\publish\PecaOneConnect-v1.0.0-portable-r2-win-x64.zip'),
    [string]$OriginalArchive = (Join-Path $PSScriptRoot '..\publish\PecaOneConnect-v1.0.0-portable-win-x64.zip'),
    [switch]$LaunchApp
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$validation = Join-Path $repo ('artifacts\配布の 起動確認-' + [Guid]::NewGuid().ToString('N'))
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory([IO.Path]::GetFullPath($Archive), $validation)
$rootEntries = @(Get-ChildItem -LiteralPath $validation | ForEach-Object Name | Sort-Object)
if (($rootEntries -join '|') -ne ((@('app', 'はじめに.txt', 'ぺかわん コネクト.exe') | Sort-Object) -join '|')) { throw 'The package root is not the expected three entries.' }
$original = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($OriginalArchive))
$count = 0
try {
    foreach ($entry in $original.Entries | Where-Object { $_.FullName -match '\.(exe|dll)$' }) {
        $stream = $entry.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $expected = [Convert]::ToHexString($sha.ComputeHash($stream)) } finally { $stream.Dispose(); $sha.Dispose() }
        $file = Join-Path (Join-Path $validation 'app') $entry.FullName
        if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $expected) { throw ('A published application binary changed: ' + $entry.FullName) }
        $count++
    }
} finally { $original.Dispose() }
if ($count -lt 100) { throw 'Too few original binaries were inspected.' }
foreach ($file in @('LICENSE', 'THIRD_PARTY_NOTICES.md', 'FIRST_RUN.md', 'LAUNCHER-BUILDINFO.txt', 'licenses\QRCoder-LICENSE.txt',
    'ffmpeg\LICENSE.txt', 'ffmpeg\BUILDINFO.txt', 'ffmpeg\licenses\COPYING.GPLv3', 'ffmpeg\ffprobe.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $validation ('app\' + $file)) -PathType Leaf)) { throw ('Required package file missing: ' + $file) }
}
$version = & (Join-Path $validation 'app\ffmpeg\ffmpeg.exe') -version
if ($LASTEXITCODE -ne 0 -or $version[0] -notmatch '^ffmpeg version 9\.0\.2') { throw 'The relocated FFmpeg did not run.' }
Write-Output ('Layout and FFmpeg checks passed; ' + $count + ' original EXE/DLL files are unchanged.')
if ($LaunchApp) {
    $child = $null
    try {
        $expectedPath = Join-Path $validation 'app\PecaOneRelay.exe'
        $launcher = Start-Process -FilePath (Join-Path $validation 'ぺかわん コネクト.exe') -WorkingDirectory $repo -WindowStyle Hidden -PassThru
        if (-not $launcher.WaitForExit(10000) -or $launcher.ExitCode -ne 0) { throw 'The packaged launcher failed.' }
        $deadline = [DateTime]::UtcNow.AddSeconds(30)
        do {
            $child = Get-Process -Name PecaOneRelay -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $expectedPath } | Select-Object -First 1
            if ($child -and $child.MainWindowHandle -ne [IntPtr]::Zero) { break }
            Start-Sleep -Milliseconds 250
        } while ([DateTime]::UtcNow -lt $deadline)
        if (-not $child -or $child.HasExited -or $child.MainWindowHandle -eq [IntPtr]::Zero) { throw 'The packaged application did not initialize its window.' }
        Write-Output 'The packaged WPF application started successfully from the root launcher.'
    } finally {
        if ($child -and -not $child.HasExited) { Stop-Process -Id $child.Id -Force; $child.WaitForExit(10000) | Out-Null }
    }
}
Write-Output ('Validated extraction: ' + $validation)
