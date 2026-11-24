# Renombrar la misma cuenta demo a sagdi

Objetivo: usar **sagdi** tanto en local como en producción, conservando la cuenta existente. No crear otro usuario, no copiar una base sobre la otra ni sincronizar contraseñas o consumos entre ambientes.

## Qué cambia y qué se conserva

Se actualizan únicamente `UserName` a `sagdi`, `NormalizedUserName` a `SAGDI` y los sellos de seguridad/concurrencia. Los sellos nuevos obligan a renovar las sesiones para que no sigan mostrando `demo`.

Se conserva el mismo `Id`, el mismo hash y contraseña, correo, estado/bloqueo, intentos fallidos, opciones de segundo factor y todas las relaciones por `UserId`: roles, claims, comedor, tokens almacenados e inicios externos. No se modifican empleados, fotografías ni consumos.

El historial anterior no se reescribe: movimientos hechos como `demo` siguen apareciendo con ese nombre. Los nuevos movimientos se atribuyen a `sagdi`. El renombre genera su propio movimiento de auditoría, identificado por el usuario de PostgreSQL que lo ejecutó.

El `Id` se conserva **dentro de cada base**. Local y producción pueden tener cantidades de usuarios, identificadores o contraseñas diferentes; esta operación no los iguala ni reemplaza.

## Código que debe acompañar el cambio

- `AccessProfiles` centraliza el nombre administrativo `sagdi`.
- El arranque de una base de desarrollo nueva crea sólo `sagdi`.
- Si una base existente aún tiene `demo`, el arranque no crea además `sagdi`. No renombra automáticamente la cuenta: se usa el procedimiento explícito.
- Si existen ambos nombres, se detiene la inicialización de desarrollo para revisar el conflicto; no se fusionan cuentas ni se sustituyen permisos.
- La autorización especial para restablecer contraseñas pasa de `demo` administrador a **sagdi administrador**, manteniendo la restricción original a esa cuenta. Los otros administradores no adquieren ese permiso.
- El texto de Usuarios y perfiles solicita «Tu contraseña administrativa actual».
- El script Windows sólo cambia sus mensajes de usuario. Al integrarlo en la copia local, conservar sus parámetros particulares (`DbHost`, `DbName`, `SinPerfilLocal`, etc.), que no necesariamente existen en la versión del repositorio.
- Se conservan los nombres de configuración `Demo:Password`, `Demo__Password` y el archivo privado `.local/demo-password`: son mecanismos existentes del entorno de pruebas; no determinan que se cree una cuenta llamada demo y no deben recrearse ni publicarse.

## Orden en ambos ambientes

**No se ha ejecutado sobre las bases del usuario.** Se sigue el acuerdo de dar un paso, esperar el resultado y continuar.

1. Preparar el código actualizado y el paquete Linux desde una copia temporal de la fuente local, conservando el manejo particular de `ForwardedHeaders` y HTTP.
2. Consultar en cada base si existe `demo` o `sagdi`, su identificador y sus roles, sin mostrar hashes ni contraseñas. Si ambos existen o falta la cuenta esperada, detenerse para revisar.
3. En local: detener la aplicación, respaldar y verificar `comedores_pruebas`; integrar/compilar el código compatible; ejecutar el renombre explícito y arrancar. Verificar ingreso y permiso de restablecimiento como `sagdi` con la contraseña habitual.
4. En producción: con el mismo código ya validado, preparar una pausa breve; detener el servicio, respaldar código/configuración/estado y la base actual, comprobar el respaldo, instalar el paquete compatible, ejecutar el renombre e iniciar el servicio.
5. Comparar en cada base el mismo identificador antes/después y los mismos roles/claims. El número de cuentas no debe aumentar ni disminuir. Comprobar que no hay una cuenta `demo` nueva y que los historiales previos permanecen.
6. Iniciar sesión nuevamente como **sagdi**, con la contraseña que esa cuenta ya tenía en ese ambiente. No ejecutar registros ficticios en producción para comprobar el acceso.

La ventana de código y SQL debe hacerse junta: la versión anterior comprobaba literalmente `demo` para permitir restablecimientos y podía recrear ese usuario en desarrollo. No arrancar la versión anterior en local después del renombre.

## Procedimiento SQL

Archivo: `scripts/sql/renombrar-demo-sagdi.sql`.

El script exige indicar `base_objetivo`, compara ese valor con la conexión real, usa una transacción y bloqueos, comprueba que el destino esté libre y verifica que no cambió ningún campo ajeno al renombre. Nunca crea ni elimina usuarios. No muestra hashes, contraseñas, sellos ni tokens en su salida.

Si ya se ejecutó correctamente en esa cuenta, repetirlo reconoce su registro de auditoría y no vuelve a renombrar ni cerrar sesiones. Si `sagdi` existe sin evidencia de este procedimiento, se detiene para revisión; no supone que sea la misma cuenta.

Estos comandos son referencias para el paso acordado, **no para ejecutar antes del respaldo y de tener el código compatible**.

Local, desde la carpeta del proyecto en PowerShell:

```powershell
& 'C:\Program Files\PostgreSQL\17\bin\psql.exe' -h 127.0.0.1 -p 5432 -U comedores_dev -d comedores_pruebas -W -v ON_ERROR_STOP=1 -v base_objetivo=comedores_pruebas -f .\scripts\sql\renombrar-demo-sagdi.sql
```

Servidor: transferir primero el SQL revisado a la carpeta de migración. El TAR de la aplicación no incluye por sí mismo los scripts de fuente:

```bash
sudo -u postgres /usr/lib/postgresql/17/bin/psql -p 5433 -d comedores -v ON_ERROR_STOP=1 -v base_objetivo=comedores -f /Almacenamiento/comedores-migracion/renombrar-demo-sagdi.sql
```

Conservar los respaldos por separado. Si hiciera falta volver al nombre anterior, detener la aplicación y preparar una reversión puntual auditada del nombre y de los sellos, con el mismo identificador y su contraseña intacta. No restaurar automáticamente una base completa: podría descartar registros reales posteriores al respaldo. Una reversión manual también requiere revisar la evidencia del renombre antes de volver a ejecutar el procedimiento.

## Validación en desarrollo

- Compilación sin errores ni advertencias; 123 comprobaciones generales en bases temporales.
- `tests/browser/rename-account.cjs` crea y elimina su propia base: verifica creación inicial sagdi, compatibilidad con una cuenta demo existente sin duplicación, rechazo de base incorrecta o destino ocupado, conservación de identificador/hash/roles/claims/tokens/logins/bloqueo, normalización del nombre, rechazo del nombre anterior, sesiones revocadas y acceso con la misma contraseña. Verifica también permiso de restablecimiento, historial intacto, idempotencia y reinicios.
- `tests/browser/password-reset.cjs` valida con sagdi la reautenticación, la restricción frente a otros administradores, el restablecimiento de otra cuenta y el cambio propio, conservación de perfiles y bloqueo y ausencia de contraseñas en la bitácora.
- Las suites de navegador utilizan sagdi como cuenta inicial. El archivo privado de contraseña de pruebas conserva su nombre existente.
