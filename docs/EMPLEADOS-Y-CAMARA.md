# Empleados y fotografía con webcam

## Abrir el formulario

En el menú izquierdo, selecciona **Empleados**. El catálogo es el mismo que consulta **Registrar servicio**: las altas y modificaciones quedan disponibles al guardarlas. No se necesita volver a importar el CSV ni reiniciar el catálogo.

## Alta

1. Pulsa **Nuevo**.
2. Escribe un número de 1 a 5 dígitos. Los ceros iniciales se conservan y los números duplicados se rechazan.
3. Completa nombre completo, área, puesto y horario laboral. Marca turno especial si corresponde. Activo aparece seleccionado inicialmente.
4. Pulsa **Guardar datos**. Ahora se habilita la fotografía.

El nombre completo mantiene el formato del catálogo actual. El horario laboral se guarda como texto; no introduce nuevas reglas de validación de la jornada individual.

## Consulta, modificación y baja

Consulta el número en el buscador. En consulta, los datos están bloqueados y Guardar está desactivado. **Modificar** habilita los campos y Guardar; el número queda fijo. Guarda para volver a consulta. Si otra persona cambió esos datos desde que los consultaste, el sistema pide consultarlos nuevamente y no sobrescribe sus cambios.

**Dar de baja** pide confirmación y desactiva al empleado; conserva su historial y fotografía. La baja impide nuevos servicios. Para reactivar: Modificar, marcar Activo y Guardar. No se elimina físicamente al empleado. Limpiar / Cancelar descarta la edición y vuelve al formulario vacío; Salir lleva al inicio. Una captura de foto no guardada también se pierde al salir.

## Capturar y guardar una fotografía

1. Consulta a un empleado existente o guarda primero su alta.
2. Pulsa **Encender** y permite el acceso en el navegador.
3. Si hay varias cámaras, selecciona la deseada en Cámara.
4. Pulsa **Capturar**. La cámara se apaga y se muestra la foto capturada.
5. Pulsa **Guardar fotografía**. Espera el mensaje de confirmación.

Puedes repetir la captura antes de guardar. En **Seleccionar archivo JPEG** también se admite una foto existente de hasta 5 MB. La captura usa JPEG, con ancho máximo de 1280 píxeles y mantiene las proporciones. La foto se guarda por separado de los datos: un error en el servidor de imágenes permite reintentar la foto sin perder el alta.

La captura se probó con webcam simulada en Chromium. Se requiere verificar la cámara física y los permisos en el equipo del operador.

## Carpeta del servidor

Se usa **la misma carpeta configurada con `-RutaFotos`** al iniciar:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-windows.ps1 -HoraReal -RutaFotos '\\172.16.2.23\comedor\Fotos'
```

El comando se ejecuta en la terminal abierta sobre `scripts`. La cuenta de Windows que ejecuta la aplicación necesita permisos de lectura, creación, escritura y reemplazo de archivos en esa carpeta compartida, así como creación de la subcarpeta `.photo-backups`. Poder ver las fotos en el Explorador no garantiza permiso para guardarlas.

El archivo final se llama `<numero>.jpg`. Al sustituir un JPG existente, se conserva una copia con nombre único en `.photo-backups` antes de reemplazarlo. El nuevo archivo se escribe primero como temporal en el mismo directorio y luego se renombra. Las fotos anteriores en otros formatos permanecen conservadas; la consulta prioriza el JPG nuevo. La carpeta de fotos y sus respaldos deben mantenerse fuera de GitHub.

Sin `-RutaFotos`, se usa `.local/fotos` del proyecto. Este entorno de desarrollo no tiene acceso a la red institucional; el guardado en la ruta UNC requiere una comprobación desde Windows.

## Navegador y acceso

La webcam necesita un contexto seguro: **localhost en el propio equipo** o **HTTPS** cuando se abre desde otras computadoras. Una dirección HTTP con IP de la red normalmente bloquea la cámara. No es necesario desactivar la seguridad del navegador. La selección de JPEG sirve de alternativa.

Si está ocupada, cierra otros programas que usen la cámara. Al salir del formulario se apagan sus pistas. No se solicita audio.

Como las demás páginas del prototipo, este módulo requiere iniciar sesión y sus formularios usan protección antifalsificación. Sólo el perfil Catálogo (y la cuenta inicial de administración que lo incluye) puede administrar empleados y fotografías. Consulta `PERFILES-Y-USUARIOS.md` para asignar cuentas. El servidor valida números, campos obligatorios, longitudes, duplicados y tamaño/cabecera JPEG, y genera el nombre del archivo sin usar el nombre aportado por el navegador.

## Actualizar y probar

Detén la aplicación, reemplaza `src`, inicia con la ruta de fotos y recarga con Ctrl+F5. No hay cambios de esquema y no se necesita reiniciar ni vaciar la base. Verifica con un empleado de prueba: alta, consulta en Registrar servicio, modificación, foto, baja y reactivación.

Pruebas automatizadas del módulo:

```bash
CHROMIUM_PATH=/usr/bin/chromium TEST_BASE_URL=http://127.0.0.1:5084 npm run test:employees --prefix tests/browser
```

Usar exclusivamente una base y carpeta de fotografías aisladas de pruebas. La prueba crea el empleado ficticio 48888 y un consumo, modifica sus datos y foto, y verifica baja/reactivación, duplicados, concurrencia, webcam simulada, JPEG inválido, CSRF y adaptación móvil. La variable opcional `TEST_PHOTO_DIRECTORY` habilita el caso de falta de permisos y sólo admite una ruta terminada en `/.local/employee-test-photos`.
