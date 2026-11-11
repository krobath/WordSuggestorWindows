$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime
    $null = [Windows.Security.Credentials.KeyCredentialManager,Windows.Security.Credentials,ContentType=WindowsRuntime]
    $null = [Windows.Security.Cryptography.CryptographicBuffer,Windows.Security.Cryptography,ContentType=WindowsRuntime]
    function Await-Operation($operation,[type]$type) {
        $method = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetGenericArguments().Count -eq 1 -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
        $task = $method.MakeGenericMethod($type).Invoke($null,@($operation))
        if (-not $task.Wait(50000)) { throw 'timeout' }
        return $task.GetAwaiter().GetResult()
    }
    $request = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $supported = Await-Operation ([Windows.Security.Credentials.KeyCredentialManager]::IsSupportedAsync()) ([bool])
    if ($request.operation -eq 'capability') { [Console]::Out.Write((@{available=[bool]$supported} | ConvertTo-Json -Compress)); exit 0 }
    if (-not $supported -or $request.name -notmatch '^WordSuggestor\.Session\.[a-f0-9]{32}$') { throw 'unavailable' }
    if ($request.operation -eq 'delete') {
        $action = [Windows.Security.Credentials.KeyCredentialManager]::DeleteAsync($request.name)
        $method = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and -not $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction' } | Select-Object -First 1
        $task = $method.Invoke($null,@($action))
        if (-not $task.Wait(50000)) { throw 'timeout' }
        $task.GetAwaiter().GetResult()
        [Console]::Out.Write('{"available":true}'); exit 0
    }
    if ($request.operation -eq 'create') {
        $op = [Windows.Security.Credentials.KeyCredentialManager]::RequestCreateAsync($request.name,[Windows.Security.Credentials.KeyCredentialCreationOption]::FailIfExists)
    } elseif ($request.operation -eq 'open') {
        $op = [Windows.Security.Credentials.KeyCredentialManager]::OpenAsync($request.name)
    } else { throw 'invalid' }
    $key = Await-Operation $op ([Windows.Security.Credentials.KeyCredentialRetrievalResult])
    if ($key.Status.ToString() -ne 'Success') { throw 'denied' }
    $challenge = [Windows.Security.Cryptography.CryptographicBuffer]::DecodeFromBase64String($request.challenge)
    if ($challenge.Length -ne 32) { throw 'invalid' }
    $signature = Await-Operation ($key.Credential.RequestSignAsync($challenge)) ([Windows.Security.Credentials.KeyCredentialOperationResult])
    if ($signature.Status.ToString() -ne 'Success') { throw 'denied' }
    $null = [Windows.Security.Cryptography.Core.CryptographicPublicKeyBlobType,Windows.Security.Cryptography.Core,ContentType=WindowsRuntime]
    $public = $key.Credential.RetrievePublicKey([Windows.Security.Cryptography.Core.CryptographicPublicKeyBlobType]::X509SubjectPublicKeyInfo)
    [Console]::Out.Write((@{available=$true;signature=[Windows.Security.Cryptography.CryptographicBuffer]::EncodeToBase64String($signature.Result);publicKey=[Windows.Security.Cryptography.CryptographicBuffer]::EncodeToBase64String($public)} | ConvertTo-Json -Compress))
} catch { [Console]::Out.Write('{"available":false}'); exit 1 }
