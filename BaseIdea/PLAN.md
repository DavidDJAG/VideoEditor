# Plan de Trabajo: App WinForms C# .NET 9 para Operaciones de Video/Audio con FFmpeg

## Resumen
Crear una aplicación de escritorio en **C# .NET 9 WinForms** orientada a un uso **técnico**, que encapsule comandos de `ffmpeg` y `ffprobe` ya disponibles en `PATH` de Windows. La v1 cubrirá como base los scripts actuales y los ampliará con operaciones frecuentes de edición/utilidad, usando una **cola de trabajos** para ejecutar tareas largas sin bloquear la interfaz. Tener en cuenta que dicho proyecto debe ser totalmente compatible con Visual Studio 2026 IDE, es decir 
dener cuidado a la hora de crear archivos .Designer.cs de modo que la IDE pueda mostrar el formulario gráfico diseñado.

El punto de partida funcional será:
- Recorte de video por tiempo.
- Extracción de audio.
- Extracción de video sin audio.
- Inspección técnica con `ffprobe`.
- Conversión/transcodificación con presets.

Se agregarán en v1:
- Unión/concatenación de archivos compatibles.
- Conversión de formatos con perfiles predefinidos.
- Procesamiento por lote mediante cola.
- Historial/log de comandos ejecutados.
- Validación previa de entradas y parámetros.

## Cambios e Implementación
### 1. Arquitectura de la aplicación
- Estructurar la solución en capas simples:
  - `UI WinForms`: formularios, validaciones visuales, progreso y resultados.
  - `Application/Services`: construcción de comandos, orquestación de tareas, parseo de salida.
  - `Domain/Models`: definiciones de trabajos, presets, resultados, metadatos.
  - `Infrastructure`: invocación de procesos externos (`ffmpeg`/`ffprobe`) y manejo de archivos temporales.
- Mantener FFmpeg desacoplado mediante un servicio como `IFfmpegService` y `IFfprobeService`, para que la UI no construya comandos directamente.
- Resolver la ruta del ejecutable así:
  - Primero intentar `ffmpeg` y `ffprobe` desde `PATH`.
  - Si fallan, permitir configurar manualmente la carpeta binaria.
  - Mostrar el diagnóstico en una pantalla/configuración técnica.

### 2. Módulos funcionales de v1
- **Recorte**:
  - Entrada de archivo, tiempo inicial, tiempo final o duración.
  - Opción de corte rápido con `-c copy` y opción de recodificación si el usuario necesita precisión.
- **Extracción de audio**:
  - Modos `copy` y recodificación.
  - Presets iniciales: `M4A copy`, `MP3 calidad alta`, `WAV PCM`.
- **Extracción de video**:
  - Generar video sin pista de audio.
- **Inspección técnica**:
  - Ejecutar `ffprobe`.
  - Mostrar streams, duración, códecs, resolución, fps, bitrate y contenedor.
  - Permitir copiar el resultado técnico o exportarlo como texto/JSON.
- **Conversión/transcodificación**:
  - Presets iniciales:
    - H.264 + AAC compatible.
    - H.265/HEVC + AAC.
    - AV1 + Opus alto desempeño, inspirado en tu script.
    - Solo audio a MP3.
  - Exponer parámetros técnicos clave porque prefieres una UX técnica:
    - Códec de video.
    - Códec de audio.
    - CRF o calidad.
    - Preset.
    - Bitrate de audio.
    - Pixel format.
    - Contenedor de salida.
- **Unión/concatenación**:
  - Selección ordenada de varios archivos.
  - Validar compatibilidad de códecs y parámetros para concat por copia.
  - Si no son compatibles, informar y ofrecer estrategia de recodificación previa como trabajo separado.
- **Cola de trabajos**:
  - Permitir agregar múltiples operaciones.
  - Estados: `Pendiente`, `Ejecutando`, `Completado`, `Error`, `Cancelado`.
  - Procesamiento secuencial en background.
  - Cancelación cooperativa del proceso activo.
- **Historial y logs**:
  - Guardar comando generado, timestamps, salida estándar/error y código de salida.
  - Asociar cada job con su log para diagnóstico.

### 3. Diseño de UI WinForms
- Usar un **formulario principal con layout técnico**:
  - Panel izquierdo o superior para seleccionar la operación.
  - Panel central con parámetros específicos de la operación.
  - Panel derecho o inferior para cola, progreso y logs.
- Pantallas o tabs recomendadas:
  - `Operaciones`
  - `Cola`
  - `Inspección`
  - `Configuración`
  - `Historial`
- Elementos clave:
  - Selectores de archivo/carpeta.
  - Campos para tiempos con validación estricta (`hh:mm:ss[.ms]`).
  - ComboBoxes para presets y códecs.
  - TextBox de solo lectura para vista previa del comando generado.
  - Barra de progreso y estado textual.
  - Visor de salida en tiempo real.
- Mantener una opción “mostrar comando FFmpeg” para facilitar trazabilidad y depuración técnica.

### 4. Interfaces públicas, modelos y contratos
- Definir modelos explícitos para evitar lógica dispersa:
  - `MediaJob`
  - `MediaJobType`
  - `MediaJobRequest`
  - `MediaJobResult`
  - `MediaProbeResult`
  - `EncodingPreset`
  - `TimeRange`
- Interfaces recomendadas:
  - `IFfmpegCommandBuilder`
  - `IFfmpegProcessRunner`
  - `IFfmpegService`
  - `IFfprobeService`
  - `IJobQueueService`
  - `ILogStore`
- El `CommandBuilder` debe transformar requests tipadas en argumentos válidos, sin que el formulario concatene strings manualmente.
- El runner debe capturar stdout/stderr de forma asíncrona para actualizar progreso y logs.
- El parser de progreso puede basarse inicialmente en líneas de FFmpeg (`time=`, `speed=`, `fps=`), sin depender de integración compleja adicional.

### 5. Validaciones, errores y comportamiento operativo
- Validar antes de encolar:
  - Existencia del archivo de entrada.
  - Formato correcto de tiempos.
  - Que el tiempo final sea mayor que el inicial.
  - Que el archivo de salida no sobrescriba sin confirmación.
  - Que el contenedor elegido sea compatible con los códecs seleccionados.
- Manejo de errores:
  - Si FFmpeg no existe o no responde, mostrar error de entorno con instrucciones claras.
  - Si el proceso devuelve código distinto de cero, marcar el job como fallido y conservar el log completo.
  - Si el usuario cancela, matar el proceso activo de forma controlada y marcar estado `Cancelado`.
- Archivos temporales:
  - Necesarios para concat mediante archivo-lista.
  - Limpiar al finalizar o al cancelar cuando corresponda.

## Plan de Pruebas
- **Pruebas unitarias**:
  - Generación de comandos por cada tipo de operación.
  - Validación de tiempos y parámetros.
  - Selección correcta de presets.
  - Compatibilidad contenedor/códec en reglas básicas.
- **Pruebas de integración**:
  - Ejecución real de `ffprobe` sobre un archivo de muestra.
  - Ejecución real de `ffmpeg` con muestras cortas para:
    - recorte,
    - extracción de audio,
    - extracción de video,
    - conversión,
    - concatenación.
- **Pruebas funcionales de UI**:
  - Encolar varios trabajos y verificar que la UI no se bloquee.
  - Cancelar un trabajo en curso.
  - Revisar que el log y el comando mostrado coincidan con la ejecución real.
  - Verificar mensajes de error cuando falta el binario o la entrada es inválida.
- **Escenarios de aceptación**:
  - Un usuario puede tomar un `.mp4`, recortarlo y guardar el resultado.
  - Un usuario puede extraer audio a `.mp3`.
  - Un usuario puede inspeccionar un archivo y ver metadatos claros.
  - Un usuario puede lanzar una conversión AV1/Opus basada en preset.
  - Un usuario puede encolar varias tareas y seguir trabajando en la interfaz.

## Supuestos y decisiones por defecto
- Se usará **.NET 9 WinForms** en Windows, sin dependencia obligatoria de paquetes externos para la invocación de procesos.
- La aplicación asumirá que `ffmpeg` y `ffprobe` están en `PATH`, con fallback a ruta configurable manualmente.
- La UX será **técnica**, por lo que varios parámetros avanzados estarán visibles en v1.
- La ejecución será mediante **cola secuencial**, no paralela.
- La concatenación v1 priorizará el caso de archivos compatibles; la recodificación automática para unificar entradas puede dejarse como mejora posterior si no se define más detalle.
- La persistencia local mínima recomendada para v1 será:
  - configuración de rutas,
  - último directorio usado,
  - presets,
  - historial reciente de trabajos.
- No se asume edición frame-accurate tipo timeline; la app será un **frontend técnico para FFmpeg**, no un editor visual no lineal.
