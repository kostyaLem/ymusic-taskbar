param([Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$projectRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$folders = @('src','tests','assets','installer','scripts','.github')
$rootFiles = @('.gitignore','.editorconfig','Directory.Build.props','global.json','YMusicTaskbar.slnx','build.ps1','README.md','README.en.md','USAGE.md','THIRD-PARTY.md','CHANGELOG.md','CONTRIBUTING.md','SECURITY.md','LICENSE')
$files = @()
function Test-PublishableFile([IO.FileInfo]$File) {
    $segments = [IO.Path]::GetRelativePath($projectRoot, $File.FullName).Split([IO.Path]::DirectorySeparatorChar)
    if ($segments | Where-Object { $_ -in @('bin','obj','.git','.vs','.idea','.tools','artifacts','publish','dist','node_modules','docs','private') }) { return $false }
    if ($File.Name -match '^\.env($|\.)' -and $File.Name -notin @('.env.example','.env.sample')) { return $false }
    if ($File.Name -like '*.internal.md') { return $false }
    return $File.Name -notmatch '(?i)(\.(pem|key|pfx|p12|pdb|user|suo)$|^id_(rsa|ed25519)|^secrets\.json$|^credentials.*\.json$|^\.(git-credentials|npmrc|pypirc)$|^(settings|diagnostics)\.json$|^errors\.log$)'
}
foreach ($name in $rootFiles) { $path = Join-Path $projectRoot $name; if (Test-Path -LiteralPath $path) { $files += Get-Item -LiteralPath $path } }
foreach ($folder in $folders) {
    $folderPath = Join-Path $projectRoot $folder
    if (-not (Test-Path -LiteralPath $folderPath)) { continue }
    $files += Get-ChildItem -LiteralPath $folderPath -Recurse -File -Force | Where-Object {
        Test-PublishableFile $_
    }
}
$stream = [IO.File]::Create([IO.Path]::GetFullPath($Destination))
$archive = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files | Sort-Object FullName -Unique) {
        $relative = [IO.Path]::GetRelativePath($projectRoot,$file.FullName).Replace([IO.Path]::DirectorySeparatorChar,[char]'/')
        $entry = $archive.CreateEntry($relative,[IO.Compression.CompressionLevel]::Optimal)
        $input = [IO.File]::OpenRead($file.FullName); $output = $entry.Open()
        try { $input.CopyTo($output) } finally { $input.Dispose(); $output.Dispose() }
    }
} finally { $archive.Dispose(); $stream.Dispose() }
