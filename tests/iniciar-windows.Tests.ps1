# Ejecutar en PowerShell sobre un entorno de pruebas con el puerto 5080 libre.
# Sustituye dotnet y las entradas de contraseña: no conecta ni cambia ninguna base.
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../scripts/iniciar-windows.ps1'
$tokens = $null; $syntaxErrors = $null
[void][Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$syntaxErrors)
if ($syntaxErrors.Count) { throw ($syntaxErrors | Out-String) }
if ([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners() | Where-Object { $_.Port -eq 5080 }) { throw 'Usa un entorno de pruebas con puerto 5080 libre.' }
$initialLocation = Get-Location
$initialNow = $env:Demo__Now
$env:Demo__Now = '2026-11-06T08:30:00-06:00'
function global:Read-Host { param($Prompt, [switch]$AsSecureString) ConvertTo-SecureString 'Prueba-local-123!' -AsPlainText -Force }
function global:dotnet {
 $global:LASTEXITCODE = 0
 if ($args[0] -eq 'run') { $global:ComedoresTestNow = $env:Demo__Now; $global:ComedoresTestArguments = @($args); $global:ComedoresTestIp = $env:Demo__TerminalIp }
}
function Check($condition, $label) { if (-not $condition) { throw $label }; Write-Host "PASS: $label" }
function Reject($arguments, $expected) {
 $caught = $false
 try { & $scriptPath @arguments } catch { $caught = $_.Exception.Message -match $expected }
 Check $caught "Rechaza $expected"
}
try {
 & $scriptPath -FechaHoraPrueba '2026-11-06T14:30:00-06:00' -Comedor PLATAFORMA
 Check ($global:ComedoresTestNow -eq '2026-11-06T14:30:00-06:00') 'Fecha y hora explicitas sustituyen la hora previa'
 Check ($global:ComedoresTestArguments -contains '--Demo:Now=2026-11-06T14:30:00-06:00') 'La fecha se pasa a la aplicacion como argumento explicito'
 Check ($global:ComedoresTestIp -eq '172.16.60.54') 'Conserva la seleccion de comedor'
 Check ($env:Demo__Now -eq '2026-11-06T08:30:00-06:00') 'Restaura el entorno de la terminal al terminar'
 & $scriptPath -HoraReal
 Check ([string]::IsNullOrEmpty($global:ComedoresTestNow)) 'Hora real elimina la simulacion anterior'
 Check ($global:ComedoresTestArguments -contains '--Demo:Now=') 'Hora real tambien anula configuraciones simuladas anteriores'
 & $scriptPath -SimularDesayuno
 Check ($global:ComedoresTestNow -match 'T08:30:00-06:00$') 'Mantiene la opcion de desayuno rapido'
 Reject @{FechaHoraPrueba='2026-02-30T14:30:00-06:00'} 'Fecha de prueba invalida'
 Reject @{FechaHoraPrueba='14:30'} 'Fecha de prueba invalida'
 Reject @{HoraReal=$true;FechaHoraPrueba='2026-11-06T14:30:00-06:00'} 'Elija hora real o simulacion'
 $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,5080)
 try { $listener.Start(); Reject @{FechaHoraPrueba='2026-11-06T14:30:00-06:00'} 'puerto 5080 ya esta en uso' }
 finally { $listener.Stop() }
} finally {
 $env:Demo__Now = $initialNow
 Set-Location $initialLocation
 Remove-Item Function:Read-Host -ErrorAction SilentlyContinue
 Remove-Item Function:dotnet -ErrorAction SilentlyContinue
 Remove-Variable ComedoresTestNow,ComedoresTestArguments,ComedoresTestIp -Scope Global -ErrorAction SilentlyContinue
}
