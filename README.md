# GeoDataCollection
La aplicación **GeoDataRecollector** es una herramienta diseñada para registrar, visualizar y exportar información de geolocalización de un dispositivo móvil Android. A través de una interfaz simple y moderna, el usuario puede iniciar y detener la grabación de su posición GPS, visualizar su velocidad en tiempo real y administrar los datos capturados. A continuación se describen las principales funciones y el flujo de trabajo de la aplicación:

---

### 1. Interfaz Principal

- **Nombre de la aplicación**: En la parte superior se muestra el título “GeoDataRecollector” junto a un ícono de ubicación, que sirve para indicar la funcionalidad principal: la recolección de datos geográficos.
- **Botón central (Iniciar/Detener Grabación)**: Al presionar este botón, la aplicación solicita los permisos de ubicación en el dispositivo (si no han sido otorgados previamente) y, una vez concedidos, comienza a recopilar información del GPS en intervalos configurables. Si la grabación ya está en marcha, el mismo botón permite detenerla.
- **Sección de Exportación**: Un botón con ícono de archivo CSV permite **exportar los datos** capturados a la memoria local del dispositivo y, en Android, los copia también a la carpeta de descargas.
- **Tabla de Registros**: Debajo de los controles principales se encuentra un listado que muestra cada registro capturado. Cada fila incluye:
    - **TimeStamp**: Fecha y hora en la que se tomó el dato.
    - **Latitude**: Latitud actual.
    - **Longitude**: Longitud actual.
    - **Velocity**: Velocidad estimada (en metros por segundo).

---

### 2. Configuración de Parámetros

- **Intervalo de Captura**: En la parte superior (o en una sección dedicada) se dispone de un campo de texto que permite ajustar el **intervalo de grabación** en segundos. Este intervalo determina cada cuántos segundos la aplicación guardará un nuevo registro (timestamp, latitud, longitud, velocidad).
- **Límite de Datos a Mostrar**: Otro campo de texto permite establecer la **cantidad máxima de registros** que se muestran simultáneamente en la lista (por ejemplo, 10 registros). Cuando se supera este límite, los registros más antiguos se eliminan de la visualización (aunque se siguen almacenando internamente para la exportación).

---

### 3. Funciones Clave

1. **Iniciar Grabación**
    - Habilita el servicio de ubicación del dispositivo y comienza a tomar lecturas del GPS.
    - Cada registro incluye la hora exacta de captura, latitud, longitud y se calcula la velocidad en metros por segundo basándose en la posición anterior.
2. **Detener Grabación**
    - Finaliza la recolección de datos y detiene el servicio de ubicación.
    - No se siguen añadiendo registros al listado ni al archivo interno.
3. **Visualización de Velocidad**
    - En la pantalla se muestra en tiempo real la velocidad estimada, calculada con una fórmula de distancia geográfica (Haversine) dividida entre el tiempo transcurrido entre mediciones.
4. **Exportar CSV**
    - Al presionar el botón de exportación, la aplicación guarda o actualiza el archivo CSV en la memoria interna del dispositivo.
    - En dispositivos Android, también copia el archivo a la carpeta de descargas para un fácil acceso.
5. **Eliminar CSV Local**
    - Permite borrar por completo el archivo CSV que se encuentra en la ruta de almacenamiento interno, reiniciando así los datos guardados.
6. **Limitar la Cantidad de Registros en Pantalla**
    - Permite que la interfaz se mantenga limpia, mostrando solo los últimos N registros (configurados por el usuario). Los más antiguos se van removiendo de la vista, pero continúan almacenados internamente para su exportación, a menos que se haya optado por eliminar el CSV local.
7. **Persistencia de Contador de Registros**
    - Se utiliza un contador que registra cuántos datos se han capturado hasta el momento. Este contador se guarda de manera persistente en el dispositivo, de modo que, si la aplicación se cierra y se vuelve a abrir, se conserva el historial de la cantidad de mediciones realizadas.

---

### 4. Flujo de Uso

1. **Apertura de la Aplicación**
    - La app verifica y solicita permisos de ubicación y almacenamiento en caso de no tenerlos concedidos.
2. **Configuración Inicial**
    - El usuario establece el intervalo de captura (en segundos) y el número máximo de registros visibles en pantalla.
3. **Inicio de la Recolección**
    - Al presionar el botón principal, se activa el servicio de GPS. La aplicación comienza a generar un nuevo registro en cada intervalo establecido.
    - Cada registro aparece en la tabla, mostrando su hora, coordenadas y velocidad.
4. **Monitoreo y Visualización**
    - El usuario puede ver en la interfaz cómo se incrementa la lista de registros y observar su velocidad actual.
    - Cuando el número de registros excede el límite configurado, los más antiguos desaparecen de la vista.
5. **Finalización o Pausa**
    - El usuario puede detener la grabación en cualquier momento presionando el mismo botón.
    - El servicio de ubicación se detiene para conservar batería y recursos.
6. **Exportación de Datos**
    - El usuario presiona el botón de exportación para generar o actualizar el archivo CSV con todos los registros capturados.
    - En Android, el archivo también se copia a la carpeta de descargas, facilitando su envío o consulta externa.
7. **Gestión de Datos**
    - El usuario puede borrar el archivo CSV local si desea reiniciar completamente los datos y comenzar desde cero.
    - El contador de registros se mantiene en PlayerPrefs (almacenamiento interno), de modo que si la app se reinicia, este contador continúa desde el último valor registrado.

---

### 5. Diseño Visual

La imagen de la interfaz muestra una **estética oscura y minimalista**, donde los componentes principales se encuentran claramente organizados:

- **Título en la parte superior** con un ícono central de GPS.
- **Botón de “Export Data”** con un ícono de descarga o de archivo CSV.
- **Tabla con cabeceras** para TimeStamp, Latitud, Longitud y Velocidad.
- **Campos de texto** para configurar el intervalo de captura y el límite de registros.
- **Visualización de Velocidad** en un texto destacado.

Este diseño intuitivo permite que cualquier usuario, sin conocimientos técnicos avanzados, pueda utilizar la aplicación para recolectar y gestionar datos de geolocalización de forma sencilla y rápida.

---

**Conclusión:**

La aplicación **GeoDataRecollector** ofrece una solución completa para quienes necesiten registrar y analizar información de posición y velocidad en tiempo real. Su capacidad de exportar a un archivo CSV, su facilidad de uso y su interfaz clara la convierten en una herramienta versátil para proyectos de seguimiento, monitoreo de rutas, recolección de datos de campo y más.
