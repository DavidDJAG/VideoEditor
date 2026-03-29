# HELP - Guía de operación de VideoEditor

Este documento reúne la guía operativa del sistema para uso diario. Está pensado para sustituir la información dispersa de entregas intermedias y dejar una referencia estable del comportamiento actual de la aplicación.

## 1. ¿Qué es VideoEditor?

VideoEditor es una aplicación de escritorio construida con **WPF + MVVM + .NET 9** que utiliza **FFmpeg, FFprobe y FFplay** para:

- analizar archivos multimedia,
- previsualizar segmentos,
- ejecutar conversiones y otras operaciones de edición,
- administrar una cola persistente de trabajos,
- exportar diagnósticos de ejecución.

## 2. Requisitos previos

1. **Sistema**: Windows.
2. **SDK**: .NET 9.0 o superior.
3. **Herramientas**:
   - `ffmpeg`
   - `ffprobe`
   - `ffplay` (recomendado para Preview)
4. **Acceso de escritura** en:
   - `%AppData%/VideoEditor/`
   - carpeta `data/` junto al ejecutable o proyecto

## 3. Archivos y persistencia

Ubicaciones relevantes:

- **Directorio de jobs**: `data/jobs/` (un JSON por job)
- **Configuración y presets persistentes**: `%AppData%/VideoEditor/`
- **Ruta de herramientas**: archivo JSON de settings bajo `%AppData%/VideoEditor/`
- **Bundles de diagnóstico**: carpeta elegida por el operador al exportar

## 4. Puesta en marcha inicial

1. Inicie la aplicación.
2. Abra **Settings**.
3. Configure la carpeta de herramientas si FFmpeg no está disponible en `PATH`.
4. Pulse **Rescan tools**.
5. Verifique en el dashboard o en startup diagnostics:
   - rutas resueltas de `ffmpeg`, `ffprobe` y `ffplay`,
   - versión de FFmpeg,
   - resumen de codecs,
   - aceleración por hardware detectada.

Si faltan binarios, corrija la ruta o el `PATH` y vuelva a escanear.

## 5. Módulos principales

La aplicación se organiza alrededor de estos flujos:

- Dashboard / diagnostics
- Settings
- Preview / Probe
- Queue
- Convert
- otras operaciones soportadas por el motor de comandos (trim, concat, extract audio/video, subtítulos, thumbnails, watermark, HLS, etc.)

## 6. Flujo operativo recomendado

### Paso 1: verificar el entorno

Antes de procesar archivos, confirme que:

- `ffmpeg` y `ffprobe` están resueltos,
- no hay errores bloqueantes en diagnósticos,
- el snapshot de capacidades se cargó correctamente.

### Paso 2: analizar el archivo de entrada

Use el flujo de **Probe** o el propio panel **Convert** para analizar el archivo.

El análisis real vía `ffprobe` permite obtener:

- contenedor,
- duración,
- tamaño,
- streams de video,
- streams de audio,
- streams de subtítulos,
- idioma, título y flags por stream,
- resolución y FPS,
- sample rate y canales.

### Paso 3: previsualizar si hace falta

En **Preview** puede:

- reproducir entre marcadores `In` y `Out`,
- comparar segmentos A/B,
- hacer quick seek,
- ajustar offset de subtítulos,
- cambiar velocidad de reproducción,
- detener la reproducción activa.

### Paso 4: configurar la operación

Según el caso, use la solapa apropiada. Para flujos de transcodificación, use **Convert**, que es el módulo más completo del sistema.

### Paso 5: decidir si el trabajo se ejecuta o se encola

La aplicación permite:

- crear **Draft**,
- agregar a la **Queue**,
- reintentar,
- pausar,
- reanudar,
- cancelar.

La cola es persistente y se recupera al reiniciar.

## 7. Guía operativa de Convert

La solapa **Convert** es un panel de transcodificación avanzado. Reemplaza el flujo anterior de “Input + Output + Run” por una operación mucho más completa.

### 7.1 Secciones de la solapa Convert

Según la versión actual del proyecto, Convert puede incluir estas áreas:

- origen / input
- análisis del archivo con `ffprobe`
- preset library
- opciones de video
- opciones de audio
- opciones de subtítulos
- metadata y chapters
- output / naming
- validaciones y advisories
- preview del comando FFmpeg
- acciones de cola y batch

### 7.2 Flujo recomendado en Convert

#### A. Seleccionar input y output

- indique archivo de entrada,
- indique archivo de salida o una carpeta objetivo si usa batch,
- verifique que input y output no sean el mismo archivo,
- si cambia contenedor, sincronice la extensión del archivo de salida.

#### B. Analizar el input

Pulse **Analyze** cuando corresponda. Esto habilita:

- validaciones más precisas,
- detección de streams disponibles,
- recomendaciones automáticas,
- presets adaptativos,
- restricciones de compatibilidad más confiables.

#### C. Elegir un preset

Puede usar:

- presets built-in,
- presets guardados por el usuario,
- presets importados desde JSON.

Presets built-in importantes:

- `Balanced H.264 MP4`
- `Efficient H.265 MP4`
- `Stream Copy / Remux`
- `AV1 1440p 10-bit MKV`

#### D. Ajustar video

Controles habituales:

- modo: `Encode`, `Copy`, `Disable`
- codec
- control de tasa: `CRF/CQ` o `Bitrate`
- `Preset`
- `Tune`
- pixel format
- FPS
- escala
- profile / level / GOP
- 2-pass
- deinterlace
- crop
- pad

Reglas importantes:

- si video está en `Copy`, no se aplican filtros ni cambio de FPS ni scale,
- `2-pass` debe usarse con control por bitrate,
- crop/pad/deinterlace requieren video en `Encode`.

#### E. Ajustar audio

Controles habituales:

- modo: `Encode`, `Copy`, `Disable`
- codec
- bitrate
- sample rate
- channels
- channel layout
- normalización simple:
  - `Loudnorm`
  - `Dynaudnorm`

Regla importante:

- la normalización requiere audio en `Encode`.

#### F. Ajustar subtítulos

Flujos soportados:

- `Disable`
- `Copy`
- `Encode`
- `BurnIn`

Comportamiento esperado:

- `BurnIn` incrusta el subtítulo sobre el video,
- `Copy` conserva el stream de subtítulos,
- `Encode` permite adecuar el codec al contenedor,
- si no hay subtítulos en el input, las opciones dependientes del stream quedan limitadas.

#### G. Mapear streams

Convert permite trabajar con streams reales del input:

- stream principal de video,
- stream principal de audio,
- streams adicionales de audio,
- streams adicionales de subtítulos.

Use este bloque cuando el archivo tiene múltiples idiomas, comentarios de director, o varias pistas de subtítulos.

#### H. Metadata y chapters

Puede configurar políticas para:

- preservar metadata,
- eliminar metadata,
- aplicar overrides de campos como:
  - `title`
  - `artist`
  - `comment`
- preservar o eliminar chapters.

#### I. Output naming

Convert soporta plantillas de nombre para salida. Según la configuración de la entrega actual, puede usar tokens como:

- `{name}`
- `{preset}`
- `{container}`
- `{vcodec}`
- `{acodec}`
- `{height}`
- `{fps}`
- `{tag}`

Úselo para:

- evitar nombres manuales repetitivos,
- identificar codec y resolución en el archivo,
- generar salidas consistentes en batch.

#### J. Validar el comando

Antes de ejecutar:

- revise el bloque de validaciones,
- revise los warnings/advisories,
- revise el preview del comando FFmpeg.

Esto es especialmente importante cuando combina:

- `Copy` con cambios de filtros,
- contenedor y codec poco compatibles,
- 2-pass,
- subtítulos burn-in,
- escalado con dimensiones no ideales,
- audio normalization.

#### K. Ejecutar o encolar

Opciones disponibles desde Convert:

- **Create Draft**
- **Add to Queue**
- acciones batch equivalentes
- refresco del estado de cola

Convert también puede mostrar:

- resumen de cola,
- último job agregado,
- historial reciente de jobs de Convert.

## 8. Presets de Convert

### 8.1 Presets built-in

Son presets mantenidos por la aplicación. No deben editarse directamente como si fueran presets de usuario.

Ejemplos:

- `Balanced H.264 MP4`
- `Efficient H.265 MP4`
- `Stream Copy / Remux`
- `AV1 1440p 10-bit MKV`

### 8.2 Presets de usuario

Puede:

- guardar la configuración actual,
- cargar un preset previo,
- eliminar presets propios,
- exportarlos a JSON,
- importarlos desde JSON.

### 8.3 Preset built-in AV1 1440p 10-bit MKV

Este preset se agregó para aproximar un perfil AV1 profesional con esta forma de comando:

```bash
ffmpeg -i input -c:v libsvtav1 -preset 6 -crf 28 -pix_fmt yuv420p10le -c:a libopus -b:a 128k -y output.mkv
```

Configuración esperada:

- container: `mkv`
- video codec: `libsvtav1`
- preset: `6`
- CRF: `28`
- pixel format: `yuv420p10le`
- audio codec: `libopus`
- audio bitrate: `128k`

## 9. Batch convert

Si usa la funcionalidad de lote desde Convert:

1. agregue múltiples inputs,
2. elija una carpeta o naming apropiado,
3. valide que la plantilla no genere colisiones,
4. cree drafts o encole el lote,
5. supervise el resultado desde Queue y el resumen embebido en Convert.

## 10. Cola de trabajos

Estados posibles:

- `Draft`
- `Queued`
- `Running`
- `Paused`
- `Succeeded`
- `Failed`
- `Cancelled`

Comportamiento relevante:

- trabajos `Running` o `Queued` pueden recuperarse como `Queued` al reiniciar,
- la política de reintentos controla cantidad y demora,
- cada job conserva artefactos útiles para soporte.

## 11. Diagnóstico y soporte

La aplicación puede exportar bundles de diagnóstico con:

- `jobs.json`
- artefactos por job
- comando ejecutado
- `stdout`
- `stderr`
- exit code
- timestamps
- rutas de salida esperadas

Esto es útil para:

- soporte,
- análisis de fallas,
- validación pre-release,
- trazabilidad de ejecuciones complejas.

## 12. Problemas comunes y solución

### Problema: FFmpeg o FFprobe no se detecta

Acciones:

1. revise `Settings`,
2. configure el directorio correcto,
3. pulse **Rescan tools**,
4. confirme que los binarios también pueden resolverse desde `PATH` si corresponde.

### Problema: no aparecen ciertos codecs en Convert

Causas típicas:

- el encoder no existe en la build local de FFmpeg,
- la lista de capacidades no se recargó todavía.

Acciones:

1. use **Reload capabilities**,
2. verifique el snapshot de capacidades,
3. revise si el encoder realmente figura en `ffmpeg -encoders`.

### Problema: un preset no carga como esperaba en otra máquina

Causa típica:

- el preset fue creado en una instalación con capacidades distintas.

Acción:

- cargue el preset y revise los warnings de compatibilidad antes de encolar.

### Problema: 2-pass no funciona

Revise que:

- video esté en `Encode`,
- el control de tasa sea por `Bitrate`,
- exista escritura en la ubicación temporal usada por los logs de pass.

### Problema: filtros no tienen efecto

Revise que el stream afectado no esté en `Copy`.

### Problema: subtítulos burn-in falla

Revise que:

- exista stream de subtítulos de origen,
- video esté en `Encode`,
- la ruta del input sea válida para el filtro de subtítulos.

### Problema: el archivo de salida no reproduce como esperaba

Revise:

- compatibilidad codec/contenedor,
- pixel format,
- FPS,
- si el flujo fue `Copy` o `Encode`,
- avisos contextuales mostrados por Convert.

## 13. Recomendaciones de uso

- haga siempre análisis de input antes de una conversión compleja,
- use presets built-in como punto de partida,
- valide warnings antes de encolar,
- use naming templates en flujos batch,
- guarde presets propios para recetas recurrentes,
- exporte diagnósticos ante fallas de codec o compatibilidad.

## 14. Guía breve de build y prueba

### En Visual Studio

- abra `VideoEditor.sln`,
- use `VideoEditor.UI` como startup project,
- ejecute tests desde `Test Explorer`.

### Por consola

```bash
dotnet restore VideoEditor.sln
dotnet build VideoEditor.sln
dotnet test VideoEditor.sln
```

## 15. Documentación complementaria

- `README.md` — resumen técnico y características del producto
- `VideoEditor.Tests/README.md` — guía de ejecución de la suite de pruebas
- `docs/ReleaseChecklist.md`
- `docs/DesignerIntegrityChecklist.md`
- `docs/CodingStandards.md`
