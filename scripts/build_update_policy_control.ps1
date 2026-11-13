param([string]$WindowsSdkVersion='10.0.22621.0')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$vsWhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$vsRoot=& $vsWhere -latest -version '[17.0,18.0)' -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if($LASTEXITCODE -ne 0 -or -not $vsRoot){throw 'Visual Studio 2022 C++ tools required.'}
$env:PATH=[IO.Path]::GetDirectoryName($vsWhere)+';'+$env:PATH
& (Join-Path $vsRoot 'Common7\Tools\Launch-VsDevShell.ps1') -Arch amd64 -HostArch amd64 -SkipAutomaticLocation | Out-Null
$out=Join-Path $repo ('.artifacts\native-policy-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $out | Out-Null
$sdk=Join-Path ${env:ProgramFiles(x86)} ('Windows Kits\10\Include\'+$WindowsSdkVersion)
$includes=@('ucrt','um','shared','winrt','cppwinrt') | ForEach-Object {'/I'+(Join-Path $sdk $_)}
$exe=Join-Path $out 'UpdatePolicyControl.exe'
& cl.exe /nologo /std:c++20 /EHsc /MT /O2 /W4 @includes (Join-Path $repo 'tests\WordSuggestorWindows.Native\UpdatePolicyControl.cpp') ('/Fo'+(Join-Path $out 'control.obj')) ('/Fe'+$exe) /link windowsapp.lib advapi32.lib
if($LASTEXITCODE -ne 0){throw 'Native G05 control build failed.'}
Write-Output ('Native policy control: '+$exe)
[pscustomobject]@{NativeControlPath=$exe}
