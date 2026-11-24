# Publicación de SICOM en 172.16.2.12

Preparado el viernes 20 de noviembre de 2026. Aplicación prevista: **lunes 23 de noviembre de 2026**, en una ventana acordada sin capturas. **Esta guía no ejecuta acciones ni confirma el estado actual del servidor.** Sus rutas corresponden a la instalación que se verificó anteriormente; se volverán a comprobar el lunes.

Actualización del 24/11/2026: la versión a publicar debe incluir también **Auditoría**, el crédito trasladado a la barra lateral y el ajuste preparado de CREI. Usar la versión actual integrada de `desarrollo/primera-version-web`; no reutilizar un paquete anterior limitado a `66b98f5`. Consultar `docs/AUDITORIA-Y-CREI.md`.

## Qué se publicará

Código y recursos de SICOM para Linux x64 / .NET 10. Incluye contador por servicio/comedor/fecha, encargos dentro del conteo, consulta de grupos por folio, búsqueda en reportes, eliminación auditada para coordinadores/administradores, avisos de empleado inexistente y dado de baja, e identidad visual y crédito del área desarrolladora.

No se copiará la base `comedores_pruebas` a producción. La base real `comedores`, usuarios, contraseñas, terminales y fotografías se conservan. La hora y la IP simuladas son sólo para desarrollo. DNS y certificado quedan pendientes según lo acordado; se mantiene el acceso por `http://172.16.2.12`.

## 1. Preparación en Windows con Codex local

No reemplazar todo el proyecto con un ZIP. Pedir a Codex local que prepare una publicación desde **la copia local integrada**, conservando sus ajustes y respaldándolos. La fuente de GitHub no contiene necesariamente todos los ajustes particulares de la instalación del usuario.

Antes de generar el paquete:

1. Confirmar que están integrados los commits `4fb9b06`, `f66657a`, `8d9c84f` y `66b98f5`, o cambios equivalentes completos.
2. Verificar el tratamiento de `ForwardedHeaders` indicado abajo. Una integración anterior informó que `Program.cs` local no lo contenía: **no darlo por hecho**.
3. Compilar y revisar los módulos con la base local `comedores_pruebas`.
4. Crear la publicación en una **copia temporal separada**, para no modificar `packages.lock.json`, `obj` ni el arranque de Windows. Una publicación Linux puede cambiar los identificadores de ejecución del archivo de bloqueo y provocar NU1004 al volver a iniciar localmente.
5. Publicar con `dotnet publish` en Release para `linux-x64`, dependiente del runtime, sin apphost. El servidor ya tenía .NET 10 instalado; se comprobará de nuevo.
6. Excluir del paquete `.local`, respaldos, fotos privadas, dumps y todos los `appsettings*.json` privados. El archivo de producción del servidor será conservado. No copiar contraseñas a argumentos ni logs.
7. Entregar un TAR, SHA256 y manifiesto de la fuente utilizada. No transferir ni instalar sin el paso acordado con el usuario.

Comando de publicación, **desde la copia temporal ya preparada**:

```powershell
dotnet publish .\src\Comedores.Web\Comedores.Web.csproj -c Release -r linux-x64 --self-contained false -p:UseAppHost=false -p:RestoreLockedMode=false --force -o .\publicacion-linux
```

Este comando sólo es para la copia temporal. El archivo de producción no viaja en el paquete. Después de revisar y retirar los archivos de configuración de esa salida, crear:

```powershell
tar.exe -czf .\sicom-linux-20261123.tar.gz -C .\publicacion-linux .
Get-FileHash .\sicom-linux-20261123.tar.gz -Algorithm SHA256
```

Guardar el hash y la identificación de los cambios locales aplicados junto al TAR. El paquete real se prepara en Windows, donde están esos ajustes; esta guía no sustituye ese paquete.

### IP real de Nginx: condición previa a publicar

Revisar, y agregar si falta, la configuración equivalente a ésta en la copia de publicación. Nginx y Kestrel deben seguir comunicándose por loopback; Kestrel no se expone a la red.

```csharp
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

// Antes de builder.Build():
if (!builder.Environment.IsDevelopment()) {
    builder.Services.Configure<ForwardedHeadersOptions>(options => {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(IPAddress.Loopback);
    });
}

// Después de builder.Build(), antes de HSTS, redirección y autenticación:
if (!app.Environment.IsDevelopment()) app.UseForwardedHeaders();
```

Confiar únicamente en el proxy local. En Nginx, conservar la sobrescritura de `X-Forwarded-For` con `$remote_addr`, no aceptar una cadena arbitraria enviada por el cliente. La configuración conocida es:

```nginx
proxy_set_header Host $host;
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $remote_addr;
proxy_set_header X-Forwarded-Proto $scheme;
```

Se conserva el comportamiento HTTP de la instalación actual; no activar un puerto HTTPS ni una redirección nueva durante esta actualización.

## 2. Comprobaciones del lunes en el servidor

Conectar por SSH como se ha hecho anteriormente:

```powershell
ssh desarrollo@172.16.2.12
```

Comprobaciones de lectura, antes de modificar nada:

```bash
hostname
systemctl is-active comedores
systemctl is-active postgresql@17-main
/usr/bin/dotnet --list-runtimes
sudo nginx -t
sudo ss -ltnp 'sport = :5080'
sudo readlink -f /opt/comedores/appsettings.Production.json
sudo -u postgres /usr/lib/postgresql/17/bin/psql -p 5433 -d comedores -c 'SELECT current_database();'
curl -sS -o /dev/null -w '%{http_code}\n' http://127.0.0.1:5080/Login
curl -sS -o /dev/null -w '%{http_code}\n' http://172.16.2.12/Login
```

Esperado: servicio activo, PG17 en 5433, ASP.NET Core 10 disponible, Nginx válido, Kestrel en `127.0.0.1:5080`, configuración apuntando a `/etc/comedores/appsettings.Production.json` y Login con HTTP 200.

Revisar de manera privada, sin pegar el contenido de la conexión en el chat:

- Entorno `Production`, base `comedores`, host `127.0.0.1`, puerto `5433`, rol `comedores_app`.
- `Demo:Now` y `Demo:TerminalIp` sin valores, tanto en JSON como en variables de la unidad systemd o sus archivos de entorno. Ninguna variable debe sobreescribirlos con simulaciones.
- Fotografías: `/Almacenamiento/comedores/fotos`, legibles por `comedores`.
- Directorio de claves de protección de datos conservado bajo `/var/lib/comedores`.
- Espacio disponible en `/opt` y `/Almacenamiento`; `rsync`, `tar` y herramientas PostgreSQL 17 disponibles.
- El usuario que actualiza el esquema tiene permisos sobre sus tablas. El arranque agregará tablas/columnas e índices si faltan; **no ejecuta una restauración ni vacía la base**.

Tomar como referencia los totales reales, sin publicar datos personales:

```bash
sudo -u postgres /usr/lib/postgresql/17/bin/psql -p 5433 -d comedores -c 'SELECT count(*) AS empleados FROM "Employees"; SELECT count(*) AS usuarios FROM "AspNetUsers"; SELECT count(*) AS servicios_guardados FROM "Consumptions";'
```

Si algún punto no coincide, resolverlo antes de seguir.

## 3. Transferir y verificar el paquete el lunes

Desde la carpeta donde se generó el TAR en Windows:

```powershell
scp .\sicom-linux-20261123.tar.gz desarrollo@172.16.2.12:/Almacenamiento/comedores-migracion/
```

En Ubuntu:

```bash
sha256sum /Almacenamiento/comedores-migracion/sicom-linux-20261123.tar.gz
tar -tzf /Almacenamiento/comedores-migracion/sicom-linux-20261123.tar.gz
```

Comparar el hash con Windows. Comprobar que el listado sólo contiene la publicación, que no tiene rutas absolutas o `..`, secretos, fotos privadas ni archivos de configuración de producción. Extraer en una carpeta nueva, **nunca directamente sobre `/opt/comedores`**:

```bash
mkdir /Almacenamiento/comedores-migracion/sicom-20261123-nuevo
tar -xzf /Almacenamiento/comedores-migracion/sicom-linux-20261123.tar.gz -C /Almacenamiento/comedores-migracion/sicom-20261123-nuevo
```

Si esa carpeta ya existe, no reutilizarla: crear otra con sufijo. Confirmar `Comedores.Web.dll`, `.deps.json`, `.runtimeconfig.json`, assets y que no venga `appsettings.Production.json`.

## 4. Ventana de actualización: detener y respaldar

Los siguientes pasos **son para el lunes y se realizan acompañados**, después de verificar el paquete y avisar la pausa de capturas.

En la misma sesión SSH, elegir una carpeta única y detener SICOM:

```bash
respaldo_sicom="/Almacenamiento/comedores-migracion/respaldo-sicom-$(date +%Y%m%d-%H%M%S)"
sudo install -d -m 700 "$respaldo_sicom"
sudo systemctl stop comedores
```

Anotar la ruta exacta fuera de la terminal: será necesaria para volver a la versión anterior. Respaldar antes de copiar código:

```bash
sudo cp -a /opt/comedores "$respaldo_sicom/aplicacion"
sudo cp -a /etc/comedores "$respaldo_sicom/configuracion"
sudo cp -a /var/lib/comedores "$respaldo_sicom/estado-aplicacion"
sudo cp -a /Almacenamiento/comedores/fotos "$respaldo_sicom/fotos"
```

Generar el dump con el binario **17** y conexión a **5433**. Usar `pipefail` para no confundir un archivo vacío con un respaldo correcto:

```bash
set -o pipefail
sudo -u postgres /usr/lib/postgresql/17/bin/pg_dump -p 5433 -d comedores -Fc | sudo tee "$respaldo_sicom/comedores.dump" >/dev/null
```

Si falla cualquiera de estos pasos, no publicar. La aplicación se puede volver a iniciar con el código anterior mientras se resuelve el respaldo.

Verificar archivo y contenido, sin restaurar sobre producción:

```bash
sudo test -s "$respaldo_sicom/comedores.dump"
sudo /usr/lib/postgresql/17/bin/pg_restore --list "$respaldo_sicom/comedores.dump" | sed -n '1,20p'
```

Antes de publicar, comprobar que se puede restaurar el dump en una base temporal distinta y cotejar sus totales con los anotados en producción:

```bash
base_validacion_sicom="sicom_validacion_$(date +%Y%m%d_%H%M%S)"
sudo -u postgres /usr/lib/postgresql/17/bin/createdb -p 5433 "$base_validacion_sicom"
sudo cat "$respaldo_sicom/comedores.dump" | sudo -u postgres /usr/lib/postgresql/17/bin/pg_restore -p 5433 --dbname="$base_validacion_sicom" --no-owner --no-privileges --single-transaction --exit-on-error
sudo -u postgres /usr/lib/postgresql/17/bin/psql -p 5433 -d "$base_validacion_sicom" -c 'SELECT count(*) AS empleados FROM "Employees"; SELECT count(*) AS usuarios FROM "AspNetUsers"; SELECT count(*) AS servicios_guardados FROM "Consumptions";'
```

Verificar que la restauración terminó correctamente y que los totales coinciden. Después eliminar únicamente esa base temporal, con protección del nombre:

```bash
case "$base_validacion_sicom" in
  sicom_validacion_*) sudo -u postgres /usr/lib/postgresql/17/bin/dropdb -p 5433 "$base_validacion_sicom" ;;
  *) echo "Nombre inesperado: no se elimina ninguna base." ;;
esac
```

Nunca usar `--clean` ni el nombre `comedores` como destino de esa prueba. Si falla la restauración o los totales no coinciden, no publicar.

## 5. Instalar el código

Sólo continuar con respaldos válidos. Conservar el enlace al archivo privado de producción:

```bash
sudo rsync -a --delete --exclude=appsettings.Production.json /Almacenamiento/comedores-migracion/sicom-20261123-nuevo/ /opt/comedores/
sudo chown -hR root:comedores /opt/comedores
sudo find /opt/comedores -type d -exec chmod 750 {} +
sudo find /opt/comedores -type f -exec chmod 640 {} +
sudo test -r /etc/comedores/appsettings.Production.json
sudo readlink -f /opt/comedores/appsettings.Production.json
sudo systemctl start comedores
sudo systemctl is-active comedores
```

No modificar ni reemplazar `/etc/comedores`, `/var/lib/comedores`, las fotos, la base, la configuración de terminales ni Nginx en este paso. No ejecutar el script de arranque de Windows en Ubuntu.

El arranque aplica actualizaciones aditivas: conserva datos, agrega los campos de recogida y eliminación lógica y sus índices cuando faltan, y crea `AuditLogs` con sus índices e incorpora los consumos y eventos de seguridad anteriores sin duplicarlos. Una eliminación posterior guarda responsable, fecha y motivo; no borra físicamente el registro.

## 6. Validar antes de abrir la operación

1. Login devuelve HTTP 200 tanto por Kestrel como por `http://172.16.2.12`.
2. No hay errores de arranque o de PostgreSQL. Revisar `journalctl -u comedores -n 80 --no-pager` sin publicar credenciales o datos personales.
3. No aparece aviso de hora/IP simuladas. Fecha y hora corresponden a Ciudad de México.
4. Entrar con un operador desde una terminal real autorizada: el comedor corresponde a su IP; una cuenta de otro comedor sigue bloqueada. Un túnel SSH no sirve para comprobar esta regla porque cambia la IP vista por la aplicación.
5. Consultar empleado activo, inexistente y dado de baja. Aparecen los avisos correctos; no registrar consumos ficticios en producción para probar el diseño.
6. Consultar fotografías conocidas, tanto nombres `.jpg` como `.JPG`.
7. Revisar reloj, servicio vigente, contador y crédito del área en la barra. El contador se compara contra servicios vigentes del mismo comedor/fecha/tipo, incluyendo encargos.
8. En Reportes: fechas, comedor, búsqueda, grupos completos y folios, CSV. Operador no tiene Eliminar; coordinador y administrador sí. No eliminar registros reales sólo para demostrar el botón.
9. Comprobar acceso al módulo Auditoría sólo como administrador, filtros y CSV. La configuración de CREI se aplica aparte con el SQL revisado y respaldo previo; no deshabilita los demás comedores.
10. Verificar que los totales de empleados, usuarios y servicios físicos coinciden con el respaldo previo, antes de reabrir capturas. Las actualizaciones aditivas no deben insertar consumos.
11. Cámara: el acceso HTTP por IP conserva su limitación de navegador. El túnel SSH a localhost puede usarse para la cámara administrativa como antes; DNS/TLS se retoma después.

## 7. Volver al código anterior si falla

Si falla la aplicación antes de reabrir capturas, detenerla y restaurar **sólo código**, conservando configuración y datos:

```bash
sudo systemctl stop comedores
sudo rsync -a --delete --exclude=appsettings.Production.json "$respaldo_sicom/aplicacion/" /opt/comedores/
sudo systemctl start comedores
sudo systemctl is-active comedores
curl -sS -o /dev/null -w '%{http_code}\n' http://127.0.0.1:5080/Login
```

Las columnas aditivas permanecen; no hace falta borrarlas. **No restaurar automáticamente la base ni las fotos**: se perderían cambios posteriores al respaldo.

Si ya se utilizó la eliminación lógica en la versión nueva, no reabrir una versión antigua sin revisar compatibilidad: una versión que no conoce `IsCancelled` podría volver a contar consumos eliminados. En ese caso se conserva la pausa y se corrige el problema con revisión de datos; restaurar un dump de producción requiere evaluar expresamente los registros posteriores y acordar la recuperación.

## Texto para Codex local: preparar, sin publicar

```text
Prepara la publicación de SICOM para el lunes 23/11/2026 en 172.16.2.12.
Consulta docs/PUBLICACION-172.16.2.12.md de la rama desarrollo/primera-version-web
 de https://github.com/hmunoz-gdi/comedoresWEB.
Verifica que mi copia incluya también los cambios de Auditoría y CREI del 24/11/2026.
Consulta docs/AUDITORIA-Y-CREI.md y respalda el código.
Genera una copia temporal de la fuente para publicar Linux x64, sin modificar
mi arranque Windows, .local, conexiones, fotos, base comedores_pruebas ni lockfile original.
Verifica y agrega si falta, sólo en esa copia de publicación, ForwardedHeaders
para Nginx por loopback, ForwardLimit=1 y sin confiar proxies externos.
Compila y prepara un TAR Release dependiente de .NET 10, sin appsettings privados,
con SHA256 y manifiesto de los ajustes incluidos. No uses fecha ni IP simuladas.
No conectes, transfieras, despliegues ni modifiques el servidor todavía.
Informa la ruta del paquete y cualquier requisito pendiente antes del lunes.
```
