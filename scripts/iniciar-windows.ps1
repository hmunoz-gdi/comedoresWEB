param([switch]$HoraReal)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
function Read-PrivateText([string]$Prompt) {
    $secret = Read-Host $Prompt -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}
$names = @('ConnectionStrings__Comedores','Demo__Password','Demo__TerminalIp','Demo__Now','ASPNETCORE_ENVIRONMENT')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    dotnet restore src/Comedores.Web --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron descargar las dependencias.' }
    Write-Host 'Introduce la contrasena de PostgreSQL que elegiste durante su instalacion.'
    $databasePassword = Read-PrivateText 'Contrasena de postgres (no se muestra)'
    $quotedPassword = $databasePassword.Replace('"','""')
    $env:ConnectionStrings__Comedores = 'Host=127.0.0.1;Port=5432;Database=comedores;Username=postgres;Password="' + $quotedPassword + '"'
    Write-Host 'La primera vez, elige una contrasena para el usuario demo de la aplicacion.'
    Write-Host 'Minimo 12 caracteres, con mayuscula, minuscula, numero y simbolo. En siguientes arranques usa la misma.'
    $env:Demo__Password = Read-PrivateText 'Contrasena de la cuenta demo (no se muestra)'
    if ($env:Demo__Password.Length -lt 12 -or $env:Demo__Password -cnotmatch '[A-Z]' -or $env:Demo__Password -cnotmatch '[a-z]' -or $env:Demo__Password -notmatch '[0-9]' -or $env:Demo__Password -notmatch '[^a-zA-Z0-9]') {
        throw 'La contrasena demo no cumple los requisitos indicados.'
    }
    $env:Demo__TerminalIp = '172.16.66.230'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:Demo__Now = $null
    if (-not $HoraReal) {
        $env:Demo__Now = (Get-Date).ToString('yyyy-MM-dd') + 'T08:30:00-06:00'
        Write-Host 'DEMOSTRACION: comedor SUBESTACION, hora simulada 08:30 para probar el desayuno.'
    }
    Write-Host 'Cuando aparezca Now listening on, abre http://localhost:5080 en tu navegador.'
    Write-Host 'Usuario: demo. Para detener la aplicacion presiona Ctrl+C.'
    dotnet run --project src/Comedores.Web --no-launch-profile --urls http://127.0.0.1:5080
    if ($LASTEXITCODE -ne 0) { throw 'La aplicacion se detuvo con un error. Revisa los mensajes anteriores.' }
}
finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    $databasePassword = $null
    $quotedPassword = $null
}
