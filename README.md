# Sistema Integral de Comedores

Primer incremento de una aplicación nueva en ASP.NET Core 10, Razor Pages, C#, Entity Framework Core y PostgreSQL. Las capturas y el VB6 se usan como referencia funcional. No se importaron empleados ni credenciales de los archivos enviados.

## Funcionalidad disponible

- Acceso y cierre de sesión con ASP.NET Core Identity, contraseña con hash, bloqueo por intentos y protección antifalsificación en formularios.
- Consulta de empleado y registro de consumo según IP autorizada y horario del comedor.
- Consecutivo diario del empleado entre todos los comedores; desde el segundo se identifica como adicional, incluyendo turno especial. Se guarda el turno especial del momento, operador y ubicación. No se calculan cobros ni exenciones.
- Reenvíos de una misma solicitud no duplican consumos. Registros simultáneos de un empleado se serializan en PostgreSQL.
- Reporte por fechas inclusivas, comedor/GLOBAL y categoría, resumen de cantidades y todas las horas por empleado/día/comedor, adicionales y turno especial. Detalle de cada consumo e impresión mediante el navegador (incluye guardar PDF).

## Ejecutar en desarrollo

Requisitos: SDK .NET 10, Docker con daemon activo, Python 3 para las comprobaciones. En este entorno el SDK está en `/workspace/.dotnet`; el script lo activa si es necesario.

Desde la raíz del repositorio:

```bash
./scripts/dev.sh
```

El script inicia PostgreSQL 17, espera a que esté disponible e inicia la aplicación en el puerto local 5080. Usuario: `demo`. La contraseña aleatoria se conserva en `.local/demo-password`, archivo ignorado por Git. No publicar ese archivo ni sus valores. PostgreSQL está expuesto exclusivamente en loopback con autenticación de confianza para este desarrollo; no es una configuración de producción.

Empleados ficticios: `40001` (sin turno especial), `49001` (con turno especial). El script asigna una IP demo de SUBESTACION; la anulación de IP solo funciona en Development. Para probar otro comedor: `Demo__TerminalIp=172.16.60.54 ./scripts/dev.sh`. Por defecto se usa la hora real del servidor convertida a America/Mexico_City.

Los consumos de prueba persisten en el contenedor. No eliminarlo sin respaldo si interesa conservarlos. Las tablas se crean con `EnsureCreated` únicamente en Development; los cambios futuros al esquema necesitan migraciones antes de usar datos reales.

## Validar

```bash
export PATH="/workspace/.dotnet:$PATH"
export DOTNET_CLI_HOME=/workspace/.dotnet-home
export NUGET_PACKAGES=/workspace/.nuget/packages
dotnet build src/Comedores.Web
dotnet run --project tests/Comedores.Checks
```

Las comprobaciones crean una base temporal independiente, prueban transacciones reales y la eliminan al terminar. No modifican la base de desarrollo. Requieren PostgreSQL iniciado por `dev.sh`.

Para la prueba HTTP, arrancar el servidor con una hora de prueba dentro del desayuno:

```bash
Demo__Now=2026-10-19T14:30:00Z ./scripts/dev.sh
python3 scripts/smoke.py
```

Esta prueba sí agrega un consumo del empleado ficticio 40001 a la base de desarrollo. Comprueba login, autorización, consulta, registro, idempotencia y filtros. Al terminar, reiniciar sin `Demo__Now` para volver a la hora real. Las anulaciones Demo no funcionan fuera de Development.

## Reglas confirmadas

- SUBESTACION y PLATAFORMA: desayuno 08:00–11:00, comida 14:00–18:00, cena 20:30–22:00.
- CREI: desayuno 08:00–11:00, comida 14:00–18:00, cena 19:00–22:00. Se incluyen los extremos.
- Corporativo: número menor de 40000; Operativo: 40000–48999; Mando: desde 49000.
- El primer consumo del día es el primero; los siguientes son adicionales. Se permiten repetidos. Otra área determina calificación y cobro.
- IP conocidas: SUBESTACION 172.16.66.230; PLATAFORMA 172.16.60.54; CREI 172.16.70.68, 172.18.208.1 y 172.18.219.13. PRB, JONAS y ANTES_SUB quedan sin habilitar hasta confirmar su correspondencia.
- No se confía en cabeceras X-Forwarded-For arbitrarias. En despliegue con proxy será necesario configurar proxies confiables y verificar que la IP del cliente se conserve.

## Pendientes antes del uso real

- Confirmar el significado de EMP_STATUS (no importar 0/1 como activo sin confirmación). Los empleados ficticios tienen habilitación explícita.
- Definir e implementar validación del horario laboral individual de RH, integración/actualización del catálogo y fotos.
- Captura extemporánea con autorización y motivo; definir cómo reordenar consumos y adicionales cuando se registra una fecha pasada.
- Administración de usuarios, roles y permisos por módulo. En este incremento todos los usuarios autenticados tienen las mismas funciones de operador.
- Migraciones versionadas, pruebas de despliegue, gestión de secretos, HTTPS, configuración de proxy y respaldos. No desplegar esta configuración de desarrollo a la red del AICM.
- Confirmar licencia del código propio con la organización antes de publicarlo. El stack es de código abierto; no se ha asignado una licencia al código institucional sin autorización.

El logo institucional no se incluye: usar un recurso autorizado. La cabecera emplea texto en esta versión. Las dependencias mantienen las licencias de sus autores.

## Guías para compartir

En [docs](docs/README.md) están la guía rápida ilustrada para el equipo y la propuesta para presentar el avance a jefatura, en PDF, Word y lectura web.

## Perfil propio y fotografías

Se puede agregar un perfil local y mostrar JPG/PNG sin subir datos a GitHub. Consulta [Perfil y fotos](docs/PERFIL-Y-FOTOS.md). Los perfiles existentes no se sobrescriben y la carpeta `.local` está excluida del repositorio.
