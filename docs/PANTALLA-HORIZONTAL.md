# Vista horizontal y diálogo fuera de horario

## Fuera de horario

Al consultar un empleado activo fuera del horario del comedor, aparece un cuadro titulado Fuera de horario con el botón Entendido. No se registra ningún consumo. Se puede cerrar con Enter, clic o Escape; el foco vuelve al número de empleado y se selecciona para facilitar la siguiente captura.

Si el horario termina después de consultar, pero antes de confirmar, el servidor rechaza la operación y se muestra el mismo diálogo. La causa viene identificada por el backend; no depende de comparar el texto del mensaje en JavaScript.

Abrir la página sin consultar no abre avisos continuamente. El reloj puede indicar Fuera de horario, pero el diálogo solo aparece al consultar/intentar registrar, o al abrir una consulta directa de un empleado activo. Los demás errores, como empleado inexistente o inactivo, conservan su propio mensaje. El diálogo de servicios repetidos sigue usando Continuar y Cancelar.

## Distribución en monitor horizontal

En modo horizontal, desde 960 píxeles de ancho:

- Encabezado y navegación más compactos.
- Título y comedor a la izquierda, reloj y fecha a la derecha.
- Campo de captura a la izquierda; fotografía y datos a la derecha.
- Nombre, número, área, puesto, horario, turno especial, conteo y botón de registro conservan su contenido.
- Los datos de las fichas se distribuyen en bloques y ajustan líneas sin recortarlas. Las reglas de uso se pueden desplegar bajo el campo de captura.
- Se utilizan los márgenes del monitor, sin aplicar zoom ni esconder el contenido con overflow de la página.

Se probó con datos ficticios largos en 1920×1080, 1366×768, 1366×650, 1280×720, 1024×768 y 1024×650 de área visible del navegador, al 100 %. En esas pruebas la ficha y los controles caben sin desplazamiento vertical ni horizontal.

La resolución del monitor no equivale al espacio disponible de la página: barras del navegador y zoom consumen espacio. En teléfonos, ventanas pequeñas, zoom elevado o datos excepcionalmente largos se conserva el desplazamiento natural para no ocultar información ni reducir excesivamente el texto. Los reportes con muchas filas siguen pudiendo desplazarse e imprimirse completos; no se intenta comprimir una cantidad ilimitada de registros en una sola vista.

## Actualizar en Windows

1. Detener la aplicación con Ctrl+C.
2. Descargar y extraer el ZIP actualizado de la rama `desarrollo/primera-version-web`.
3. Copiar `src` dentro de la carpeta del proyecto actual, combinando carpetas y reemplazando archivos. Conservar `.local` y la base; no ejecutar reiniciar-catalogo.ps1.
4. Iniciar normalmente con hora real y la ruta de fotografías.
5. En el navegador, pulsar Ctrl+F5. Comprobar al 100 % con una ficha de empleado completa.

Para verificar el diálogo sin alterar Windows, usar un proceso de prueba con Demo:Now fijado fuera del horario del comedor. No usar pruebas de fechas sobre datos operativos sin identificar sus consumos como pruebas.

## Verificación técnica

El backend devuelve `OutsideHours` tanto para consulta como para confirmación. La vista usa un diálogo HTML modal; al cerrarlo se restablece el foco. La clase `registration-page` permite compactar esta pantalla sin aplicar la ficha horizontal al PDF del reporte.

Se probó en Chromium: medidas de la página en los seis tamaños, ausencia de desbordamiento horizontal en 390×844, aviso por consulta, por cierre de horario al confirmar y por URL directa, cierre con Enter/Escape y ausencia de un consumo nuevo. Se repitió el flujo de registro rápido y servicios repetidos.

La prueba está en `tests/browser/registration-layout.cjs`. Requiere una base temporal con empleados ficticios y dos procesos: `TEST_BASE_URL` dentro de horario y `TEST_OUTSIDE_URL` fuera, por defecto puertos 5082 y 5083. Cada proceso usa su propia sesión y antifalsificación; no se desactiva la protección. Preparar texto largo para la ficha 40001 permite verificar envoltura de líneas. Para la prueba general de registro, TEST_EXPECTED_TIME permite indicar la hora de simulación esperada.
