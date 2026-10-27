# Empezar desde cero con el catálogo de RH

Procedimiento autorizado: respaldar la base LOCAL `comedores`, vaciar consumos, sustituir empleados por el CSV y conservar cuentas, contraseñas de acceso, permisos, comedores, equipos y horarios. No borra la base, no modifica SQL Server y no elimina fotografías. No ejecutar de nuevo después de comenzar a registrar servicios reales sin decidir expresamente que se desean borrar.

## 1. Preparar la actualización

1. En la terminal donde corre la aplicación, presionar Ctrl+C. Mantener PostgreSQL iniciado.
2. Descargar la versión actualizada del repositorio, rama `desarrollo/primera-version-web`: Code → Download ZIP.
3. Extraer en una carpeta nueva y abrirla en VSCodium. Deben existir `tools/Comedores.Catalogo` y `scripts/reiniciar-catalogo.ps1`.
4. Conservar la carpeta anterior. Si contiene `.local`, copiarla a la nueva carpeta para conservar fotos y archivos locales; no sobrescribir archivos distintos. La base PostgreSQL permanece en el servidor y no necesita copiarse.
5. Tener disponible el CSV original `cat-empleados.csv` en una carpeta privada. No subirlo a GitHub. El código ignora archivos CSV y respaldos para prevenir su publicación accidental.

## 2. Ejecutar una sola vez

En VSCodium, botón derecho sobre `scripts` → Open in Integrated Terminal. Pegar:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\reiniciar-catalogo.ps1
```

Se abre una ventana para elegir el CSV. Seleccionar `cat-empleados.csv`.

La herramienta muestra la validación antes de pedir la contraseña de PostgreSQL. Para el archivo revisado se espera:

- 2,237 empleados importables: 2,025 activos y 212 bajas.
- 1 fila excluida porque no tiene número de empleado.
- 2 empleados sin nombre en origen; se conservan sin inventar nombres. La pantalla avisa que el nombre no está disponible.

Escribir la contraseña del usuario `postgres` cuando se solicite; no se muestra. No es la contraseña de demo. Si no coincide el resumen esperado, detener con Ctrl+C antes de escribir la contraseña y revisar que se eligió el archivo correcto.

## 3. Qué hace automáticamente

1. Lee CSV UTF-8 con BOM opcional y separador punto y coma; respeta campos entre comillas, acentos y saltos de línea incluidos en campos.
2. Valida encabezados, longitud de filas, números, duplicados, estatus y turno especial. Una fila sin número se excluye y cuenta; los demás errores detienen la operación antes de borrar.
3. Solo permite la base `comedores` en localhost/127.0.0.1. Usa las utilidades de PostgreSQL 17 de `C:\Program Files\PostgreSQL\17\bin`.
4. Abre una transacción y bloquea escrituras de empleados y consumos durante el respaldo y el reemplazo.
5. Crea un respaldo completo de la base en formato custom mediante pg_dump y comprueba su cabecera e índice con pg_restore. Si falla, no elimina registros. No guarda la contraseña en el archivo del proyecto ni en los argumentos del proceso.
6. Borra consumos y empleados; carga el catálogo. `EMP_STATUS=1` habilita al empleado; `0` lo deja inactivo. `SI/NO` identifica turno especial. El número se conserva como texto; no se importa EMP_REG como identificador nuevo.
7. Comprueba los totales y confirma la transacción. Si falla la carga o la verificación, revierte los borrados. No toca cuentas, permisos ni configuración. Los identificadores internos no se reinician; el siguiente consumo comienza su consecutivo diario desde 1 al no existir consumos.

## 4. Comprobar el resultado

Esperar el mensaje `Operación completada` y guardar la ruta del respaldo que se imprime. El archivo está en `.local/backups` dentro de la carpeta nueva del proyecto. Protegerlo porque contiene datos y hashes de acceso. Los archivos `.incompleto` indican respaldo no verificado; no usarlos como respaldo válido.

En la misma terminal, iniciar la aplicación:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-windows.ps1
```

Usar la contraseña PostgreSQL y la misma contraseña demo de antes. Abrir el navegador y consultar `41560`: debe encontrarse. Antes de registrar el primer consumo, el reporte no debe tener consumos. Los empleados ficticios 40001 y 49001 solo permanecerán si esos números existen realmente en el CSV; ya no se reinsertan por esta herramienta.

La importación es local y no depende del archivo después de cargarlo. Conservar el CSV para auditoría o futuras actualizaciones. No volver a ejecutar este reinicio para actualizar RH: un futuro proceso de actualización debe preservar consumos.

## 5. Si algo falla

- La herramienta pide detener la aplicación: usar Ctrl+C en la terminal de arranque y repetir.
- Falta pg_dump/pg_restore: comprobar la instalación de PostgreSQL 17 y la ruta indicada. No omitir el respaldo.
- CSV rechazado: corregir su formato o seleccionar el archivo correcto; no cambiar los datos de la base a mano.
- Contraseña incorrecta: comprobarla con pgAdmin; no desactivar autenticación.
- Fallo después del respaldo: conservar el archivo y la salida de la terminal, sin contraseñas. Los fallos de carga anteriores al commit se revierten automáticamente.

Para recuperar un respaldo, primero restaurarlo en una base nueva desde pgAdmin → Create Database → Restore, formato Custom, y comprobar empleados, usuarios y consumos. Pedir asistencia antes de sustituir la base en uso; no usar Clean/Drop sobre la base activa. El respaldo no contiene la instalación del servidor ni archivos de fotografías; conservar también `.local/fotos` si existe.

## Validación realizada

Se probó el motor de importación en PostgreSQL 17: respaldo fallido sin borrado, reversión por fallo de carga, conservación de cuentas/hash/configuración, respaldo y restauración en otra base, y carga completa del CSV en una base temporal. El script PowerShell debe comprobarse en el equipo Windows del usuario; no se ha accedido ni modificado su base desde la nube.
