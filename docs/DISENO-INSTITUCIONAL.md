# Diseño visual del sistema

La navegación de escritorio está a la izquierda. La sección activa se identifica en amarillo y con `aria-current`. En pantallas pequeñas el menú se acomoda arriba para dar espacio a los datos. Los reportes impresos excluyen la navegación.

Los tonos azul marino y amarillo toman como referencia las capturas de la intranet proporcionadas por el equipo. El avión y el comedor del inicio son una ilustración original, no una fotografía del AICM. Los logotipos de Marina Grupo Aeroportuario y de la Secretaría de Marina se incorporaron desde el ZIP proporcionado por el equipo, en un panel blanco para conservar sus colores y proporciones.

La captura conserva el flujo por teclado y sus diálogos. La ilustración se reserva para inicio/acceso para mantener visible la ficha del empleado durante el registro. Los reportes extensos y los teléfonos mantienen desplazamiento natural.

Actualización en Windows: detener con Ctrl+C, copiar la carpeta `src` de la versión nueva al proyecto actual y reemplazar archivos, iniciar con el comando habitual y recargar el navegador con Ctrl+F5. No ejecutar el reinicio del catálogo para aplicar este diseño.

## Fotografía y legibilidad

La ficha utiliza fotografía de 210 × 280 píxeles en monitores amplios y 155 × 210 en monitores pequeños; en ventanas de poca altura reduce la altura para mantener el registro visible. El nombre tiene separación, mayor tamaño y un divisor antes de los datos. El título del sistema aparece en un bloque propio con un icono de plato y cubiertos. Se verificó visualmente con un nombre largo y campos extensos en 1366 × 768 y 1024 × 650, sin desplazamiento vertical en la ficha; en móvil se conserva desplazamiento natural sin desbordamiento horizontal.

Los archivos originales recibidos en `Logos.zip` se conservan sin alteraciones en `wwwroot/images`. Se seleccionaron las variantes transparentes para el menú, el acceso y la cabecera impresa.

El menú lateral muestra sólo Marina Grupo Aeroportuario, con AICM de mayor tamaño debajo. La identidad del sistema utiliza un avión vectorial y el enlace Registrar servicio conserva el plato con cubiertos, para distinguir ambos elementos. Los logotipos dentro del inicio de sesión mantienen su distribución propia.

La marca del menú lateral se sustituyó por el logotipo original de Marina · Secretaría de Marina, conservando AICM debajo.

## Menú lateral ampliado

El menú utiliza 280 px en pantallas de al menos 1440 px, 260 px en escritorio intermedio y 220 px en monitores compactos. El logo conserva su archivo original y ocupa mayor ancho; AICM utiliza una composición ligera con la leyenda Aeropuerto Internacional / Ciudad de México. El título se alinea con el avión para reducir espacio vertical. En ventanas de poca altura se compactan los márgenes y se oculta únicamente el lema decorativo del pie lateral. La captura se revisó con ficha extensa en 1366 × 768 y 1024 × 650, sin desbordamiento horizontal y con el botón de registro visible. La navegación sigue excluida de impresión.
