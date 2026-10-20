$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $projectRoot 'assets/screenshots'
dotnet run --project "$PSScriptRoot/DocumentationPreview/DocumentationPreview.csproj" -c Release -- $projectRoot $destination
if ($LASTEXITCODE -ne 0) { throw "Screenshot rendering failed; see $destination/error.txt" }
Get-ChildItem -LiteralPath $destination -Filter '*.png' | Select-Object Name,Length
