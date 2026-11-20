# SICOM: contador del servicio y administración de registros

## Comportamiento

- La barra muestra los **servicios registrados del comedor, fecha local y servicio vigente**, incluyendo registros individuales, comida propia y encargos de compañeros. Un grupo de cuatro personas suma cuatro servicios, no un solo grupo.
- Para Operador el servicio se determina por la hora del servidor y los horarios configurados del comedor. Cambia el total mostrado al siguiente servicio y al siguiente día; conserva los registros históricos. Fuera del horario muestra «— / Sin servicio vigente».
- Para Coordinador el contador sigue el comedor y servicio seleccionados, igual que su registro manual. No mezcla otros comedores o servicios. La fecha corresponde al servidor, incluida la hora simulada de desarrollo.
- Actualiza después de guardar, al cambiar selección, al recuperar el foco y cada 15 segundos para recoger cambios de otros equipos. Un fallo de conexión muestra «—», no un cero que pudiera confundirse con un conteo válido.
- En **Reportes**, «Empleado · número o nombre» busca un número exacto de hasta cinco dígitos o parte del nombre sin distinguir mayúsculas. Conserva los otros filtros y también filtra las exportaciones CSV.
- En **Ver detalle de cada consumo**, Coordinador y Administrador tienen **Eliminar**. Se confirma en un diálogo con motivo obligatorio de 5 a 300 caracteres. Operador no tiene el botón y el servidor rechaza intentos enviados directamente; Catálogo no tiene acceso a reportes.
- Eliminar descuenta el consumo de reportes, exportaciones, contador y avisos de repetición. Es una baja lógica: conserva el registro, folio, consecutivo, fecha real de eliminación, responsable y motivo. El movimiento aparece en el historial de seguridad de Usuarios y perfiles. No modifica el resto de una recogida, ni renumera registros históricos.
- Reenviar una solicitud individual eliminada no la vuelve a crear. Reenviar un grupo previamente procesado informa cuántos servicios siguen vigentes y cuántos se eliminaron, sin recrearlos. Para registrar nuevamente se necesita una solicitud nueva.

## Integrar en Windows sin reemplazar todo el proyecto

Primero detén la aplicación con **Ctrl+C**. Extrae por completo `sicom-contador-reportes.zip` en una carpeta separada; contiene `archivos`, `scripts` y esta guía. No copies el proyecto completo de GitHub encima de tu carpeta local.

Desde PowerShell, entra a la carpeta extraída del paquete y ejecuta:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\actualizar-contador-reportes.ps1 -Proyecto 'C:\Proyectos\COMEDORES_WEB\comedoresWEB-desarrollo-primera-version-web'
```

El instalador valida los puntos de integración antes de modificar archivos; respalda el código existente en `.local\respaldos-codigo\contador-reportes-FECHA`, copia únicamente los archivos del cambio e integra cuatro ajustes puntuales en tu `Program.cs`. Conserva sus otros ajustes, incluidos el manejo de IP por proxy. No modifica `iniciar-windows.ps1`, cadenas de conexión, contraseñas, fotos ni los archivos privados de `.local`.

Si tu `Program.cs` tiene una estructura distinta, el instalador se detiene antes de copiar. Pide a Codex local integrar los cuatro cambios indicados abajo conservando sus modificaciones actuales.

Después inicia con tu comando habitual, usando la copia local:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\iniciar-windows.ps1 -DbHost 127.0.0.1 -DbPort 5432 -DbName comedores_pruebas -DbUser comedores_dev -SinPerfilLocal -Comedor PLATAFORMA -FechaHoraPrueba '2026-11-20T15:30:00-06:00'
```

Este comando se ejecuta desde la carpeta **del proyecto**, no desde la carpeta del paquete. Los parámetros de conexión corresponden a tu script local mejorado; esta actualización lo conserva. No cambia la contraseña de las cuentas existentes.

Al iniciar se agregan cuatro columnas a `Consumptions` si faltan y un índice para el conteo. No vacía ni restaura tablas. Primero verifica en `comedores_pruebas`; publicar en Ubuntu es un paso separado y requiere conservar su configuración privada y el manejo local de `ForwardedHeaders`.

## Cuatro cambios de Program.cs para integración manual

1. Dentro de `AddAuthorization`, agregar:

```csharp
o.AddPolicy("Reports",p=>p.RequireRole(AccessProfiles.Operator,AccessProfiles.Coordinator,AccessProfiles.Admin));
```

2. Cambiar únicamente la política de la página de reportes:

```csharp
o.Conventions.AuthorizePage("/Reports","Reports");
```

3. Junto al registro de `PickupService`, agregar:

```csharp
builder.Services.AddScoped<ConsumptionManagementService>();
```

4. Después de las actualizaciones aditivas de `PickupBatches` y antes de obtener `RoleManager`, en el bloque con `securityDb`, agregar:

```csharp
await ConsumptionManagementService.EnsureSchemaAsync(securityDb);
```

## Verificación

Compilación .NET sin errores. 116 comprobaciones de lógica en bases temporales, incluyendo actualización aditiva de una tabla anterior con datos, separación por horario/fecha/comedor, permisos de eliminación, auditoría, consecutivos y reintentos. Prueba de navegador `tests/browser/service-counter-reports.cjs`: grupos incluidos en contador, cambio de selección, búsqueda y CSV, cancelación, eliminación por ambos perfiles autorizados, rechazo del operador, exclusión de Catálogo y cuatro anchos de pantalla. `capture-experience.cjs` comprueba la captura y el foco de los módulos existentes. `server-clock.cjs` verifica avance del reloj, corte a Cena, medianoche, desconexión/recuperación y hora simulada.

El instalador PowerShell se entrega para Windows; no se ha ejecutado en este entorno Linux. Conserva el respaldo que genera al aplicarlo.
