param([Parameter(Mandatory)][string]$PublishDirectory,[string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$packages = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget/packages' }
$configuration = Get-Content -LiteralPath (Join-Path $PublishDirectory 'YMusicTaskbar.runtimeconfig.json') -Raw | ConvertFrom-Json
$legal = Join-Path $PublishDirectory 'legal'
New-Item -ItemType Directory -Path $legal -Force | Out-Null
foreach ($framework in $configuration.runtimeOptions.includedFrameworks) {
    $package = switch ($framework.name) {
        'Microsoft.NETCore.App' { "microsoft.netcore.app.runtime.$Runtime" }
        'Microsoft.WindowsDesktop.App' { "microsoft.windowsdesktop.app.runtime.$Runtime" }
        default { throw "Unrecognized bundled framework: $($framework.name)" }
    }
    $directory = Join-Path $packages "$package/$($framework.version)"
    $notices = @(Get-ChildItem -LiteralPath $directory -File | Where-Object { $_.Name -match '^(LICENSE|THIRD-PARTY-NOTICES)(\.TXT)?$' })
    if (-not ($notices | Where-Object { $_.Name -match '^LICENSE' })) { throw "Missing runtime license in $directory" }
    foreach ($notice in $notices) {
        Copy-Item -LiteralPath $notice.FullName -Destination (Join-Path $legal ($framework.name + '-' + $notice.Name))
    }
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'installer/licenses/CsWinRT-LICENSE.txt') -Destination $legal
Copy-Item -LiteralPath (Join-Path $projectRoot 'THIRD-PARTY.md') -Destination $legal
