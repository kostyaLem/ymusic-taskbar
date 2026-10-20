$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$fixture = Join-Path $projectRoot ('artifacts/archive-test-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path "$fixture/scripts","$fixture/src/local","$fixture/src/obj","$fixture/docs" -Force | Out-Null
Copy-Item -LiteralPath "$projectRoot/scripts/New-SourceArchive.ps1" -Destination "$fixture/scripts/New-SourceArchive.ps1"
Set-Content -LiteralPath "$fixture/README.md" -Value 'Publication fixture'
Set-Content -LiteralPath "$fixture/src/Good.cs" -Value '// Publishable source'
foreach ($file in @('.env','.env.local','signing.pfx','private.pem','session.key','symbols.pdb','id_ed25519.pub','credentials.local.json','settings.json','diagnostics.json','.npmrc','notes.internal.md')) {
    Set-Content -LiteralPath (Join-Path "$fixture/src/local" $file) -Value 'NONSECRET_TEST_FIXTURE'
}
Set-Content -LiteralPath "$fixture/src/obj/cache.txt" -Value 'BUILD_FIXTURE'
Set-Content -LiteralPath "$fixture/USAGE.md" -Value 'Public instructions'
Set-Content -LiteralPath "$fixture/docs/PRIVATE.md" -Value 'PRIVATE_DOCUMENT_FIXTURE'
Add-Type -AssemblyName System.IO.Compression.FileSystem
& "$fixture/scripts/New-SourceArchive.ps1" -Destination "$fixture/source.zip"
$zip = [IO.Compression.ZipFile]::OpenRead("$fixture/source.zip")
try {
    foreach ($required in @('README.md','src/Good.cs','USAGE.md')) {
        if (-not $zip.GetEntry($required)) { throw "Missing public file: $required" }
    }
    if ($zip.Entries | Where-Object { $_.FullName.StartsWith('src/local/') -or $_.FullName.StartsWith('src/obj/') -or $_.FullName.StartsWith('docs/') }) {
        throw 'Private or build fixture leaked into source archive'
    }
    'PASS Source archive retains source and user guides while excluding internal documents, local settings, keys, credentials and debug files'
} finally { $zip.Dispose() }
