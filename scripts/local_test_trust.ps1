# Elevated, temporary leaf-certificate trust for one isolated qualification run.
# Never imports a root, exports a private key, or trusts a production identity.
param([Parameter(Mandatory=$true)][string]$SessionDirectory,
      [Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9]{40}$')][string]$Thumbprint)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root = [IO.Path]::GetFullPath($SessionDirectory)
if (-not $root.StartsWith(($repo + '\.artifacts\msix-tests\'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Test trust session is outside the test workspace.' }
$cert = New-Object Security.Cryptography.X509Certificates.X509Certificate2 (Join-Path $root 'local-test-public.cer')
if ($cert.Thumbprint -ne $Thumbprint -or $cert.Subject -cnotmatch '^CN=WordSuggestor Local Test [a-f0-9]{12}$' -or
    $cert.NotAfter -gt [DateTime]::Now.AddDays(10)) { throw 'Only the short-lived isolated local-test certificate may be trusted.' }
$basic = $cert.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.19' }
$eku = $cert.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.37' }
if (-not $basic -or $basic.CertificateAuthority -or -not $eku -or $eku.EnhancedKeyUsages.Count -ne 1 -or @($eku.EnhancedKeyUsages | Where-Object Value -eq '1.3.6.1.5.5.7.3.3').Count -ne 1) {
    throw 'Expected a non-CA codesigning leaf certificate.'
}
$store = New-Object Security.Cryptography.X509Certificates.X509Store('TrustedPeople','LocalMachine')
$added = $false
try {
    $store.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    if (@($store.Certificates | Where-Object Thumbprint -eq $Thumbprint).Count -eq 0) { $store.Add($cert); $added = $true }
    [IO.File]::WriteAllText((Join-Path $root 'trust-ready.json'),(@{ thumbprint=$Thumbprint; added=$added } | ConvertTo-Json))
    $limit = [DateTime]::UtcNow.AddMinutes(30)
    while (-not (Test-Path -LiteralPath (Join-Path $root 'cleanup.request')) -and [DateTime]::UtcNow -lt $limit) {
        Start-Sleep -Seconds 1
    }
} finally {
    if ($added) { $store.Remove($cert) }
    $store.Close()
    [IO.File]::WriteAllText((Join-Path $root 'trust-cleaned.json'),(@{ thumbprint=$Thumbprint; removed=$added } | ConvertTo-Json))
    $cert.Dispose()
}
