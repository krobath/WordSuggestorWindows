[CmdletBinding(SupportsShouldProcess=$true)]
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'release_transport.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$pin=(Get-Content (Join-Path $repo 'dependencies.lock.json') -Raw|ConvertFrom-Json).innoSetup
$root=Join-Path $repo ('.artifacts\tools\inno-'+$pin.version)
$compiler=Join-Path $root 'ISCC.exe';$receiptPath=Join-Path $root 'bootstrap.json'
if((Test-Path -LiteralPath $compiler) -and (Test-Path -LiteralPath $receiptPath)){
    $receipt=Get-Content $receiptPath -Raw|ConvertFrom-Json
    if($receipt.installer_sha256 -cne $pin.sha256 -or $receipt.compiler_sha256 -ine (Get-FileHash $compiler -Algorithm SHA256).Hash){throw 'Installer compiler changed; preserve evidence and re-provision it.'}
    Write-Output $compiler;return
}
$registered=Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1' -ErrorAction SilentlyContinue
if($registered.InstallLocation -and [IO.Path]::GetFullPath($registered.InstallLocation).TrimEnd('\') -ine $root.TrimEnd('\')){throw 'An existing Inno Setup installation is preserved. Pass its ISCC.exe through InstallerCompilerPath instead of replacing it.'}
if(-not $PSCmdlet.ShouldProcess('pinned signed Inno Setup compiler','Download, verify SHA-256 and Authenticode, install per-user under .artifacts/tools; no admin or certificate trust changes')){return}
New-Item -ItemType Directory -Path $root -Force|Out-Null
$download=Join-Path $root 'compiler-setup.exe'
Invoke-WebRequest -UseBasicParsing -Uri $pin.url -OutFile $download -TimeoutSec 120
if((Get-FileHash $download -Algorithm SHA256).Hash -ine $pin.sha256){throw 'Installer compiler download hash mismatch.'}
$signature=Get-AuthenticodeSignature -LiteralPath $download
if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::SimpleName,$false) -cne $pin.publisher){throw 'Installer compiler publisher/trust verification failed.'}
$result=Invoke-ReleaseProcess $download @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/CURRENTUSER',('/DIR='+$root),'/NOICONS') 90
if($result.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $compiler)){throw 'Per-user installer compiler provisioning failed.'}
[IO.File]::WriteAllText($receiptPath,(@{version=$pin.version;installer_sha256=$pin.sha256;compiler_sha256=(Get-FileHash $compiler -Algorithm SHA256).Hash.ToLowerInvariant()}|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Write-Output $compiler
