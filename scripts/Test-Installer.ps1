param([switch]$LaunchSmokeTest)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$version = ([xml](Get-Content -LiteralPath (Join-Path $projectRoot 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version
$setup = Join-Path $projectRoot "dist/YMusicTaskbar-$version-win-x64-Setup.exe"
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{8E24CD71-4DD7-4F8B-80B9-54EA4B20CB76}_is1'
if (Test-Path -LiteralPath $registryPath) { throw 'Use a clean Windows account for this test: an installation already exists.' }
$runPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$startupBefore = (Get-ItemProperty -LiteralPath $runPath -Name YMusicTaskbar -ErrorAction SilentlyContinue).YMusicTaskbar
$settings = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'YMusicTaskbar/settings.json'
$settingsHash = if (Test-Path -LiteralPath $settings) { (Get-FileHash -LiteralPath $settings).Hash } else { $null }
$output = Join-Path $projectRoot ('artifacts/installer-' + [Guid]::NewGuid().ToString('N'))
$target = [IO.Path]::GetFullPath((Join-Path $output 'App With Spaces'))
$allowed = [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts')) + [IO.Path]::DirectorySeparatorChar
if (-not $target.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe installer test destination.' }
New-Item -ItemType Directory -Path $output -Force | Out-Null
$shortcut = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs/YMusic Taskbar.lnk'
$oldShortcut = if (Test-Path -LiteralPath $shortcut) { [IO.File]::ReadAllBytes($shortcut) } else { $null }
$checks = [Collections.Generic.List[string]]::new()
function Assert-Check([bool]$Condition,[string]$Name) { if (-not $Condition) { throw $Name }; $checks.Add($Name); Write-Output "PASS $Name" }
function Run-Setup([string]$Executable,[string[]]$Arguments) {
    $process = Start-Process -FilePath $Executable -ArgumentList $Arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Setup process exited $($process.ExitCode)" }
}
$uninstaller = Join-Path $target 'unins000.exe'
$live = $null
try {
    Run-Setup $setup @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/TASKS=""',('/DIR="'+$target+'"'),('/LOG="'+(Join-Path $output 'install.log')+'"'))
    $exe = Join-Path $target 'YMusicTaskbar.exe'
    Assert-Check (Test-Path -LiteralPath $exe) 'Installer creates application in path with spaces'
    Assert-Check ((Test-Path -LiteralPath (Join-Path $target 'coreclr.dll')) -and (Test-Path -LiteralPath (Join-Path $target 'PresentationFramework.dll'))) 'Installer bundles .NET and Windows Desktop runtime'
    Assert-Check ((Test-Path -LiteralPath (Join-Path $target 'YMusicTaskbar.Core.dll')) -and (Test-Path -LiteralPath (Join-Path $target 'YMusicTaskbar.Windows.dll'))) 'Installer includes architectural assemblies'
    Assert-Check ((Test-Path -LiteralPath (Join-Path $target 'legal/Microsoft.NETCore.App-THIRD-PARTY-NOTICES.TXT')) -and (Test-Path -LiteralPath (Join-Path $target 'legal/Microsoft.WindowsDesktop.App-LICENSE')) -and (Test-Path -LiteralPath (Join-Path $target 'legal/CsWinRT-LICENSE.txt'))) 'Installer includes bundled dependency licenses and notices'
    Assert-Check (Test-Path -LiteralPath $shortcut) 'Start menu shortcut created'
    Assert-Check (Test-Path -LiteralPath $registryPath) 'Per-user uninstall registration created'
    Assert-Check ((Get-Item $exe).VersionInfo.ProductVersion.StartsWith($version)) 'Executable carries release version'
    Assert-Check (@(Get-ChildItem -LiteralPath $target -Filter 'YMusicTaskbar*.pdb' -File).Count -eq 0) 'Release omits local debug symbols'
    Run-Setup $exe @('--quit')
    Assert-Check $true 'Installed executable starts its bundled runtime'
    Run-Setup $setup @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/TASKS=""',('/DIR="'+$target+'"'))
    Assert-Check (Test-Path -LiteralPath $uninstaller) 'Reinstall preserves working uninstaller'
    if ($LaunchSmokeTest) {
        $smoke = Join-Path $output 'smoke'
        Run-Setup $exe @('--smoke-test','--output',('"'+$smoke+'"'))
        $state = Get-Content -LiteralPath (Join-Path $smoke 'smoke.json') -Raw | ConvertFrom-Json
        Assert-Check ($null -eq $state.Error) 'Installed application smoke run has no errors'
        $live = Start-Process -FilePath $exe -ArgumentList @('--test-host','--output',('"'+(Join-Path $output 'live')+'"')) -WindowStyle Hidden -PassThru
        Start-Sleep -Seconds 2
    }
    Run-Setup $uninstaller @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $output 'uninstall.log')+'"'))
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while ((Test-Path -LiteralPath $exe) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 200 }
    Assert-Check (-not (Test-Path -LiteralPath $exe)) 'Uninstaller removes application files'
    Assert-Check (-not (Test-Path -LiteralPath $registryPath)) 'Uninstaller removes application registration'
    if ($live) { $live.Refresh(); Assert-Check $live.HasExited 'Uninstaller closes running observer gracefully' }
    $startupAfter = (Get-ItemProperty -LiteralPath $runPath -Name YMusicTaskbar -ErrorAction SilentlyContinue).YMusicTaskbar
    Assert-Check ($startupBefore -eq $startupAfter) 'Installer leaves pre-existing startup preference unchanged'
    $afterHash = if (Test-Path -LiteralPath $settings) { (Get-FileHash -LiteralPath $settings).Hash } else { $null }
    Assert-Check ($settingsHash -eq $afterHash) 'User settings preserved across install and uninstall'
    [pscustomobject]@{ Passed=$true; Checks=$checks.ToArray(); Directory=$target } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $output 'results.json')
} finally {
    if (Test-Path -LiteralPath $uninstaller) { Run-Setup $uninstaller @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') }
    if ($oldShortcut) { [IO.File]::WriteAllBytes($shortcut,$oldShortcut) }
    $startupFinal = (Get-ItemProperty -LiteralPath $runPath -Name YMusicTaskbar -ErrorAction SilentlyContinue).YMusicTaskbar
    if ($null -ne $startupBefore -and $startupFinal -ne $startupBefore) {
        Set-ItemProperty -LiteralPath $runPath -Name YMusicTaskbar -Value $startupBefore
    }
    if ($live) { $live.Dispose() }
}
