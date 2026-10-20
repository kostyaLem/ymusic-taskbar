param(
    [switch]$Publish,
    [switch]$Installer,
    [switch]$BootstrapCompiler,
    [switch]$DesktopTests,
    [string]$Runtime = 'win-x64'
)
$ErrorActionPreference = 'Stop'
function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw ('dotnet failed: ' + ($Arguments -join ' ')) }
}
Push-Location $PSScriptRoot
try {
    Invoke-Dotnet @('restore','YMusicTaskbar.slnx','--disable-parallel','-m:1')
    Invoke-Dotnet @('build','YMusicTaskbar.slnx','-c','Release','--no-restore','-m:1')
    foreach ($project in @('YMusicTaskbar.Core.Tests','YMusicTaskbar.App.Tests')) {
        Invoke-Dotnet @('run','--project',("tests/$project/$project.csproj"),'-c','Release','--no-build')
    }
    & "$PSScriptRoot/tests/Test-SourceArchive.ps1"
    if ($DesktopTests) {
        $application = Join-Path $PSScriptRoot 'src/YMusicTaskbar.App/bin/Release/net10.0-windows10.0.19041.0/YMusicTaskbar.exe'
        Invoke-Dotnet @('run','--project','tests/YMusicTaskbar.Windows.Tests/YMusicTaskbar.Windows.Tests.csproj','-c','Release','--no-build','--',$application,(Join-Path $PSScriptRoot 'artifacts/desktop-tests'))
    }
    if (-not ($Publish -or $Installer)) { return }
    if ($Runtime -ne 'win-x64') { throw 'The current installer is verified for win-x64 only.' }
    $publishDirectory = Join-Path $PSScriptRoot "publish/$Runtime"
    Invoke-Dotnet @('publish','src/YMusicTaskbar.App/YMusicTaskbar.App.csproj','-c','Release','-r',$Runtime,'--self-contained','true','-p:TargetLatestRuntimePatch=true','-p:DebugType=None','-p:DebugSymbols=false','-m:1','-o',$publishDirectory)
    Get-ChildItem -LiteralPath $publishDirectory -Filter 'YMusicTaskbar*.pdb' -File | ForEach-Object { Remove-Item -LiteralPath $_.FullName }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination (Join-Path $publishDirectory 'YMusicTaskbar-LICENSE.txt')
    & "$PSScriptRoot/scripts/Copy-RuntimeNotices.ps1" -PublishDirectory $publishDirectory -Runtime $Runtime
    $version = ([xml](Get-Content -LiteralPath 'Directory.Build.props' -Raw)).Project.PropertyGroup.Version
    $destination = Join-Path $PSScriptRoot 'dist'
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    if ($Installer) {
        $compiler = & "$PSScriptRoot/scripts/Get-InnoCompiler.ps1" -Bootstrap:$BootstrapCompiler
        & $compiler '/Q' "/DAppVersion=$version" "/DPublishDir=$publishDirectory" "$PSScriptRoot/installer/YMusicTaskbar.iss"
        if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
    }
    $portable = Join-Path $destination "YMusicTaskbar-$version-$Runtime-portable.zip"
    Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $portable -Force
    & "$PSScriptRoot/scripts/New-SourceArchive.ps1" -Destination (Join-Path $destination "YMusicTaskbar-$version-source.zip")
    $hashes = Get-ChildItem -LiteralPath $destination -File | Where-Object { $_.Name -like "YMusicTaskbar-$version-*" -and $_.Extension -in '.exe','.zip' } | Sort-Object Name | ForEach-Object {
        (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $_.Name
    }
    $hashes | Set-Content -LiteralPath (Join-Path $destination 'SHA256SUMS.txt') -Encoding utf8
    Get-ChildItem -LiteralPath $destination -File | Select-Object Name,Length
} finally { Pop-Location }
