param([string]$LauncherDirectory = (Join-Path $PSScriptRoot '..\artifacts\launcher'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$testRoot = Join-Path $repo ('artifacts\launcher-tests-' + [Guid]::NewGuid().ToString('N'))
$unrelatedDirectory = Join-Path $testRoot '別の 作業フォルダ'
New-Item -ItemType Directory -Path $unrelatedDirectory -Force | Out-Null
$previousReport = $env:PECAONE_LAUNCHER_TEST_REPORT
try {
    foreach ($folder in @('日本語の 展開先', 'folder with spaces')) {
        $package = Join-Path $testRoot $folder
        $app = Join-Path $package 'app'
        New-Item -ItemType Directory -Path $app -Force | Out-Null
        $launcher = Join-Path $package 'ぺかわん コネクト.exe'
        $child = Join-Path $app 'PecaOneRelay.exe'
        Copy-Item -LiteralPath (Join-Path $LauncherDirectory 'PecaOneConnect.exe') -Destination $launcher
        Copy-Item -LiteralPath (Join-Path $LauncherDirectory 'TestChild.exe') -Destination $child
        $report = Join-Path $package 'child-report.txt'
        $env:PECAONE_LAUNCHER_TEST_REPORT = $report
        $process = Start-Process -FilePath $launcher -WorkingDirectory $unrelatedDirectory -WindowStyle Hidden -PassThru
        if (-not $process.WaitForExit(10000) -or $process.ExitCode -ne 0) { throw 'Launcher did not exit successfully.' }
        $deadline = [DateTime]::UtcNow.AddSeconds(10)
        $lines = @()
        do {
            if (Test-Path -LiteralPath $report) { $lines = @(Get-Content -LiteralPath $report -Encoding Unicode) }
            if ($lines.Count -eq 4 -and $lines[3] -eq 'OK') { break }
            Start-Sleep -Milliseconds 100
        } while ([DateTime]::UtcNow -lt $deadline)
        if ($lines.Count -ne 4 -or $lines[3] -ne 'OK') { throw 'The child did not produce a complete report.' }
        if ($lines[0] -ne $child -or $lines[1] -ne $app -or $lines[2] -ne ('"' + $child + '"')) { throw ('Incorrect child path, working directory, or quoting: ' + ($lines -join ' | ')) }
        Write-Output ('Launcher path test passed: ' + $folder)
    }
} finally { $env:PECAONE_LAUNCHER_TEST_REPORT = $previousReport }
