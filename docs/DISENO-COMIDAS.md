# Diseño de inicio: alimentos y comedor

La cabecera presenta un plato servido sobre fondo azul institucional y una sección inferior muestra personas compartiendo alimentos en un comedor industrial, con chalecos verdes de seguridad, franjas reflejantes y polos azul marino con el emblema de Grupo Aeroportuario Marina. Ambas imágenes son ilustraciones fotográficas generadas para el diseño; no documentan instalaciones ni personal reales del AICM. Se conserva la identidad de Marina y todos los permisos y destinos de los accesos. El icono de avión se sustituye por una campana de servicio y el fondo de acceso utiliza la nueva imagen gastronómica.

![Inicio del sistema con alimentos y comedor](capturas/inicio-comidas.png)

## Instalar sobre tu copia de desarrollo

Detén únicamente la aplicación local con Ctrl+C, conserva una copia de los archivos anteriores y sustituye sólo estos cinco archivos, respetando las carpetas:

- `src/Comedores.Web/Pages/Index.cshtml`
- `src/Comedores.Web/Pages/Shared/_Layout.cshtml`
- `src/Comedores.Web/wwwroot/css/site.css`
- `src/Comedores.Web/wwwroot/images/comida-institucional.webp`
- `src/Comedores.Web/wwwroot/images/comedor-personal.webp`

No reemplaces todo `src`: tu instalación tiene cambios propios de arranque local, encabezados reenviados y gestión de contraseñas que este paquete no modifica. No requiere cambios de base de datos, usuarios, reglas, configuración de conexión o fotografías de empleados.

Compila con `dotnet build .\src\Comedores.Web\Comedores.Web.csproj`, inicia con tu comando local de pruebas y actualiza el navegador con Ctrl+F5. La aplicación del servidor no cambia hasta publicar y desplegar. Las imágenes se sirven localmente desde `wwwroot/images`, sin depender de un servicio externo ni conexión a internet. Peso conjunto aproximado: 624 KiB, formato WebP.

Validación: compilación sin errores ni advertencias, revisión de inicio y navegación en navegador, imágenes cargadas y ausencia de desbordamiento horizontal en 1536, 1366, 1024, 768 y 390 píxeles. Los enlaces y sus condiciones de autorización conservan su comportamiento.
