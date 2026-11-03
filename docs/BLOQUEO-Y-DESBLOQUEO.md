# Inicio de sesión, bloqueo y Mesa de Ayuda

Los logos institucionales aparecen dentro del recuadro de inicio de sesión. No se muestra la cabecera exterior en esta pantalla.

## Tres intentos fallidos

- Después de tres contraseñas incorrectas consecutivas, la cuenta queda bloqueada.
- Un acceso correcto antes del tercer fallo reinicia el contador.
- Los intentos simultáneos se procesan en orden por cuenta.
- Cerrar el navegador, reiniciar la aplicación o esperar no retira el bloqueo.
- La contraseña correcta tampoco permite entrar mientras la cuenta permanezca bloqueada.
- Las sesiones abiertas de la cuenta se invalidan al bloquearla.

La pantalla indica: **Cuenta bloqueada. Comunícate con la Mesa de Ayuda del AICM, extensión 2442, para solicitar el desbloqueo.** No se envían llamadas, correos o tickets automáticamente.

El mensaje de acceso fallido también muestra la extensión de ayuda, pero no indica cuántos intentos quedan ni confirma cuentas inexistentes.

## Control del administrador

1. La Mesa de Ayuda verifica la identidad del solicitante y registra su folio de atención.
2. Entra con la cuenta de administración y abre **Usuarios y perfiles**.
3. Localiza la cuenta con estado **Bloqueada**.
4. Escribe el motivo o folio, por ejemplo “Identidad verificada, folio 12345”.
5. Pulsa **Desbloquear cuenta**.

El usuario conserva su contraseña. El contador vuelve a cero y se invalidan las sesiones anteriores. Los perfiles Operador, Catálogo y Coordinador no tienen permiso para desbloquear cuentas. Cambiar de perfil no desbloquea una cuenta.

En la misma pantalla se muestran los últimos 50 movimientos de bloqueo/desbloqueo, con fecha y hora de Ciudad de México, cuenta, responsable y motivo. Los movimientos se conservan en PostgreSQL, en `AccountSecurityEvents`; no se eliminan al reiniciar la aplicación. No se registran contraseñas.

## Si se bloquea la propia cuenta administradora

No hagas las primeras pruebas de bloqueo con `demo`: usa una cuenta operativa de prueba.

Si `demo` queda bloqueada y no hay otro administrador disponible, quien tenga acceso administrativo local a PostgreSQL puede recuperarla:

1. Abre pgAdmin y selecciona la base **comedores**.
2. Abre **Query Tool / Herramienta de consultas**.
3. Abre el archivo `scripts/desbloquear-admin.sql` de esta versión.
4. Revisa que el script apunta únicamente a `DEMO` y ejecútalo.
5. Vuelve a iniciar sesión con su contraseña habitual.

Este procedimiento no cambia contraseñas ni perfiles; desbloquea únicamente `demo` y deja un movimiento de recuperación local en la bitácora. Requiere acceso administrativo a la base, no está expuesto en la pantalla pública de inicio de sesión. El archivo debe ejecutarse después de arrancar esta versión, para que exista la tabla de seguridad.

Para otro nombre de administrador se necesita un procedimiento supervisado por Sistemas; no amplíes el UPDATE a todas las cuentas.

## Actualización

Detén la aplicación y reemplaza `src` y `scripts`. Arranca normalmente y recarga con Ctrl+F5. Se añade únicamente la tabla de movimientos de seguridad mediante `CREATE TABLE IF NOT EXISTS`, sin borrar ni recrear tablas existentes. El usuario de conexión necesita permiso para crear esa tabla durante la actualización. Las cuentas conservan sus contraseñas y perfiles.

La política también se aplica a cuentas existentes. La validación de intentos usa las tablas de Identity actuales. El reinicio del catálogo de empleados no elimina la bitácora de seguridad.

## Verificación

`tests/browser/lockout.cjs`, sobre una base aislada, comprueba logos interiores, bloqueo en el tercer intento, reinicio del contador al acertar, denegación aun con contraseña correcta, revocación de sesiones, rechazo de desbloqueo sin motivo o sin permiso, bitácora y concurrencia. La recuperación SQL y la creación de la tabla en una instalación existente se prueban en una base local desechable.
