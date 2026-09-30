param([switch]$TestOnly, [switch]$ForRelease)
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
    & $dotnet restore PecaOneRelay.slnx --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed' }
    & $dotnet test PecaOneRelay.slnx -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
    if (-not $TestOnly) {
        $packageDirectory = if ($ForRelease) {
            Join-Path $repo ('publish\release-staging-' + [Guid]::NewGuid().ToString('N'))
        } else { Join-Path $repo 'publish\win-x64' }
        $includeFfmpeg = if ($ForRelease) { 'false' } else { 'true' }
        & $dotnet restore src\PecaOneRelay\PecaOneRelay.csproj -r win-x64 --configfile NuGet.Config
        if ($LASTEXITCODE -ne 0) { throw 'Publish restore failed' }
        & $dotnet publish src\PecaOneRelay\PecaOneRelay.csproj -c Release -r win-x64 --self-contained true --no-restore -p:IncludeFfmpeg=$includeFfmpeg -o $packageDirectory
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
        $archiveName = if ($ForRelease) { 'PecaOneConnect-v1.0.0-win-x64.zip' } else { 'PecaOneRelay-win-x64.zip' }
        $archive = Join-Path $repo ('publish\' + $archiveName)
        if ($ForRelease) {
            Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-FFmpeg.ps1') -Destination $packageDirectory
            Copy-Item -LiteralPath (Join-Path $repo 'docs\FIRST_RUN.md') -Destination $packageDirectory
            if (Test-Path -LiteralPath (Join-Path $packageDirectory 'ffmpeg')) { throw 'Public package must not bundle FFmpeg.' }
        }
        foreach ($required in @('PecaOneRelay.exe', 'coreclr.dll', 'LICENSE', 'THIRD_PARTY_NOTICES.md',
            'licenses\QRCoder-LICENSE.txt', 'licenses\NET-Runtime-LICENSE.txt',
            'licenses\NET-Runtime-THIRD-PARTY-NOTICES.txt', 'licenses\Windows-Desktop-LICENSE.txt',
            'licenses\ASP-NET-Core-LICENSE.txt', 'licenses\ASP-NET-Core-THIRD-PARTY-NOTICES.txt')) {
            if (-not (Test-Path -LiteralPath (Join-Path $packageDirectory $required) -PathType Leaf)) { throw ('Missing package file: ' + $required) }
        }
        if (Test-Path $archive) { Remove-Item -LiteralPath $archive -Force }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $packageDirectory,
            $archive,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false
        )
        "$(Get-FileHash $archive -Algorithm SHA256 | Select-Object -ExpandProperty Hash)  $archiveName" |
            Set-Content -LiteralPath ($archive + '.sha256') -Encoding ascii
    }
} finally {
    Pop-Location
}
