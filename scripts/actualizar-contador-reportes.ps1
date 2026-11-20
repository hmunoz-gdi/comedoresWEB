param([string]$Proyecto = (Get-Location).Path)
$ErrorActionPreference = 'Stop'
$Proyecto = (Resolve-Path -LiteralPath $Proyecto).Path
$paquete = Join-Path $PSScriptRoot '..\archivos'
$programPath = Join-Path $Proyecto 'src\Comedores.Web\Program.cs'
if (!(Test-Path -LiteralPath $paquete) -or !(Test-Path -LiteralPath $programPath)) {
    throw 'Extrae todo el paquete y ejecuta este script indicando la carpeta de tu proyecto con -Proyecto.'
}
# Prepara Program.cs en memoria: conserva los ajustes locales de IP, proxy y configuración.
$program = [IO.File]::ReadAllText($programPath)
if (!$program.Contains('o.AddPolicy("Reports",')) {
    $anchor = 'o.AddPolicy("Services",'
    if (!$program.Contains($anchor)) { throw 'Program.cs tiene otra estructura. Integra los cuatro cambios de la guía antes de copiar archivos.' }
    $program = $program.Replace($anchor, 'o.AddPolicy("Reports",p=>p.RequireRole(AccessProfiles.Operator,AccessProfiles.Coordinator,AccessProfiles.Admin));' + [Environment]::NewLine + ' ' + $anchor)
}
$old = 'o.Conventions.AuthorizePage("/Reports","Services")'
$new = 'o.Conventions.AuthorizePage("/Reports","Reports")'
if ($program.Contains($old)) { $program = $program.Replace($old,$new) }
elseif (!$program.Contains($new)) { throw 'No se encontró la política de autorización de /Reports. Integra Program.cs con la guía.' }
if (!$program.Contains('builder.Services.AddScoped<ConsumptionManagementService>();')) {
    $anchor = 'builder.Services.AddScoped<PickupService>();'
    if (!$program.Contains($anchor)) { throw 'Falta el punto de integración de servicios. Revisa la guía.' }
    $program = $program.Replace($anchor,$anchor + [Environment]::NewLine + 'builder.Services.AddScoped<ConsumptionManagementService>();')
}
if (!$program.Contains('await ConsumptionManagementService.EnsureSchemaAsync(securityDb);')) {
    $anchor = 'var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();'
    if (!$program.Contains($anchor)) { throw 'No se encontró el punto de actualización de esquema. Integra Program.cs con la guía.' }
    $program = $program.Replace($anchor,'await ConsumptionManagementService.EnsureSchemaAsync(securityDb);' + [Environment]::NewLine + ' ' + $anchor)
}
$files = @(Get-ChildItem -LiteralPath $paquete -Recurse -File)
if ($files.Count -eq 0) { throw 'El paquete no contiene archivos.' }
$paquete = (Resolve-Path -LiteralPath $paquete).Path
$backup = Join-Path $Proyecto ('.local\respaldos-codigo\contador-reportes-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$utf8 = New-Object System.Text.UTF8Encoding($false)
# Respalda todos los archivos que se van a reemplazar antes de modificar el proyecto.
foreach ($file in $files) {
    $relative = $file.FullName.Substring($paquete.Length).TrimStart([char[]]'\/')
    $target = Join-Path $Proyecto $relative
    if (Test-Path -LiteralPath $target) {
        $saved = Join-Path $backup $relative
        New-Item -ItemType Directory -Path (Split-Path $saved) -Force | Out-Null
        Copy-Item -LiteralPath $target -Destination $saved
    }
}
New-Item -ItemType Directory -Path (Join-Path $backup 'src\Comedores.Web') -Force | Out-Null
Copy-Item -LiteralPath $programPath -Destination (Join-Path $backup 'src\Comedores.Web\Program.cs')
foreach ($file in $files) {
    $relative = $file.FullName.Substring($paquete.Length).TrimStart([char[]]'\/')
    $target = Join-Path $Proyecto $relative
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target -Force
}
[IO.File]::WriteAllText($programPath,$program,$utf8)
Write-Host "Actualización integrada. Respaldo del código anterior: $backup"
Write-Host 'Inicia con tu script habitual y tu base comedores_pruebas. La actualización no cambia conexiones, contraseñas ni fotos.'
