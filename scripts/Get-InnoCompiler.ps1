param([switch]$Bootstrap)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$localCompiler = Join-Path $projectRoot '.tools\InnoSetup\ISCC.exe'
if (Test-Path -LiteralPath $localCompiler) { return $localCompiler }
$command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($command) { return $command.Source }
foreach ($directory in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
    foreach ($version in @('Inno Setup 7','Inno Setup 6')) {
        $candidate = Join-Path $directory ($version + '\ISCC.exe')
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }
}
if (-not $Bootstrap) { throw 'Install Inno Setup or run build.ps1 -Installer -BootstrapCompiler.' }
$tools = Join-Path $projectRoot '.tools'
New-Item -ItemType Directory -Path $tools -Force | Out-Null
$download = Join-Path $tools 'innosetup-6.7.3.exe'
if (-not (Test-Path -LiteralPath $download)) {
    Invoke-WebRequest 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe?download=1' -OutFile $download
}
$expected = '9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732'
if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne $expected) { throw 'Inno Setup download hash mismatch.' }
$signature = Get-AuthenticodeSignature -LiteralPath $download
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Pyrsys B.V.') { throw 'Inno Setup signature verification failed.' }
$destination = Join-Path $tools 'InnoSetup'
$process = Start-Process -FilePath $download -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER','/NOICONS','/TASKS=""',('/DIR="'+$destination+'"')) -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Inno Setup installation failed: $($process.ExitCode)" }
return $localCompiler
