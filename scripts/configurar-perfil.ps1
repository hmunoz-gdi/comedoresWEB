$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$folder = Join-Path (Get-Location) '.local'
$path = Join-Path $folder 'empleado.json'
if (Test-Path $path) { throw 'El perfil local ya existe. No se sobrescribio. Revise .local\empleado.json.' }
$number = Read-Host 'Numero de empleado'
if ($number -notmatch '^\d{1,10}$') { throw 'El numero debe contener de 1 a 10 digitos.' }
$name = Read-Host 'Nombre completo'
$area = Read-Host 'Area'
$position = Read-Host 'Puesto'
$schedule = Read-Host 'Horario laboral'
$special = Read-Host 'Turno especial: S o N'
if ([string]::IsNullOrWhiteSpace($name) -or [string]::IsNullOrWhiteSpace($schedule)) { throw 'Nombre y horario son obligatorios.' }
if ($special -notmatch '^[sSnN]$') { throw 'Turno especial debe ser S o N.' }
New-Item -ItemType Directory -Path (Join-Path $folder 'fotos') -Force | Out-Null
[ordered]@{Number=$number;Name=$name;Area=$area;Position=$position;WorkSchedule=$schedule;SpecialShift=($special -ieq 'S');EnabledForDemo=$true} | ConvertTo-Json | Set-Content -Path $path -Encoding UTF8
Write-Host 'Perfil guardado en .local\empleado.json, excluido de GitHub.'
Write-Host 'Se habilita este empleado para la prueba local; no interpreta automaticamente EMP_STATUS de RH.'
Write-Host 'Reinicie la aplicacion para agregarlo a la base. Los registros existentes no se borran ni sobrescriben.'
Write-Host 'Para mostrar su foto, copie un JPG o PNG a .local\fotos con el numero de empleado como nombre.'
