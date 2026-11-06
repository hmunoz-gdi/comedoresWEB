# Marquesina y reloj en línea

La pantalla Registrar servicio muestra una franja con reloj de segundos, fecha completa y servicio vigente en Ciudad de México. El texto inferior de la marquesina se desplaza; el reloj permanece legible y fijo en su lugar.

- La hora inicial proviene del servidor. El navegador la hace avanzar cada segundo usando un contador monotónico, sin tomar como referencia su fecha local ni su zona horaria.
- Se sincroniza con el servidor cada 30 segundos y al volver a la pestaña o recuperar la conexión. No se consulta la base cada segundo.
- El servicio vigente se actualiza al cruzar un límite de horario, usando la configuración de ese comedor recibida del servidor. La clasificación y hora guardadas en cada consumo siguen siendo responsabilidad del backend.
- El botón Pausar marquesina detiene el desplazamiento del texto; el reloj continúa. Se respeta la preferencia del sistema de reducir movimiento.
- Si falla la sincronización, se avisa que la hora mostrada es una estimación desde la última conexión; cuando se recupera, vuelve a sincronizar. Si termina la sesión se indica volver a entrar.
- En modo de simulación, el reloj se identifica como PRUEBA / Hora simulada fija. No se hace avanzar una hora que el backend mantiene fija, para evitar mostrar una hora distinta de la que se guarda.

La indicación EN LÍNEA corresponde a la última sincronización exitosa, no garantiza que la red permanecerá disponible entre consultas. El instante mostrado aproxima el tiempo de transporte; la hora oficial del consumo es la que guarda el servidor al confirmar.

## Actualizar en Windows

1. Detener la aplicación con Ctrl+C.
2. Descargar y extraer el ZIP actualizado de la rama `desarrollo/primera-version-web`.
3. Copiar la carpeta `src` del ZIP dentro del proyecto actual, combinando y reemplazando archivos. Conservar `.local`; no reiniciar catálogo ni borrar datos.
4. Iniciar normalmente con hora real y la ruta de fotos. En el navegador presionar Ctrl+F5.

El reloj debe avanzar aunque no se consulte ningún empleado. Comprobar que coincide con la fecha/hora esperada de Ciudad de México. Al pausar la marquesina deben seguir cambiando los segundos.

## Archivos involucrados

- `Pages/Register.cshtml`: franja, fecha, reloj, estado y control de movimiento.
- `Pages/Register.cshtml.cs`, `OnGetClockAsync`: fecha UTC y horarios que entrega el servidor al usuario autenticado.
- `wwwroot/js/server-clock.js`: avance por segundo, sincronización y conversión de fecha/hora a Ciudad de México.
- `wwwroot/css/site.css`: presentación y movimiento de la marquesina.

## Comprobación automatizada

Prueba en Chromium con el cliente en otra zona horaria y Date.now alterado: avance de segundos, fecha del servidor, pausa, movimiento reducido, cambio de servicio, medianoche, pérdida/recuperación de sincronización y simulación fija. También se repitió el flujo de registro rápido y la confirmación de servicios repetidos.

La prueba está en `tests/browser/server-clock.cjs` (`npm run test:clock`). Usa `TEST_BASE_URL` para el servidor de prueba en hora real y, opcionalmente, `TEST_SIM_BASE_URL` para un segundo proceso con la hora simulada. Requiere Playwright/Chromium y la cuenta demo de pruebas. No necesita consumir servicios para comprobar el reloj.

## Identidad visual en la barra

La barra del reloj incluye el logotipo original de Marina Grupo Aeroportuario en un panel blanco, conservando proporciones y usando el PNG de 1080 × 832 píxeles a un tamaño menor que el original. La distribución mantiene visibles reloj, fecha, servicio y control de marquesina en escritorio y se reorganiza en móvil. El enlace Registrar servicio utiliza un pictograma vectorial de plato y cubiertos en lugar del signo +, para distinguirlo de una acción de expandir.

## Elegir fecha y hora de pruebas sin editar el archivo

Actualiza `scripts/iniciar-windows.ps1`. Desde una terminal integrada abierta en `scripts`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-windows.ps1 -FechaHoraPrueba "2026-11-06T14:30:00-06:00" -Comedor SUBESTACION -RutaFotos '\\172.16.2.23\comedor\Fotos'
```

El formato obligatorio es año-mes-día, T, hora:minuto:segundo y desplazamiento horario. `-06:00` corresponde a Ciudad de México. Esta opción fija tanto la fecha/hora del reloj como la que se guarda en los consumos. Para probar horarios utiliza una cuenta Operador asignada al comedor seleccionado. El Coordinador puede registrar fuera de horario por su permiso.

La terminal imprime **Archivo de arranque** con la ruta del archivo ejecutado y **MODO DE PRUEBA: fecha y hora fija 2026-11-06T14:30:00-06:00**. La fecha se pasa explícitamente como argumento de la aplicación, por encima de una configuración simulada previa. `-SimularDesayuno` sigue disponible para las 08:30 del día actual; si se proporciona también `-FechaHoraPrueba`, tiene prioridad la fecha explícita.

Para volver a la hora real, detén con Ctrl+C y usa el mismo comando sustituyendo `-FechaHoraPrueba "..."` por `-HoraReal`. No combines ambas opciones. El arranque con hora real también anula valores simulados heredados. No es necesario cambiar la fecha o la hora de Windows.

Cada cambio requiere detener el proceso anterior y arrancar nuevamente. El script avisa **El puerto 5080 ya está en uso** si detecta otra instancia; no la cierra automáticamente. Una recarga del navegador por sí sola no cambia la configuración del servidor. Los consumos previos conservan su hora original.

Si PowerShell no reconoce `FechaHoraPrueba`, se está ejecutando un archivo anterior. En VSCodium abre el archivo que modificas, usa Revelar en Explorador para identificar su carpeta real y reemplázalo allí. Comprueba que la ruta impresa al arrancar corresponde a esa copia. Un ZIP descargado o extraído en otra carpeta no actualiza el archivo abierto.

Pruebas: `tests/iniciar-windows.Tests.ps1` valida sintaxis, selección de fecha/hora, formato, conflictos, restauración de variables y puerto ocupado usando entradas y `dotnet` simulados (sin conexiones a bases). Se ejecutó con PowerShell 7 en Linux; utiliza sintaxis compatible con PowerShell 5.1. La prueba `tests/browser/startup-clock.cjs` verifica el reloj y un consumo COM a las 14:30, arrancando la aplicación real con una hora anterior en el entorno y el argumento explícito nuevo. Con `TEST_REAL_BASE_URL` comprueba además una instancia con argumento vacío y hora real.

Para comprobar la IP física, utiliza `-AccesoRed` sin `-Comedor` y abre la dirección LAN del servidor. `-Comedor` activa una IP de prueba explícita, independiente de la fecha simulada. Consulta [Acceso por IP](ACCESO-POR-IP.md).
