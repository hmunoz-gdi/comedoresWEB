# Abrir la demostración en Windows

Requisitos ya preparados: .NET SDK 10 y PostgreSQL 17 con la base `comedores` vacía. Este proyecto no requiere Visual Studio 2019 ni Docker en Windows.

1. Extraer el ZIP con el botón derecho > Extraer todo. Usar una carpeta de tu usuario, por ejemplo Documentos. No abrir el proyecto dentro del ZIP.
2. En VSCodium elegir File > Open Folder y seleccionar la carpeta que contiene este archivo, `global.json`, `scripts` y `src`. Si pregunta si confías en los autores, comprobar que es el proyecto descargado de esta conversación y aceptar para trabajar con él.
3. Elegir Terminal > New Terminal. En esa terminal PowerShell ejecutar:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\iniciar-windows.ps1
   ```

   Este parámetro permite ejecutar este script en ese proceso; no modifica la política permanente del equipo. Si una política corporativa lo impide, no modificarla: solicitar el procedimiento autorizado a TI.
4. Esperar a que descargue las dependencias. Necesita acceso a NuGet.
5. Introducir la contraseña del usuario `postgres`, elegida durante la instalación de PostgreSQL. No se muestra al escribir y no se guarda en un archivo del proyecto.
6. Elegir una contraseña para la cuenta `demo` de la aplicación: mínimo 12 caracteres, mayúscula, minúscula, número y símbolo. Es una cuenta distinta del usuario de PostgreSQL. Guardarla de forma privada. En siguientes arranques usar esa contraseña; el script no cambia la contraseña de una cuenta ya creada.
7. Esperar al mensaje `Now listening on`. Abrir el navegador en `http://localhost:5080`. Esta dirección es de TU computadora con la aplicación en ejecución, no una dirección del entorno de nube.
8. Entrar con usuario `demo` y su contraseña. En Registrar servicio consultar `40001` o `49001`. Son empleados ficticios. Repetir el registro para ver el consecutivo y revisar Reportes.

Mantener la terminal abierta mientras usas la aplicación; Ctrl+C la detiene. El arranque simula la IP de SUBESTACION, pero usa la hora real del servidor convertida a Ciudad de México. Para una prueba explícita de desayuno, agregar `-SimularDesayuno` al comando del paso 3: fija las 08:30 y muestra un aviso visible. Esos consumos se guardan con la hora simulada. `-HoraReal` se conserva como opción compatible, aunque ya es el comportamiento predeterminado.

La aplicación crea sus tablas y empleados ficticios en la base vacía. No ejecutar `comedores2026.sql`: es un script del sistema anterior para SQL Server. No importar empleados reales para esta demostración. La lista de funciones pendientes está en README.md.

Si hay un error, compartir el mensaje ocultando cualquier dato sensible. Nunca compartir contraseñas ni una cadena de conexión que las contenga.

Al repetir un tipo de servicio en el mismo día aparece un cuadro con el número de consumos previos. Cancelar no guarda otro; Continuar confirma el registro. Consulta [Horarios y confirmaciones](docs/HORARIOS-Y-CONFIRMACION.md).
