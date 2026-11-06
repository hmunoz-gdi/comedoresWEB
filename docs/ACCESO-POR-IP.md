# Operadores exclusivos de su comedor por IP

## Regla de acceso

Después de comprobar la contraseña, el sistema consulta la IP real de la conexión del navegador en **Terminals**. Obtiene el comedor mediante **DiningRoomId** y lo compara con el comedor asignado al usuario (claim `ComedorId` de Identity).

- IP registrada para el mismo comedor de la cuenta: permite iniciar sesión.
- IP registrada para otro comedor: muestra **Equipo no autorizado** y no inicia sesión.
- IP no registrada o cuenta sin comedor: rechaza el acceso y pide revisión a Mesa de Ayuda, extensión 2442.

Un rechazo por IP con contraseña correcta no se cuenta como contraseña errónea. La regla también se comprueba en cada petición de una sesión existente: cambiar la asignación de la cuenta o de la IP no deja una sesión anterior con permisos para el otro comedor. Los handlers de registro conservan su propia validación.

Esta restricción corresponde al perfil **Operador**. Catálogo conserva el acceso a empleados y fotos; Coordinador conserva registro manual de todos los comedores; la cuenta administrativa permite configurar el sistema. No se cambia automáticamente el comedor de una cuenta para hacerlo coincidir con el equipo.

Varias IP pueden pertenecer a CREI: cualquier terminal autorizada para CREI admite cuentas asignadas a CREI. La cuenta no queda ligada a una única fila de terminal, sino al comedor.

## Consultar la configuración

En pgAdmin, sobre la base `comedores`:

```sql
SELECT t."Id", t."Ip", d."Id" AS "ComedorId", d."Name" AS "Comedor"
FROM "Terminals" t
JOIN "DiningRooms" d ON d."Id"=t."DiningRoomId"
ORDER BY d."Name", t."Ip";
```

Para consultar asignaciones de cuentas:

```sql
SELECT u."UserName", d."Id" AS "ComedorId", d."Name" AS "Comedor"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserClaims" c ON c."UserId"=u."Id" AND c."ClaimType"='ComedorId'
LEFT JOIN "DiningRooms" d ON d."Id"::text=c."ClaimValue"
ORDER BY u."UserName";
```

Los permisos de cada cuenta se administran en **Usuarios y perfiles**. Las asignaciones de IP se mantienen en **Terminals**. No se agregan ni reasignan IP automáticamente.

## Arrancar para comprobar IP real en Windows

Actualizar **src y scripts**. Desde una terminal abierta en `scripts`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-windows.ps1 -AccesoRed -FechaHoraPrueba "2026-11-06T15:30:00-06:00" -RutaFotos '\\172.16.2.23\comedor\Fotos'
```

El comando no lleva `-Comedor`: usa la IP real. `-AccesoRed` permite escuchar conexiones LAN en el puerto 5080. La terminal confirma **IP REAL**. Deben estar autorizadas la conectividad y la regla de firewall correspondiente por Soporte; el script no cambia el firewall.

En el equipo del comedor, abrir **http://IP-DEL-SERVIDOR:5080**. El servidor obtiene la IP del cliente que abre el navegador. Si servidor y navegador están en el mismo equipo, utilizar también su dirección LAN para esta comprobación.

**localhost no es la IP LAN**: se identifica como 127.0.0.1 o ::1. No registrar una IP de localhost ni de un proxy como sustituto de todas las terminales. Con la base inicial, esas direcciones no están autorizadas para operadores. La cuenta `demo` permite entrar para administrar aunque el equipo no sea un comedor.

## Pruebas explícitas en una sola computadora

Se conserva `-Comedor CREI` (o SUBESTACION/PLATAFORMA) para simular una terminal en desarrollo. Sólo se utiliza si se proporciona expresamente; ya no se simula SUBESTACION por defecto. La terminal y la página anuncian **IP SIMULADA DE PRUEBA**. Ese modo prueba el flujo del comedor, pero no verifica la IP física del equipo. La fecha simulada es independiente de la IP simulada.

Los valores `Demo:TerminalIp` se ignoran fuera del entorno Development. El script Windows es una herramienta de desarrollo; el alojamiento operativo debe configurarse mediante los servicios del servidor.

## Proxy y despliegue futuro

La aplicación no confía por sí sola en cabeceras `X-Forwarded-For` o `Forwarded` aportadas por el cliente. Si se coloca Nginx u otro proxy delante, se debe configurar y validar expresamente el procesamiento de cabeceras sólo desde los proxies conocidos. Sin esa configuración, la aplicación ve la conexión del proxy y puede rechazar al operador. No autorizar la IP del proxy como si correspondiera a todos los comedores.

## Verificación

`tests/browser/operator-ip.cjs` utiliza una base aislada y la IP real local de conexión de Chromium. Verifica IP no registrada, comedor distinto, coincidencia, registro en el comedor correcto, cabeceras falsificadas y revalidación/rechazo de una cookie anterior tras reasignar una terminal. La IP loopback registrada en esa prueba es una fixture temporal y no una recomendación de configuración operativa.
