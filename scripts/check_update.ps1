param([Parameter(Mandatory=$true)][uri]$AppInstallerUri)
$ErrorActionPreference = 'Stop'
if ($AppInstallerUri.Scheme -ne 'https' -or $AppInstallerUri.UserInfo -or $AppInstallerUri.Query -or $AppInstallerUri.Fragment) {
    throw 'Use the credential-free HTTPS App Installer URL supplied by the release owner.'
}
# Windows verifies publisher, signature, package hash/block map and installation
# transaction. No custom updater, unsigned fallback or forced application shutdown.
Add-AppxPackage -AppInstallerFile -Path $AppInstallerUri.AbsoluteUri
