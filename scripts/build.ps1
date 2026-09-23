param([switch]$TestOnly)
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
        & $dotnet restore src\PecaOneRelay\PecaOneRelay.csproj -r win-x64 --configfile NuGet.Config
        if ($LASTEXITCODE -ne 0) { throw 'Publish restore failed' }
        & $dotnet publish src\PecaOneRelay\PecaOneRelay.csproj -c Release -r win-x64 --self-contained true --no-restore -o publish\win-x64
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
        $archive = Join-Path $repo 'publish\PecaOneRelay-win-x64.zip'
        if (Test-Path $archive) { Remove-Item -LiteralPath $archive -Force }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            (Join-Path $repo 'publish\win-x64'),
            $archive,
            [System.IO.Compression.CompressionLevel]::Optimal,
            $false
        )
        "$(Get-FileHash $archive -Algorithm SHA256 | Select-Object -ExpandProperty Hash)  PecaOneRelay-win-x64.zip" |
            Set-Content (Join-Path $repo 'publish\PecaOneRelay-win-x64.zip.sha256')
    }
} finally {
    Pop-Location
}
