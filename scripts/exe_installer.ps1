# Called by package_app.ps1; payload publication/Core/pack staging stays shared.
function New-WordSuggestorExeInstaller([string]$Stage,[string]$Bundle,[string]$Version,[string]$DisplayVersion,[string]$Publisher,[string]$CertificateThumbprint,[bool]$Preview,[string]$PreviewId,[string]$CompilerPath) {
    if($DisplayVersion -cnotmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.]+)?$'){throw 'EXE display version must be a safe semantic version.'}
    if($Preview -and $PreviewId -cnotmatch '^[a-f0-9]{12}$'){throw 'Preview installer requires an isolated nonce.'}
    if(-not $Preview -and $CertificateThumbprint -cnotmatch '^[a-fA-F0-9]{40}$'){throw 'Public EXE installers require the approved signing certificate.'}
    if(-not $Preview){$certificate=Get-Item -LiteralPath ('Cert:\CurrentUser\My\'+$CertificateThumbprint);if(-not $certificate.HasPrivateKey -or $certificate.Subject -cne $Publisher){throw 'EXE signing identity/publisher mismatch.'}}
    if(-not $CompilerPath){$CompilerPath=& (Join-Path $PSScriptRoot 'bootstrap_installer.ps1')|Select-Object -Last 1}
    if(-not (Test-Path -LiteralPath $CompilerPath)){throw 'Inno Setup compiler missing.'}
    $id=if($Preview){'WordSuggestor.Windows.Preview.'+$PreviewId}else{'WordSuggestor.Windows.Beta.Direct'}
    $name=if($Preview){'WordSuggestor Preview '+$PreviewId}else{'WordSuggestor Beta'}
    $leaf=if($Preview){'WordSuggestorPreview-'+$PreviewId}else{'WordSuggestorBeta'}
    $output=if($Preview){'WordSuggestor.Preview_'+$Version+'_'+$PreviewId+'_x64'}else{'WordSuggestor.Beta_'+$Version+'_x64'}
    $marker=@{schema_version=1;product_id=$id;package_version=$Version;display_version=$DisplayVersion;publisher=$Publisher;updates_enabled=(-not $Preview)}
    [IO.File]::WriteAllText((Join-Path $Stage 'direct-install.json'),($marker|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
    Add-Type -AssemblyName System.Drawing
    if(-not ('WordSuggestor.Packaging.IconHandle' -as [type])){Add-Type 'using System; using System.Runtime.InteropServices; namespace WordSuggestor.Packaging { public static class IconHandle { [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr h); } }'}
    $iconPath=Join-Path $Stage 'Assets\WordSuggestor.ico'
    $bitmap=New-Object Drawing.Bitmap (Join-Path $windowsRepo 'src\WordSuggestorWindows.App\Assets\AppIcon.png')
    $handle=$bitmap.GetHicon();$icon=[Drawing.Icon]::FromHandle($handle);$stream=[IO.File]::Create($iconPath)
    try{$icon.Save($stream)}finally{$stream.Dispose();$icon.Dispose();$null=[WordSuggestor.Packaging.IconHandle]::DestroyIcon($handle);$bitmap.Dispose()}
    $arguments=@(('/DPayload='+$Stage),('/DProductId='+$id),('/DAppName='+$name),('/DInstallLeaf='+$leaf),('/DPackageVersion='+$Version),('/DDisplayVersion='+$DisplayVersion),('/DOutputDirectory='+$Bundle),('/DOutputName='+$output),('/DIconPath='+$iconPath))
    if($Preview){$arguments+='/DLocalPreview'}
    if(-not $Preview){
        $signTool=Get-ReleaseSdkTool 'signtool'
        $null=& $signTool sign /sha1 $CertificateThumbprint /fd SHA256 /tr 'https://timestamp.digicert.com' /td SHA256 (Join-Path $Stage 'WordSuggestorWindows.App.exe') 2>&1
        if($LASTEXITCODE -ne 0){throw 'EXE payload signing failed.'}
        $arguments+=@('/DSignedBuild',('/SWordSuggestor=$q'+$signTool+'$q sign /sha1 '+$CertificateThumbprint+' /fd SHA256 /tr https://timestamp.digicert.com /td SHA256 $f'))
    }
    $arguments+=(Join-Path $windowsRepo 'packaging\WordSuggestor.iss')
    & $CompilerPath @arguments
    if($LASTEXITCODE -ne 0){throw 'EXE installer compilation failed.'}
    $path=Join-Path $Bundle ($output+'.exe')
    $pe=[Diagnostics.FileVersionInfo]::GetVersionInfo($path)
    if([version]::new($pe.FileMajorPart,$pe.FileMinorPart,$pe.FileBuildPart,$pe.FilePrivatePart) -ne [version]$Version){throw 'Installer embedded version differs from release version.'}
    $signing=if($Preview){@{publisher=$Publisher;signature_verified=$false;verification_result='local_unsigned_preview'}}else{Test-MsixSignature $path $Publisher}
    $draft=@{schema_version=1;backend_contract_status='extension_required';release=@{platform='windows';architecture='x64';channel='beta';version=$DisplayVersion;package_version=$Version;package_type='exe';status='pending';artifact=@{filename=[IO.Path]::GetFileName($path);size_bytes=(Get-Item $path).Length;sha256=(Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant()};signing=$signing};local_preview=$Preview;product_id=$id}
    [IO.File]::WriteAllText((Join-Path $Bundle 'release-manifest.draft.json'),($draft|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
    Write-Output ('EXE installer: '+$path)
    [pscustomobject]@{InstallerPath=$path;ProductId=$id;LocalPreview=$Preview;BundleDirectory=$Bundle}
}
