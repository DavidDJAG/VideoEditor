# VideoEditor

VideoEditor is a desktop video operations workbench built on **.NET + WPF + MVVM** that orchestrates **FFmpeg/FFprobe/FFplay** for media processing, probing, preview, and queue-driven execution.

## Project goals

- Provide a clean architecture split into **UI**, **Application**, **Domain**, and **Infrastructure** layers.
- Centralize FFmpeg command generation with validation-first operation requests.
- Support direct execution and queued execution with persistence and diagnostics export.
- Offer startup diagnostics for toolchain readiness (binary resolution, version, codecs, hardware acceleration).

## Core capabilities

### 1) FFmpeg operation support (command builder)

The command pipeline validates operation requests and builds FFmpeg arguments for:

- Trim (`TrimRequest`)
- Extract audio (`ExtractAudioRequest`)
- Extract video (`ExtractVideoRequest`)
- Convert/transcode (`ConvertRequest` with `EncodingProfile`)
- Concatenate inputs (`ConcatRequest`)
- Loudness normalization (`NormalizeLoudnessRequest`)
- Subtitles (burn-in or mux) (`SubtitleRequest`)
- Thumbnails/contact sheet generation (`ThumbnailContactSheetRequest`)
- Audio channel map/resample (`AudioChannelMapResampleRequest`)
- Watermark overlay (image or drawtext) (`WatermarkOverlayRequest`)
- Speed/framerate adjustment (`SpeedFramerateRequest`)
- HLS segmentation (`SegmentHlsRequest`)

### 1.1) Scope baseline by release phase

- **v1 funcional (MVP):** cut/trim, join/concat, split A/V (`Trim`, `Concat`, `ExtractAudio`, `ExtractVideo`).
- **v1.1+:** normalize loudness, subtitles, watermark, speed/framerate, HLS (plus transcode and advanced builder-backed operations).
- Operation metadata lives in `OperationCatalog` (`VideoEditor.Domain`) through `OperationKind` + descriptor (visible name, request type, key validations, release phase).

### 2) Toolchain detection and capabilities snapshot

VideoEditor resolves tools from:

1. Explicit configured path
2. Configured tools directory
3. PATH environment

It captures and exposes:

- Resolved ffmpeg/ffprobe/ffplay locations
- FFmpeg version string
- Detected video codec list
- Detected hardware acceleration methods
- Human-readable remediation guidance when binaries are missing

### 3) Media probe and metadata parsing

- Runs ffprobe with JSON output.
- Parses duration, file size, container format, stream counts, dimensions, fps, sample rate, and channels.
- Uses filesystem size fallback if ffprobe size metadata is missing.

### 4) Preview and playback workflows

Preview module supports:

- Load probe and auto-generate seek points
- I/O marker playback (in/out)
- A/B segment preview composition
- Quick seek playback from selected timestamp
- Subtitle timing offset and playback speed factor controls
- Stop active playback process

### 5) Queue orchestration (with persistence)

Queue service includes:

- Draft job creation
- Enqueue, pause, resume, cancel, retry
- Background processing with configurable max concurrency gate
- Recovery of queued/running jobs on startup (running jobs are re-queued)
- Retry policy with delay between attempts
- Job history filtering by state, date range, and search text
- Artifact capture per job (command, stdout/stderr, exit code, timestamps, outputs)
- Diagnostics bundle export (`jobs.json` + per-job artifacts, ZIP package)

### 6) Storage and settings

- Job persistence in SQLite (`data/jobs.db` under app base directory).
- Tool path settings persisted as JSON in `%AppData%/VideoEditor/tools.json`.
- Tool directory rescan from UI that updates dashboard diagnostics.

### 7) UI architecture and design-time support

- Main window composes dashboard + settings + preview workflows.
- Dedicated module view-models: Trim, Transcode, Concat, Probe, Preview, Queue, Settings.
- Design-time data contexts for XAML designer integrity.
- Theme/resource dictionaries for colors, controls, spacing, typography, and strings.

### 8) Testing coverage areas

The test suite targets:

- Command builder behavior
- Operation validation
- Queue transitions and queue service behavior
- FFprobe JSON parsing
- Integration fixtures for deterministic assertions

## Solution structure

- `VideoEditor.UI` — WPF presentation layer and module view-models
- `VideoEditor.Application` — application abstractions and command-building service
- `VideoEditor.Domain` — records/models for operations, jobs, policies, probes, tool paths
- `VideoEditor.Infrastructure` — process execution, toolchain resolution, FFmpeg/FFprobe/playback services, SQLite store, settings persistence
- `VideoEditor.Tests` — unit and integration tests

## Build & run

## Prerequisites

- .NET SDK 8.0+
- FFmpeg toolchain available (`ffmpeg`, `ffprobe`, optionally `ffplay`)

### Commands

```bash
dotnet restore VideoEditor.sln
dotnet build VideoEditor.sln
dotnet test VideoEditor.sln
```

Run the UI project from Visual Studio (Windows) or with:

```bash
dotnet run --project VideoEditor.UI/VideoEditor.UI.csproj
```

### Visual Studio workflow

- Use `VideoEditor.UI` as the startup project when you want to launch the application with `F5`.
- Use `Test > Test Explorer` to run `VideoEditor.Tests`.
- Do not rely on `F5` over `VideoEditor.Tests` as the way to validate tests; the expected workflow for that project is `Test Explorer` or `dotnet test`.

## Operational notes

- On startup, queue data is initialized and dashboard diagnostics are loaded.
- If FFmpeg/FFprobe are not found, dashboard and settings display a remediation message.
- Queue diagnostics export creates a timestamped ZIP package for troubleshooting and release validation.

## Documentation

- `HELP.md` — operator manual (daily usage guide)
- `docs/ReleaseChecklist.md` — release validation steps
- `docs/DesignerIntegrityChecklist.md` — XAML designer checks
- `docs/CodingStandards.md` — coding and quality expectations
