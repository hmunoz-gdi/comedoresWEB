# Exportar el reporte a archivo plano o Excel

## Actualizar

Detén la aplicación con Ctrl+C, reemplaza `src` con la versión nueva, inicia con tu comando habitual y recarga con Ctrl+F5. No se necesita vaciar la base ni cambiar las tablas.

## Descargar

1. Abre **Reportes**.
2. Selecciona fecha inicial, fecha final, comedor y categoría del personal.
3. Pulsa **Consultar** para aplicar los filtros y revisar el resultado.
4. Pulsa una de las opciones:
   - **Exportar resumen CSV / Excel:** una fila por empleado, fecha, comedor y condición de turno especial. Incluye cantidades de desayunos, comidas y cenas, todas sus horas y adicionales.
   - **Exportar detalle CSV / Excel:** una fila por consumo. Incluye número de empleado, nombre y puesto registrados, fecha, hora, código DYN/COM/CEN, nombre del servicio, comedor, consecutivo diario, primero/adicional y turno especial.
5. El navegador descarga un archivo `.csv`, normalmente en **Descargas**, o solicita dónde guardarlo según su configuración.

Primero aplica los filtros con Consultar: los botones exportan el mismo periodo y filtros del reporte mostrado. Un resultado vacío produce un archivo con las cabeceras, sin registros. Las fechas inválidas o un comedor ajeno al permiso de la cuenta no generan un archivo de datos.

## Abrir en Excel

CSV es un archivo de texto, no un libro `.xlsx`, pero se abre en Excel o LibreOffice Calc. Se genera con codificación **UTF-8 con BOM** para conservar acentos y ñ, y separador **punto y coma**.

Si al abrirlo todos los datos aparecen en una columna, en Excel usa **Datos → Desde texto/CSV**, selecciona el archivo, elige UTF-8 y el delimitador **punto y coma**.

Para conservar números de empleado con ceros iniciales, importa esa columna como **Texto** antes de la conversión automática a números. Si utilizas Power Query y ya existe un paso Tipo cambiado que eliminó ceros, elimina ese paso y asigna Texto a la columna antes de cargar. El archivo CSV conserva los ceros originales.

Si deseas un libro de Excel, una vez importado selecciona **Archivo → Guardar como → Libro de Excel (.xlsx)**. Cambiar sólo la extensión del archivo no lo convierte.

## Permisos y contenido

Operador exporta sólo su comedor. Coordinador puede exportar todos los comedores o elegir uno. Catálogo no puede exportar reportes de servicios. La descarga requiere una sesión válida y cumple la misma validación de IP del módulo.

Las horas son las registradas, convertidas a Ciudad de México. Los servicios no se reclasifican por la hora al exportar: se conserva el tipo DYN/COM/CEN guardado, incluyendo capturas manuales de Coordinador. Se conserva también el consecutivo diario, aunque un servicio previo haya ocurrido en otro comedor. No se calculan importes ni descuentos.

Los campos que podrían interpretarse como fórmulas se neutralizan con un apóstrofo inicial. El archivo se genera al descargar y no se almacena como copia permanente en el servidor.

## Pruebas

Las comprobaciones de .NET verifican BOM, acentos, campos con comillas o separadores, ceros iniciales, horas locales, repetidos, totales, archivos vacíos y protección contra fórmulas. `tests/browser/export-report.cjs` comprueba una descarga real, filtros, permisos de perfiles y rechazo de solicitudes que intenten exportar otro comedor. Debe ejecutarse únicamente contra una base aislada de pruebas con la IP de demostración SUBESTACION.
