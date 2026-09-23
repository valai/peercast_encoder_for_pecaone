$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$localDotnet = Join-Path $repo '.tools\dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { (Get-Command dotnet.exe -ErrorAction Stop).Source }
$env:DOTNET_CLI_HOME = Join-Path $repo '.tools\cli'
$env:NUGET_PACKAGES = Join-Path $repo '.tools\nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:PECAONE_FFMPEG_DIR = Join-Path $repo 'vendor\ffmpeg'
if (-not (Test-Path (Join-Path $repo 'vendor\ffmpeg\ffmpeg.exe'))) {
    & (Join-Path $PSScriptRoot 'prepare-ffmpeg.ps1')
}
Push-Location $repo
try {
    & $dotnet restore src\PecaOneRelay\PecaOneRelay.csproj --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed' }
    & $dotnet run --project src\PecaOneRelay\PecaOneRelay.csproj --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'App exited with an error' }
} finally {
    Pop-Location
}
