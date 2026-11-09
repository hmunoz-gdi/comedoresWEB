# Recoger comida para compañeros

En **Registrar servicio**, pulsa **Recoger para compañeros**. El registro individual conserva su funcionamiento.

1. Captura el número de quien recoge y pulsa **Enter**. Verifica su nombre y fotografía.
2. Deja marcada **Incluir también su comida** si recibe una comida propia. Desmárcala si únicamente lleva encargos; no se le registra consumo por ser responsable.
3. Captura el número de cada compañero y pulsa **Enter**. Verifica cada nombre y fotografía. Puedes quitar un compañero antes de guardar.
4. Pulsa **Registrar servicios** o **Ctrl+Enter**. Si hay consumos previos del mismo segmento, aparece una sola confirmación con los comensales afectados y su cantidad. Cancelar conserva la lista sin registrar servicios.
5. Al guardar aparece **Servicio aplicado**. Aceptar limpia el grupo y regresa al número del siguiente responsable. Para coordinadores, comedor y tipo de servicio se conservan hasta cambiarlos.

Cada servicio pertenece al comensal cuyo gafete se capturó. Todos comparten el número y nombre de quien recogió y un folio de recogida. Los reportes y CSV incluyen el responsable; el CSV de detalle incluye además el folio. Los registros anteriores no tienen responsable capturado y se muestran sin ese dato. El resumen separa las filas cuando cambia el responsable para conservar la trazabilidad.

El operador mantiene las validaciones de IP, comedor asignado y horario. El coordinador selecciona comedor y DYN/COM/CEN sin restricción de horario. El servidor vuelve a validar empleados activos y consumos previos al confirmar. Un grupo admite hasta 100 servicios, sin repetir un gafete dentro del mismo grupo. Si alguien ya tiene servicios anteriores, puede recibir otro después de confirmar el aviso habitual.

El guardado es una sola transacción: si algún empleado no existe o está de baja, no se guarda parte del grupo. Dos registros simultáneos conservan los consecutivos diarios individuales. Ante una interrupción de red, se conserva el grupo y el botón permite reintentar el mismo folio sin duplicar servicios. Mientras el resultado sea incierto, la lista queda protegida contra cambios. Evita recargar o cerrar la página en ese momento.

## Actualizar la instalación

Detén la aplicación con Ctrl+C y actualiza **src** y **tools** desde la rama `desarrollo/primera-version-web`. Conserva `.local`, fotografías y base de datos. Inicia con tu comando habitual y actualiza el navegador con Ctrl+F5.

El arranque agrega `PickupBatches` y tres campos opcionales en `Consumptions`; no borra registros. No necesitas reiniciar el catálogo ni crear de nuevo la base. Como en cualquier actualización, conserva un respaldo previo. La herramienta de reinicio del catálogo también contempla las recogidas al limpiar pruebas y respalda estos datos antes de eliminarlos.

## Validación del desarrollo

Las comprobaciones de `tests/Comedores.Checks` cubren ambas modalidades, responsables, permisos, horarios, rechazo íntegro, avisos y confirmaciones desactualizadas, reintentos, CSV y reinicio con respaldo. `tests/browser/pickup.cjs` usa exclusivamente `comedores_pickup_test` en el puerto 5104, con datos ficticios; prueba teclado, confirmación, cancelación, limpieza, conservación de combos y reintentos concurrentes. No debe apuntarse a una base operativa.

## Servicios para llevar en el reporte

El reporte muestra el **total de servicios para llevar** del periodo y filtros consultados. El resumen agrega la cantidad por fila y el detalle identifica **Para llevar: Sí/No**. Estos datos también se incluyen al imprimir/guardar PDF y en ambos CSV para Excel.

Se considera para llevar el servicio del compañero que recoge otra persona. La comida propia del responsable no se clasifica automáticamente como para llevar. Los encargos ya guardados con su folio y responsable se identifican con esta regla, sin recapturarlos; los registros individuales anteriores conservan su clasificación. El conteo es parte del total de consumos, no un consumo adicional.
