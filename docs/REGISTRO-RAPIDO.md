# Registro rápido con teclado y fotos compartidas

## Actualizar sin borrar datos

1. Detener la aplicación con Ctrl+C en su terminal.
2. Descargar Code → Download ZIP desde la rama `desarrollo/primera-version-web` y extraerlo aparte.
3. Copiar las carpetas `src` y `scripts` del ZIP extraído dentro de la carpeta del proyecto que está abierta en VSCodium. Combinar carpetas y reemplazar sus archivos. No copiar ni eliminar `.local`; ahí están los archivos privados.
4. No ejecutar `reiniciar-catalogo.ps1`. Esta actualización no necesita importación ni cambios de esquema y conserva consumos.

## Configurar las fotos desde Windows

La aplicación admite una ruta local o una carpeta compartida. Los nombres deben corresponder al número de empleado, como `41560.jpg`. Acepta JPG/JPEG/PNG, también extensiones mayúsculas, hasta 5 MB por imagen.

Primero comprobar en el Explorador de Windows que se puede abrir la carpeta de red y alguna foto con el mismo usuario con que se ejecuta la aplicación. No poner credenciales del recurso compartido en el código ni en GitHub.

En VSCodium, botón derecho en `scripts` → Open in Integrated Terminal. Ejecutar el arranque agregando `-RutaFotos` y la ruta de la organización entre comillas simples:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-windows.ps1 -RutaFotos '\\SERVIDOR\RECURSO\Fotos'
```

Sustituir la ruta del ejemplo por la proporcionada por el responsable de infraestructura. El parámetro configura `EmployeePhotos__Directory` solo en ese proceso y restaura su valor anterior al salir. Si no se configura, se usa `.local/fotos` como antes. También puede configurarse esa variable directamente en un servicio; no es necesario cambiar el código.

Introducir las contraseñas habituales, esperar Now listening on y abrir la aplicación. En el navegador presionar Ctrl+F5 para recargar los scripts nuevos.

La captura del recurso compartido no demuestra que el proceso del servidor tenga acceso. En Windows local se utiliza la cuenta del proceso que arranca la aplicación. En Ubuntu se necesitará montar el recurso SMB y configurar una ruta Linux, permisos y credenciales gestionadas por infraestructura; no usar directamente una ruta UNC ni incorporar una contraseña en la aplicación.

La nube no puede comprobar el recurso privado de la organización. La validación realizada usa un directorio externo de prueba. Si falta foto o no se puede leer, la interfaz muestra Sin fotografía disponible; el registro sigue disponible para que el operador aplique el procedimiento autorizado. Las imágenes se entregan solo a usuarios autenticados y no se almacenan en GitHub ni se sirven desde wwwroot.

## Operación durante la fila

1. Escribir el número (1 a 5 dígitos) y pulsar Enter.
2. Verificar número, nombre y fotografía. Si hay turno especial se muestra en la ficha.
3. Pulsar Enter otra vez, después de soltar la tecla. No es un registro automático al terminar de escribir.
4. Esperar la confirmación. El campo queda vacío y con el cursor listo; la ficha anterior se identifica como Último servicio registrado.
5. Escribir el siguiente número. La ficha previa se oculta para evitar confundir empleados.

El flujo funciona sin recargar toda la página. Se mantienen los botones Consultar y Registrar para operación con ratón. Escape limpia la selección y permite atender al siguiente sin guardar un consumo.

Un empleado inactivo o un equipo sin comedor configurado no habilita el registro. El horario y los adicionales se comprueban en el servidor como antes. No se calculan cobros.

## Evitar duplicados y atender fallas

Mientras una solicitud está pendiente se deshabilita el envío y el campo queda temporalmente en solo lectura. Mantener Enter presionado no dispara una segunda operación. Cada consulta válida genera un identificador de solicitud.

Si se pierde la respuesta al guardar, el sistema permite reintentar con el mismo identificador y el servidor evita duplicarlo. Reintentar en la misma ficha, sin volver a consultar ni recargar si se desconoce el resultado. Una consulta nueva genera otra operación; repetir intencionalmente un servicio sigue permitido y se identifica en el conteo diario.

Las solicitudes de consulta y registro tienen una espera de 15 segundos en el navegador. Una interrupción de red no confirma si el servidor guardó: por eso se conserva el identificador al reintentar. Si se venció la sesión, la interfaz indica volver a entrar. No afirmar que un consumo se guardó hasta recibir confirmación o verificarlo en el reporte.

## Comprobaciones y límites

Se probó con Chromium real: consulta/confirmación con Enter, tecla mantenida, doble envío, foco y campo limpio, cambio de empleado, Escape, foto en directorio configurado, foto inexistente y reintento después de perder una respuesta ya guardada. Los casos usan una base temporal y empleados ficticios.

Para repetir esas pruebas, preparar una base de prueba con empleados 40001 y 49001, cuenta demo, horario de desayuno y una PNG ficticia llamada 40001.png en el directorio de fotos. Ejecutar la aplicación en puerto 5082 con ese horario de prueba; no apuntar a la base operativa. En `tests/browser`: `npm ci`, instalar Chromium de Playwright o indicar `CHROMIUM_PATH`, y ejecutar `npm test`. La contraseña se lee de `TEST_DEMO_PASSWORD` o del archivo local de demo, sin imprimirse. La prueba agrega consumos ficticios.

La reducción de pasos no garantiza todavía un tiempo por comensal. Medirlo en el equipo y red reales antes de usarlo en hora pico. Si SMB introduce demoras en fotos, evaluar una copia local controlada; el flujo de consulta no necesita esperar la comprobación de existencia de la foto en la red para mostrar los datos.
