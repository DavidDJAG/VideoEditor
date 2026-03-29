# VideoEditor

VideoEditor is a desktop media operations workbench built on **.NET 8 + WPF + MVVM** that orchestrates **FFmpeg, FFprobe, and FFplay** for probing, preview, conversion, queue-driven execution, and diagnostics.

The solution is organized into **UI**, **Application**, **Domain**, **Infrastructure**, and **Tests** layers so that FFmpeg command generation, validation, queue orchestration, persistence, and operator workflows remain maintainable.

## Project goals

- Provide a clean desktop front-end for advanced FFmpeg workflows.
- Centralize command generation in a validation-first application layer.
- Support both direct execution and persistent queue-based processing.
- Surface toolchain diagnostics, media probe context, and operator guidance in the UI.
- Keep the Convert module usable for non-experts while exposing advanced controls for technical users.

## Core capabilities

## 1) Media operations supported by the command pipeline

The command builder validates operation requests and generates FFmpeg arguments for:

- Trim
- Extract audio
- Extract video
- Convert / transcode
- Concatenate inputs
- Loudness normalization
- Subtitle burn-in / mux
- Thumbnail / contact sheet generation
- Audio channel map + resample
- Watermark overlay (image or drawtext)
- Speed / framerate adjustment
- HLS segmentation

Operation metadata is defined through `OperationKind` and related request models in `VideoEditor.Domain`.

## 2) Convert module: current professional feature set

The **Convert** tab is now a full transcode workspace rather than a single-button action.

### 2.1 Conversion model and command generation

Convert is backed by a richer model (`ConvertOptions`) and a block-based command builder that supports:

- video/audio stream modes: `Encode`, `Copy`, `Disable`
- container selection
- overwrite policy
- faststart
- hardware acceleration hints
- video bitrate or constant-quality workflows
- codec/preset/tune/pixel-format selection
- fps override
- scale
- profile / level / GOP
- audio bitrate / sample rate / channels / channel layout
- 2-pass encoding
- deinterlace
- crop / pad
- simple audio normalization (`loudnorm`, `dynaudnorm`)
- stream mapping
- subtitle handling
- metadata and chapter policies

### 2.2 Dynamic capabilities from the real FFmpeg installation

The Convert UI loads capabilities from the installed FFmpeg toolchain and uses them to drive choices and warnings.

Detected capability areas include:

- encoders
- muxers / containers
- pixel formats
- hardware acceleration methods

This allows codec and container selectors to reflect what the local FFmpeg build can actually do.

### 2.3 Input context from FFprobe

Convert uses real input context gathered through `ffprobe`, including:

- container
- duration
- file size
- video/audio/subtitle stream inventory
- stream-level codec, language, title, and default/forced flags
- detected video dimensions and frame rate
- detected audio properties

This context powers better validation, stream selection, and preset guidance.

### 2.4 Presets and professional workflow helpers

Convert includes:

- built-in presets
- persistent user presets saved in application settings
- import/export of presets in JSON
- adaptive preset guidance based on capabilities + input context
- live FFmpeg command preview
- quick command copy
- validation and advisory messages in the Convert tab

### 2.5 Queue integration from Convert

Convert can now interact directly with the queue system:

- create draft
- add to queue
- view queue summary
- view last Convert job
- review recent Convert history
- refresh queue snapshot
- batch submission from the Convert tab

### 2.6 Batch and naming workflows

Convert supports batch-oriented preparation with:

- multiple input list management
- batch draft creation
- batch enqueue
- output naming templates
- suggested output names built from tokens such as preset, codec, height, fps, and container

### 2.7 Subtitles, metadata, and stream orchestration

Convert supports:

- explicit source stream selection for video/audio
- additional mapped audio streams
- additional mapped subtitle streams
- subtitle modes: `Disable`, `Copy`, `Encode`, `BurnIn`
- container-aware subtitle codec defaults
- metadata policy: preserve / strip / override selected tags
- chapter policy: preserve / strip

## 3) Built-in Convert presets

The built-in preset library includes, depending on capability availability and current context:

- `Balanced H.264 MP4`
- `Efficient H.265 MP4`
- `Stream Copy / Remux`
- `AV1 1440p 10-bit MKV`

The reference AV1 preset matches this intended profile:

- video codec: `libsvtav1`
- preset: `6`
- CRF: `28`
- pixel format: `yuv420p10le`
- audio codec: `libopus`
- audio bitrate: `128k`
- container: `mkv`

Equivalent FFmpeg shape:

```bash
ffmpeg -i input -c:v libsvtav1 -preset 6 -crf 28 -pix_fmt yuv420p10le -c:a libopus -b:a 128k -y output.mkv
```

## 4) Toolchain detection and startup diagnostics

VideoEditor resolves tools from:

1. Explicit configured path
2. Configured tools directory
3. `PATH`

Startup diagnostics expose:

- resolved ffmpeg / ffprobe / ffplay locations
- FFmpeg version string
- codec summary
- hardware acceleration summary
- remediation guidance when binaries are missing

## 5) Media probe and preview workflows

The preview/probe workflow supports:

- probing input files with JSON ffprobe output
- seek point generation
- playback between I/O markers
- A/B segment comparison
- quick seek playback
- subtitle offset and playback speed controls
- stop active playback process

## 6) Queue orchestration and persistence

The queue layer includes:

- draft creation
- enqueue, pause, resume, cancel, retry
- background processing with configurable concurrency gate
- startup recovery of queued/running jobs
- retry policy support
- history filtering
- per-job execution artifacts
- diagnostics bundle export (`jobs.json` + artifacts ZIP)

Persistence locations:

- jobs database: `data/jobs.db`
- tool/preset/settings JSON: `%AppData%/VideoEditor/`

## 7) Solution structure

- `VideoEditor.UI` — WPF presentation layer, views, view-models, themes
- `VideoEditor.Application` — command builder, request factory, queue coordination, convert intelligence
- `VideoEditor.Domain` — models for operations, jobs, probes, settings, toolchain snapshot, convert presets/options
- `VideoEditor.Infrastructure` — FFmpeg/FFprobe/FFplay services, process execution, SQLite job store, JSON settings persistence, toolchain resolution
- `VideoEditor.Tests` — unit and integration tests

## 8) Testing coverage

The test suite covers, among other areas:

- command builder behavior
- convert intelligence and compatibility guidance
- ffprobe JSON parsing
- operation validation
- queue transitions and queue service behavior
- operation request factory behavior
- integration fixtures for deterministic assertions

## Build & run

## Prerequisites

- Windows
- .NET SDK 8.0+
- FFmpeg toolchain available (`ffmpeg`, `ffprobe`, optionally `ffplay`)
- Visual Studio 2026 IDE or `dotnet` CLI

### CLI

```bash
dotnet restore VideoEditor.sln
dotnet build VideoEditor.sln
dotnet test VideoEditor.sln
```

Run the UI project with:

```bash
dotnet run --project VideoEditor.UI/VideoEditor.UI.csproj
```

### Visual Studio workflow

- Open `VideoEditor.sln`.
- Use `VideoEditor.UI` as the startup project to launch the app with `F5`.
- Use `Test > Test Explorer` to run `VideoEditor.Tests`.
- Do not use `F5` over the test project as the primary validation workflow.

## Operational notes

- On startup, the application loads queue state and toolchain diagnostics.
- If FFmpeg or FFprobe cannot be resolved, the dashboard/settings surfaces remediation hints.
- Convert adapts its available options to the detected FFmpeg installation and the currently analyzed input file.
- Queue diagnostics export creates a timestamped ZIP package for troubleshooting and release validation.

## Documentation

- `HELP.md` — operator guide and day-to-day workflows
- `VideoEditor.Tests/README.md` — how to execute and interpret the automated tests
- `docs/ReleaseChecklist.md` — release validation steps
- `docs/DesignerIntegrityChecklist.md` — XAML designer checks
- `docs/CodingStandards.md` — coding and quality expectations
