# Horarios, hora registrada y aviso de repetición

## Regla de clasificación

El servidor identifica el comedor del equipo y toma la fecha y hora al recibir la confirmación de registro. La hora real se convierte a Ciudad de México. Busca el rango vigente en `MealWindows` y guarda su tipo en el consumo. No se elige manualmente Desayuno, Comida o Cena.

| Comedor | DYN: Desayuno | COM: Comida | CEN: Cena |
| --- | --- | --- | --- |
| SUBESTACION | 08:00–11:00 | 14:00–18:00 | 20:30–22:00 |
| PLATAFORMA | 08:00–11:00 | 14:00–18:00 | 20:30–22:00 |
| CREI | 08:00–11:00 | 14:00–18:00 | 19:00–22:00 |

Se incluyen los extremos definidos. Fuera de esos rangos no se guarda un consumo nuevo. Si los rangos se superponen o el tipo es desconocido, se informa el problema de configuración y no se clasifica arbitrariamente.

La consulta puede ocurrir en un horario y la confirmación en otro: manda la hora de la confirmación recibida por el servidor. La pantalla muestra el servicio y la hora al consultar, y el mensaje de éxito muestra el tipo y la hora que realmente se guardaron.

El servidor debe tener su reloj sincronizado. El navegador no decide la hora ni el segmento.

## Hora real por defecto

A partir de esta actualización, `iniciar-windows.ps1` usa la hora real por defecto. `-HoraReal` sigue siendo válido para los comandos anteriores. La IP sigue siendo la de demostración SUBESTACION hasta preparar el despliegue institucional.

Para usar fotos del recurso compartido, desde la carpeta `scripts`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-windows.ps1 -HoraReal -RutaFotos '\\SERVIDOR\RECURSO\Fotos'
```

Sustituir la ruta del ejemplo por la ruta privada indicada por infraestructura. No publicar esa configuración ni credenciales en el código.

Solo para pruebas fuera de horario se puede agregar `-SimularDesayuno` (sin `-HoraReal`). Se fija 08:30 y aparece un aviso visible HORA SIMULADA. La hora simulada también se guarda; no debe usarse como si fuera evidencia de consumo real. Los registros de pruebas anteriores conservan sus horas: esta actualización no recalcula ni borra consumos históricos.

## Aviso por segmento y día

Antes de guardar, el servidor cuenta los consumos del empleado, fecha de Ciudad de México y tipo DYN/COM/CEN, sumando todos los comedores. Si existe al menos uno, muestra un cuadro:

> El empleado ya tiene 2 servicios de COMIDA hoy, sumando todos los comedores. ¿Deseas registrar otro?

- Cancelar: no se guarda un nuevo consumo. Escape también cancela el cuadro.
- Continuar: se confirma y guarda otra operación. Con teclado, el foco inicial está en Cancelar; usar Tab para seleccionar Continuar y pulsar Enter.
- Si otro operador agregó un consumo mientras estaba abierto el cuadro, se informa el nuevo conteo y se vuelve a pedir confirmación.
- Mantener Enter presionado o enviar dos veces la misma solicitud no registra dos consumos.
- Un reintento de una solicitud ya guardada reconoce el registro existente, incluso si el horario ya cerró; no crea otro.

El aviso aplica también a turno especial. No calcula importes ni decide exenciones. Tampoco es un bloqueo permanente: se permite continuar, como se acordó.

No confundir dos conteos:

| Conteo | Qué significa |
| --- | --- |
| Del segmento | Cuántos desayunos, comidas o cenas lleva ese empleado hoy. Determina el aviso. |
| Del día | Cuántos consumos lleva hoy de cualquier tipo y comedor. Desde el segundo se identifica como adicional. |

Ejemplo: desayuno a las 08:15 = servicio diario 1. Otro desayuno a las 09:00 avisa que hay 1 desayuno previo; al continuar queda servicio diario 2. Una primera comida a las 14:10 no repite el segmento comida, pero queda servicio diario 3 y adicional.

## Reporte y PDF

El reporte usa lo guardado en `Consumptions.Type` y `OccurredAt`; no vuelve a clasificar por los horarios actuales. La fecha de servicio se conserva en `ServiceDate`, y las horas se muestran en Ciudad de México.

El resumen presenta columnas Desayuno, Comida y Cena, cada una con la cantidad y todas sus horas. Si hubo dos comidas, se muestran las dos horas en esa columna. El detalle permite consultar cada operación individual, con comedor y consecutivo diario. Imprimir / guardar PDF conserva esos datos; el PDF se genera desde el navegador.

Ejemplo de fila:

| Empleado | Desayuno | Comida | Cena |
| --- | --- | --- | --- |
| Empleado de prueba | 1 · 08:05:11 | 2 · 14:10:12, 14:20:13 | 1 · 20:35:14 |

## Equivalencia con VB6

| VB6 anterior | Proyecto web |
| --- | --- |
| Consulta a COMEDOR_HORARIO | `MealService.PeriodAtAsync` consulta `MealWindows` |
| COMEDOR_HORARIO_TIPO | `MealWindow.Type`: DYN/COM/CEN |
| Fecha y tipo guardados en COMEDOR_REGISTRO | `Consumption.OccurredAt`, `ServiceDate` y `Type` |
| MsgBox que informa una repetición | Cuadro con Continuar / Cancelar, validado nuevamente por el servidor |
| Reporte de desayunos, comidas y cenas | `Reports.cshtml` agrupa los consumos y muestra sus horas |

En la web las horas de horario se guardan como `TimeOnly`, no como texto. Las fechas de consumo se guardan en UTC y se convierten al presentarlas. No se copió el bloqueo de repetidos del VB6: se aplica la confirmación solicitada.

Para revisar los nueve horarios actuales en pgAdmin, Query Tool de la base de pruebas:

```sql
SELECT d."Name" AS comedor, w."Type" AS tipo,
       w."Start" AS inicio, w."End" AS fin
FROM "MealWindows" w
JOIN "DiningRooms" d ON d."Id" = w."DiningRoomId"
ORDER BY d."Name", w."Start";
```

## Cómo actualizar y comprobar

1. Detener la aplicación con Ctrl+C.
2. Descargar el ZIP actualizado de la rama `desarrollo/primera-version-web`, extraerlo y copiar `src` y `scripts` dentro del proyecto actual, reemplazando archivos. Conservar `.local`.
3. No ejecutar el reinicio de catálogo: no hay cambios de esquema ni necesidad de borrar datos.
4. Arrancar con hora real y la ruta de fotos, actualizar el navegador con Ctrl+F5.
5. Consultar al empleado, registrar y comprobar el tipo/hora del mensaje. Al repetir el mismo segmento, cancelar primero y comprobar que el conteo no cambió; después continuar y revisar la hora nueva en Reportes y en Imprimir / guardar PDF.

Las pruebas automatizadas usan bases temporales y datos ficticios: extremos y fuera de rango de los tres tipos en los tres comedores, cambio de horario entre consulta y confirmación, horarios superpuestos, conteo por segmento, cancelación y aceptación, confirmación desactualizada, concurrencia, reenvío, conservación de configuración y extracción de las horas desde un PDF real. No se ha actualizado la computadora del usuario desde la nube.
