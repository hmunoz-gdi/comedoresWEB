# Perfiles y cuentas de acceso

## Permisos

| Perfil operativo | Registrar servicios | Reportes | Empleados y fotos |
|---|---|---|---|
| Operador | En su comedor asignado, desde un equipo autorizado y dentro del horario. El servidor determina DYN/COM/CEN. | Sólo su comedor, incluyendo impresión/PDF. | No |
| Catálogo | No | No | Altas, modificaciones, bajas y fotografías. |
| Coordinador | En cualquier comedor, a cualquier hora. Debe elegir comedor y DYN/COM/CEN. | Todos los comedores. | No |

Las páginas y los manejadores de consulta, guardado, cámara y reportes están protegidos en el servidor; ocultar enlaces no es la medida de autorización. Cambiar campos o URLs no amplía permisos. El servidor ignora el tipo y comedor enviados por un operador: usa su equipo autorizado y exige que coincida con el comedor de su cuenta.

No cambian las validaciones de empleado activo ni la confirmación por repetición del mismo segmento. Un coordinador también recibe el aviso de 1, 2, 3… servicios previos de ese tipo, contando todos los comedores. La hora guardada sigue siendo la actual del servidor: no se agrega captura retroactiva ni elección de otra fecha. Se conservan operador, comedor, tipo y hora del registro en el historial. Un servicio seleccionado fuera de horario se muestra en el segmento elegido, incluso si la hora no pertenece a su ventana habitual.

## Primer acceso después de actualizar

1. Detén la aplicación y reemplaza `src` y `scripts` con la versión nueva.
2. Inicia con el comando habitual y la ruta de fotografías.
3. **Cierra sesión y vuelve a entrar con `demo` y tu contraseña actual.** Es necesario renovar la sesión para recibir los perfiles nuevos.
4. Abre **Usuarios y perfiles** en el menú izquierdo.

En desarrollo, a la cuenta existente `demo` sin perfiles anteriores se le asignan los perfiles de administración y los tres operativos como acceso inicial de Sistemas. No se cambia su contraseña. Esa cuenta permite preparar las cuentas separadas; no debe utilizarse como operador cotidiano. Su pantalla de registro corresponde a Coordinador, porque cuenta con ese permiso. Las cuentas que ya tengan perfiles asignados no reciben permisos nuevos automáticamente.

Además de los tres perfiles operativos existe el permiso interno **Administrador**, que permite gestionar cuentas. La cuenta inicial se conserva y no puede cambiarse de perfil desde el formulario para evitar perder acceso a la administración. Crear otro administrador no está expuesto en este módulo. Fuera del entorno de desarrollo se necesita aprovisionar previamente la cuenta administrativa; no hay creación de administradores ni contraseñas por defecto en producción.

## Crear cuentas

En **Usuarios y perfiles**:

1. Introduce un nombre de usuario individual.
2. Selecciona Operador, Catálogo o Coordinador.
3. Para Operador, selecciona su comedor: SUBESTACION, PLATAFORMA o CREI.
4. Introduce una contraseña de al menos 12 caracteres, incluyendo mayúsculas, minúsculas, números y símbolos.
5. Pulsa **Crear usuario**.

No se muestran ni almacenan contraseñas en texto plano. Las cuentas nuevas reciben sólo el perfil elegido. Los nombres y los perfiles aparecen en la tabla junto al comedor asignado. Comparte las credenciales directamente con cada compañero; no las subas a GitHub.

Para cambiar una cuenta operativa, haz clic en su nombre en la tabla de **Usuarios y perfiles**. Se cargan automáticamente usuario, perfil, comedor y estado. El nombre queda fijo para evitar modificar por accidente otra cuenta. Elige el nuevo perfil y el comedor si corresponde, y pulsa **Cambiar perfil**. La cuenta continúa seleccionada después de guardar. Para crear otra cuenta utiliza **Nuevo / Limpiar**. No se requiere escribir contraseña y ese botón no la cambia. Se sustituyen los permisos operativos anteriores. Las sesiones de esa cuenta se invalidan: debe volver a iniciar sesión. Un fallo al asignar perfiles revierte la operación.

## Uso diario

**Operador:** entra con su cuenta en el equipo de su comedor. Consulta el número del empleado y registra con Enter. Los horarios se validan en el servidor y el tipo se calcula automáticamente. Un número de comedor distinto enviado desde el navegador no se utiliza. Si su equipo no coincide con el comedor asignado, solicita revisión a Sistemas.

**Catálogo:** abre Empleados. Puede consultar y administrar datos/fotografías, pero no registrar consumos ni consultar reportes de servicios. La vista de fotografía usa un manejador propio del catálogo para mantener esta separación.

**Coordinador:** consulta al empleado y elige **Comedor del servicio** y **Tipo de servicio** (DYN, COM o CEN) antes de registrar. Puede hacerlo dentro o fuera de horario. La pantalla no muestra el rechazo por estar fuera de ventana. Los reportes conservan el tipo elegido y la hora real; no reclasifican posteriormente el consumo por la hora.

## Equipos y comedores

La asignación de comedor a una cuenta de operador se combina con la configuración existente de terminales/IP, no la sustituye. El arranque Windows utiliza la IP real de conexión por defecto. Para simular explícitamente un comedor durante pruebas, utiliza la opción `-Comedor` del arranque. Por ejemplo, desde `scripts`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-windows.ps1 -HoraReal -Comedor PLATAFORMA -RutaFotos '\\172.16.2.23\comedor\Fotos'
```

Valores: `SUBESTACION`, `PLATAFORMA`, `CREI`. Esta opción es de desarrollo y selecciona una terminal de demostración existente; en producción se valida la IP real autorizada. Sin esa opción no se asigna una IP simulada. No basta con cambiar el comedor de la cuenta: su IP real debe estar registrada para ese mismo comedor. El operador se valida antes de iniciar sesión y en cada petición posterior.

La carpeta de fotografías y los permisos de escritura no cambian. Tampoco se borran empleados, consumos, horarios o contraseñas al actualizar. Se utilizan las tablas de roles y claims de Identity que ya existen: no requiere reiniciar el catálogo. La actualización de bloqueo añade una tabla de bitácora sin borrar datos.

## Comprobaciones

La suite `tests/browser/profiles.cjs` usa dos instancias sobre una misma base **aislada de pruebas**, una a las 12:00 fuera de horario y otra a las 09:00 dentro de desayuno. Crea cuentas ficticias y verifica perfiles, menús, denegación de acceso directo, solicitudes manipuladas, reportes por comedor, selección manual de los tres tipos, confirmación de repetición y revocación de sesiones.

Las suites históricas del reloj, registro rápido y vista horizontal deben utilizar `TEST_USERNAME` con una cuenta **Operador** asignada al comedor de su terminal y `TEST_DEMO_PASSWORD` con su contraseña. La cuenta inicial `demo` ahora tiene selección manual de Coordinador; no representa el flujo del operador. La suite de empleados requiere Catálogo para las pantallas de empleados y una cuenta con permiso de servicios si prueba el ciclo completo; la cuenta administrativa inicial incluye ambos permisos.

Consulta [bloqueos y desbloqueos](BLOQUEO-Y-DESBLOQUEO.md): tres intentos fallidos bloquean la cuenta hasta la intervención de administración.

La contraseña de una cuenta seleccionada permanece vacía y deshabilitada: se usa únicamente para crear una cuenta nueva. Seleccionar la cuenta administrativa muestra su estado y conserva protegidos sus perfiles.

Consulta [exclusividad por IP](ACCESO-POR-IP.md) para probar desde la red sin simulación.
