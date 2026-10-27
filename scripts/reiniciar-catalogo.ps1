param([string]$CsvPath)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
if (Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue) {
    throw 'Primero detenga la aplicacion con Ctrl+C en su terminal. No cierre PostgreSQL.'
}
if (-not $CsvPath) {
    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = 'Seleccione cat-empleados.csv'
    $dialog.Filter = 'Catalogo de empleados (*.csv)|*.csv'
    if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { Write-Host 'Cancelado. No se modifico la base.'; return }
    $CsvPath = $dialog.FileName
}
$CsvPath = (Resolve-Path -LiteralPath $CsvPath).Path
$pgBin = Join-Path $env:ProgramFiles 'PostgreSQL\17\bin'
if (-not (Test-Path (Join-Path $pgBin 'pg_dump.exe')) -or -not (Test-Path (Join-Path $pgBin 'pg_restore.exe'))) {
    throw 'No se encontraron pg_dump y pg_restore de PostgreSQL 17. No se modifico la base.'
}
dotnet restore tools/Comedores.Catalogo --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Fallo la preparacion de la herramienta. No se modifico la base.' }
dotnet run --project tools/Comedores.Catalogo --no-restore -- preview $CsvPath
if ($LASTEXITCODE -ne 0) { throw 'El CSV no paso la validacion. No se modifico la base.' }
Write-Host 'Se respaldara la base LOCAL comedores y se borraran solo empleados y consumos para cargar el CSV.'
Write-Host 'Se conservan usuarios, permisos, comedores, equipos, horarios y fotografias locales.'
$oldConnection = $env:ConnectionStrings__Comedores
$oldBin = $env:COMEDORES_PG_BIN
$secret = Read-Host 'Contrasena de PostgreSQL para postgres (no se muestra)' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    $quoted = $password.Replace('"','""')
    $env:ConnectionStrings__Comedores = 'Host=127.0.0.1;Port=5432;Database=comedores;Username=postgres;Password="' + $quoted + '"'
    $env:COMEDORES_PG_BIN = $pgBin
    dotnet run --project tools/Comedores.Catalogo --no-restore -- reset $CsvPath
    if ($LASTEXITCODE -ne 0) { throw 'No se completo la operacion. Conserve la salida de esta terminal sin contrasenas.' }
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    $password = $null; $quoted = $null
    $env:ConnectionStrings__Comedores = $oldConnection
    $env:COMEDORES_PG_BIN = $oldBin
}
