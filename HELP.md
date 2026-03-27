# HELP - Manual de operación de VideoEditor

Este documento describe cómo operar VideoEditor en un entorno de trabajo diario.

## 1. ¿Qué es VideoEditor?

VideoEditor es una aplicación de escritorio (WPF/MVVM) que utiliza FFmpeg, FFprobe y FFplay para:

- ejecutar operaciones de edición/transcodificación,
- inspeccionar metadatos multimedia,
- previsualizar segmentos con marcadores,
- administrar una cola persistente de trabajos,
- exportar diagnósticos para soporte y liberación.

---

## 2. Requisitos previos

1. **Sistema**: Windows con .NET SDK 8.0+.
2. **Herramientas multimedia**:
   - `ffmpeg`
   - `ffprobe`
   - `ffplay` (recomendado para previsualización)
3. **Acceso de escritura** en:
   - `%AppData%/VideoEditor/` (archivo `tools.json`)
   - carpeta `data/` junto al ejecutable (base `jobs.db`)

---

## 3. Inicio rápido

1. Inicie la aplicación.
2. En panel **Settings**, configure **Tools directory** (opcional si usa PATH).
3. Pulse **Rescan tools**.
4. Verifique en **Startup diagnostics**:
   - ruta de ffmpeg/ffprobe,
   - versión de ffmpeg,
   - resumen de codecs,
   - métodos de aceleración.

Si aparece error de herramienta faltante, corrija rutas o PATH y vuelva a escanear.

---

## 4. Flujo de operación recomendado

### Paso A: validar entorno

- Revise **Startup diagnostics**.
- Confirme que no hay `BlockingError`.

### Paso B: analizar archivo de entrada (probe)

1. Ingrese `InputPath` en el panel de preview.
2. Pulse **Probe**.
3. Verifique estado: cantidad de puntos de seek y duración detectada.

### Paso C: previsualizar

- **Play I/O**: reproduce entre marcadores `In` y `Out`.
- **Play A/B**: compara dos segmentos (`AStart/AEnd` contra `BStart/BEnd`).
- **Quick Seek**: salta al punto elegido del combo de seek.
- **Stop**: detiene la reproducción activa.

Controles disponibles:

- `SubOffset`: offset de subtítulos.
- `Speed`: factor de velocidad de reproducción.

### Paso D: ejecutar operación

Dependiendo del módulo/flujo:

- Trim
- Transcode
- Concat
- Otras operaciones soportadas por el constructor de comandos

Puede ejecutar de forma directa o crear/encolar trabajos.

### Paso E: administrar cola

Operaciones de cola disponibles:

- Crear borrador (draft)
- Encolar
- Pausar
- Reanudar
- Cancelar
- Reintentar

La cola persiste en SQLite y se recupera al reiniciar.

---

## 5. Catálogo de operaciones soportadas

El motor de comandos contempla:

1. Trim
2. Extract Audio
3. Extract Video
4. Convert/Transcode
5. Concat
6. Normalize Loudness
7. Subtitle (burn-in o mux)
8. Thumbnail / Contact Sheet
9. Audio Channel Map + Resample
10. Watermark Overlay (imagen o texto)
11. Speed / Framerate
12. Segmentación HLS

---

## 6. Diagnóstico y soporte

### Exportar bundle de diagnósticos

La cola permite exportar un ZIP con:

- `jobs.json`
- `artifacts/<job-id>.json` por trabajo con artefactos

Úselo para soporte técnico, análisis de fallas o checklist de release.

### Datos útiles para depuración

- Código de salida del proceso
- Comando ejecutado
- `stdout` / `stderr`
- Tiempos de inicio y fin
- Rutas de salida esperadas

---

## 7. Estados de trabajo (job lifecycle)

Estados posibles:

- `Draft`
- `Queued`
- `Running`
- `Paused`
- `Succeeded`
- `Failed`
- `Cancelled`

Notas:

- Un job en `Running` o `Queued` puede recuperarse como `Queued` tras reinicio.
- `RetryPolicy` controla número de intentos y demora entre reintentos.

---

## 8. Ubicación de archivos y configuración

- **Tool paths**: `%AppData%/VideoEditor/tools.json`
- **Base de jobs**: `data/jobs.db` (junto al binario)
- **Diagnósticos**: carpeta elegida por usuario (ZIP timestamp)

---

## 9. Problemas comunes y solución

### Problema: “ffmpeg/ffprobe no encontrado”

Acciones:

1. Configure `Tools directory` correcto.
2. Pulse **Rescan tools**.
3. Verifique que los binarios existen físicamente.
4. Si usa PATH, cierre/reabra sesión o app para refrescar variables.

### Problema: preview no inicia

Acciones:

1. Confirme que `ffplay` esté disponible.
2. Verifique que `InputPath` exista.
3. Revise si los marcadores contienen valores válidos (`End > Start`).

### Problema: job falla con exit code != 0

Acciones:

1. Revise artefacto del job (`stderr`).
2. Valide parámetros de operación y rutas de salida.
3. Reintente con menor complejidad (por ejemplo, transcode básico).

---

## 10. Checklist operacional diario

1. Abrir app y verificar diagnósticos.
2. Cargar archivo y ejecutar probe.
3. Definir marcadores y validar preview.
4. Lanzar operación y supervisar cola.
5. Si falla, exportar diagnóstico y adjuntarlo al reporte.

---

## 11. Build/Test para operadores técnicos

```bash
dotnet restore VideoEditor.sln
dotnet build VideoEditor.sln
dotnet test VideoEditor.sln
```

### Uso correcto en Visual Studio

1. Use `VideoEditor.UI` como proyecto de inicio para ejecutar la aplicacion con `F5`.
2. Ejecute `VideoEditor.Tests` desde `Test Explorer`.
3. Si ve que `dotnet.exe` termina con codigo `0`, interpretelo como cierre normal del proceso, no como evidencia de que la suite se haya validado.

---

## 12. Referencias internas

- `README.md` (visión general técnica)
- `docs/ReleaseChecklist.md`
- `docs/DesignerIntegrityChecklist.md`
- `docs/CodingStandards.md`
