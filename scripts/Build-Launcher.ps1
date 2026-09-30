param([string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\launcher'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repo 'native\launcher'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) { throw 'Visual Studio C++ build tools and Windows SDK are required.' }
$installation = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if ($LASTEXITCODE -ne 0 -or -not $installation) { throw 'No Visual Studio C++ toolchain found.' }
$vcvars = Join-Path $installation 'VC\Auxiliary\Build\vcvars64.bat'
$compilerEnvironment = & cmd.exe /d /s /c ('call "' + $vcvars + '" >nul && set')
if ($LASTEXITCODE -ne 0) { throw 'Compiler environment initialization failed.' }
foreach ($line in $compilerEnvironment) {
    if ($line -match '^([^=]+)=(.*)$' -and $Matches[1] -in @('Path', 'INCLUDE', 'LIB', 'LIBPATH')) {
        [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2], 'Process')
    }
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
function Invoke-BuildTool([string]$Tool, [string[]]$Arguments) {
    $output = & $Tool @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Output $_.ToString() }
    if ($exitCode -ne 0) { throw ($Tool + ' failed: ' + (($output | Select-Object -Last 15) -join "`n")) }
}
Push-Location $source
try {
    Invoke-BuildTool 'rc.exe' @('/nologo', '/fo', (Join-Path $OutputDirectory 'launcher.res'), 'launcher.rc')
    Invoke-BuildTool 'cl.exe' @('/nologo', '/O2', '/Oi', '/utf-8', '/W4', '/WX', '/GS', '/Zl', '/TC', '/c', 'launcher.c', ('/Fo' + (Join-Path $OutputDirectory 'launcher.obj')))
    $linkArguments = @('/nologo', '/NODEFAULTLIB', '/ENTRY:LauncherEntry', '/SUBSYSTEM:WINDOWS', '/MACHINE:X64', '/DYNAMICBASE', '/NXCOMPAT', '/MANIFEST:NO',
        ('/OUT:' + (Join-Path $OutputDirectory 'PecaOneConnect.exe')), (Join-Path $OutputDirectory 'launcher.obj'), (Join-Path $OutputDirectory 'launcher.res'), 'kernel32.lib', 'user32.lib')
    Invoke-BuildTool 'link.exe' $linkArguments
    Invoke-BuildTool 'cl.exe' @('/nologo', '/O2', '/Oi', '/utf-8', '/W4', '/WX', '/GS', '/Zl', '/TC', '/c', 'test-child.c', ('/Fo' + (Join-Path $OutputDirectory 'test-child.obj')))
    $linkArguments = @('/nologo', '/NODEFAULTLIB', '/ENTRY:TestChildEntry', '/SUBSYSTEM:WINDOWS', '/MACHINE:X64', '/DYNAMICBASE', '/NXCOMPAT', '/MANIFEST:NO',
        ('/OUT:' + (Join-Path $OutputDirectory 'TestChild.exe')), (Join-Path $OutputDirectory 'test-child.obj'), 'kernel32.lib')
    Invoke-BuildTool 'link.exe' $linkArguments
    $imports = & dumpbin.exe /nologo /imports (Join-Path $OutputDirectory 'PecaOneConnect.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Launcher import inspection failed.' }
    $dlls = @($imports | Where-Object { $_ -match '^\s+([A-Za-z0-9_.-]+\.dll)\s*$' } | ForEach-Object { $_.Trim().ToLowerInvariant() } | Sort-Object -Unique)
    if ($dlls.Count -ne 2 -or $dlls[0] -ne 'kernel32.dll' -or $dlls[1] -ne 'user32.dll') { throw ('Unexpected launcher dependencies: ' + ($dlls -join ', ')) }
    @('Launcher: GPL-3.0-or-later', ('Source commit: ' + (& git -C $repo rev-parse HEAD)), 'Imports: KERNEL32.dll, USER32.dll',
        ('Compiler: ' + (Get-Item -LiteralPath (Get-Command cl.exe).Source).VersionInfo.FileVersion),
        ('Linker: ' + (Get-Item -LiteralPath (Get-Command link.exe).Source).VersionInfo.FileVersion)) |
        Set-Content -LiteralPath (Join-Path $OutputDirectory 'BUILDINFO.txt') -Encoding utf8
    Write-Output ('Launcher ready: ' + (Join-Path $OutputDirectory 'PecaOneConnect.exe'))
} finally { Pop-Location }
