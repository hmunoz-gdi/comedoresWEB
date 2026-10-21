# Agregar tu perfil y la fotografía

Esta función agrega un perfil local de prueba sin publicar datos personales en GitHub. No sustituye la futura sincronización de RH. Disponible desde la actualización que incluye `scripts/configurar-perfil.ps1`.

## Actualizar el proyecto

1. Detener la aplicación con Ctrl+C en su terminal.
2. Descargar el ZIP actualizado desde Code → Download ZIP de la rama `desarrollo/primera-version-web`.
3. Extraerlo en una carpeta nueva y abrirla en VSCodium. No borrar la carpeta anterior: podría contener archivos locales.
4. No recrear ni borrar la base `comedores`. La actualización utiliza sus tablas existentes; no necesita cambios de esquema.
5. Si la carpeta anterior tiene `.local`, copiar su contenido a la carpeta nueva sin sobrescribir archivos distintos. Esa carpeta nunca viene en el ZIP porque es privada.

## Agregar el perfil

Desde el panel de VSCodium, botón derecho en `scripts` → Open in Integrated Terminal. Ejecutar:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\configurar-perfil.ps1
```

Contestar número, nombre completo, área, puesto, horario y turno especial (S o N). El script guarda `.local/empleado.json` y crea `.local/fotos`. No contiene contraseñas. Habilita explícitamente al empleado para esta prueba, sin asumir la correspondencia de EMP_STATUS de RH.

Después, desde la misma terminal:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-windows.ps1
```

El arranque agrega el perfil aunque la base ya tuviera empleados y consumos. Si el número ya existe, no modifica sus datos ni duplica el empleado. Para cambiar un perfil ya importado se necesitará un procedimiento de edición posterior; modificar el JSON no sobrescribe el catálogo existente.

Entrar al sistema y consultar el número recién agregado. Se deben mostrar sus datos y el botón Registrar servicio. Si no tiene foto, aparece Sin fotografía y se puede registrar el consumo.

## Agregar la fotografía

En la carpeta principal del proyecto, abrir `.local/fotos` y copiar la foto del empleado. El nombre debe coincidir con su número, por ejemplo `41560.jpg`. También se admite `.jpeg` o `.png`, hasta 5 MB.

Usar una fotografía auténtica autorizada. No basta cambiar la extensión de un archivo HEIC o WEBP: exportarlo como JPG o PNG. Actualizar la pantalla del navegador después de copiarlo; no hace falta reiniciar la aplicación para la foto.

Las imágenes se sirven mediante la página autenticada de registro, con tipo de archivo comprobado y sin caché. No se colocan en wwwroot ni se incluyen en GitHub. No compartir el contenido de `.local` en un repositorio público. Cada compañero necesita su copia autorizada del perfil y las fotos si desea usarlos; el ZIP solo incluye el código.

No se ha recibido todavía la fotografía del empleado 41560. Esta actualización permite mostrarla cuando se proporcione el archivo; no inventa ni reconstruye una imagen de la persona.
