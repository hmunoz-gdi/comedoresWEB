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
