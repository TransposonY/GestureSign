# Input recorder
Active contributors: TransposonY

`GestureSign.InputRecorder` is an optional Windows Forms utility for recording raw digitizer input and low-level mouse events as replayable `*.gsrec.json` files. These recordings let the input pipeline be investigated and regression-tested without repeatedly reproducing a gesture on hardware. It is not part of the daemon's live runtime.

## Purpose

The recorder captures real HID reports from pen, touch screen, and touch pad digitizer collections, plus mouse-hook events, device-change notifications, and the environment/settings needed to interpret them. It can capture interactive scenarios through a small UI or record for a fixed duration from the command line. The recorder never blocks or alters input and does not require GestureSign's daemon to be running.

## Directory layout

```text
GestureSign.InputRecorder/
├── Native/          recorder-specific native interop helpers
├── Recording/       raw input capture, recording schema, and device/contact decoding
├── ContactView.cs   live contact visualization
├── DeviceMonitor.cs per-device counters and decoded contact state
├── MainForm.cs      interactive recorder UI and take lifecycle
├── Program.cs       interactive and headless entry points
├── Scenarios.cs     built-in recording scenarios
└── README.md        usage, test-corpus workflow, and privacy details
```

## Important types

| Type | Responsibility |
| --- | --- |
| `Program` (`GestureSign.InputRecorder/Program.cs`) | Starts the interactive WinForms application or parses the fixed-duration headless CLI. |
| `MainForm` (`GestureSign.InputRecorder/MainForm.cs`) | Shows detected digitizers and live paths; arms, starts, stops, previews, and saves takes. |
| `InputCapture` (`GestureSign.InputRecorder/Recording/InputCapture.cs`) | Owns the raw-input window and low-level mouse hook and forwards events to the active recording session. The hook callback leaves its handled flag unchanged. |
| `RawInputWindow` (`GestureSign.InputRecorder/Recording/RawInputWindow.cs`) | Registers for pen, touchscreen, and touchpad raw HID input and device notifications, then receives `WM_INPUT` buffers. |
| `RecordingSession` (`GestureSign.InputRecorder/Recording/RecordingSession.cs`) | Collects reports, mouse events, device changes, timestamps, cursor coordinates, settings, and environment metadata for a take. |
| `InputRecording` (`GestureSign.InputRecorder/Recording/InputRecording.cs`) | Defines the versioned, language-neutral JSON recording schema and load/save methods. |
| `DeviceCatalog`, `ContactDecoder`, and `ContactTracker` (`GestureSign.InputRecorder/Recording/`) | Describe digitizer devices, decode contact reports for preview, and track active contact paths. |

## How it works

With no command-line arguments, `Program` opens `MainForm`. On load the form creates `InputCapture`, enumerates digitizers, monitors their reports, and displays decoded paths. Pressing **Record** arms a take; by default, recording starts on the next touch-down or mouse button press outside the recorder window and stops after all contacts/buttons are released for the configured delay. F5 starts, F6 or Esc cancels/stops, and Ctrl+S saves. The manual mode records until stopped. Mouse-move capture can be disabled for touch/pen-only scenarios.

`InputCapture` uses a hidden raw-input window registered as an input sink for digitizer collections, alongside a low-level mouse hook. `RecordingSession` stores the raw HID payload and its device's preparsed data, mouse events, device changes, relative timestamps, cursor position, monitor layout/orientation, OS version, and GestureSign input settings. `InputRecording` serializes this data as camelCase JSON. Saving uses `<name>.gsrec.json` and adds `-2`, `-3`, and so on rather than overwriting an existing take. The default output folder is `%USERPROFILE%\\Documents\\GestureSignRecordings`.

For headless recording, provide `--out <file> --seconds <n>`; optional arguments are `--description <text>` and `--no-mouse-move`. The recorder's [README](../../GestureSign.InputRecorder/README.md) documents the interactive workflow, command syntax, and adding recordings to the test corpus.

## Integration points

- `GestureSign.InputRecorder/GestureSign.InputRecorder.csproj` targets .NET 10 Windows and references `GestureSign.Common`, `GestureSign.Daemon`, and `ManagedWinapi`. The project uses the daemon's native input declarations and reads its input settings, but does not start or control the daemon.
- `GestureSign.Tests` references the recorder project. `GestureSign.Tests/Replay/InputReplayer.cs` loads `InputRecording` files and replays their events through the daemon's raw-input processor, point translator, and capture path; golden transcripts check the resulting stages and handled state.
- Copy real recordings to `GestureSign.Tests/Fixtures/Replay/recorded/<device-name>/`. Set `GESTURESIGN_UPDATE_GOLDEN=1` when running `test.ps1` to generate expected transcripts, then inspect the generated files before committing. See [Architecture](../overview/architecture.md) for the recorder-to-test flow and [Daemon](daemon/index.md) for the runtime input pipeline.

### Privacy

Recordings include device interface names (including vendor/product IDs and instance paths), monitor layout and names, OS version, GestureSign input settings, and cursor coordinates during capture. They contain **no keyboard input, window titles, or screen contents**. Treat recordings as device/environment diagnostics rather than anonymous gesture paths, and review them before sharing.

## Modification entry points

- For startup and command-line behavior, edit `GestureSign.InputRecorder/Program.cs`.
- For recording controls, auto-start/stop behavior, preview, and output naming, start with `GestureSign.InputRecorder/MainForm.cs`.
- For raw HID registration or mouse-hook transparency, inspect `GestureSign.InputRecorder/Recording/RawInputWindow.cs` and `GestureSign.InputRecorder/Recording/InputCapture.cs`.
- For event capture, environmental metadata, or settings snapshots, edit `GestureSign.InputRecorder/Recording/RecordingSession.cs`.
- For the JSON schema and serialization, edit `GestureSign.InputRecorder/Recording/InputRecording.cs`.
- For fixture replay or expected transcripts, start with `GestureSign.Tests/Replay/InputReplayer.cs` and `GestureSign.Tests/Replay/GoldenReplayTests.cs`.

## Key source files

| Path | Role |
| --- | --- |
| `GestureSign.InputRecorder/Program.cs` | Interactive and fixed-duration headless entry points. |
| `GestureSign.InputRecorder/MainForm.cs` | Recorder UI, take lifecycle, preview, and unique output-file naming. |
| `GestureSign.InputRecorder/Recording/InputCapture.cs` | Raw-input and low-level mouse-hook event forwarding. |
| `GestureSign.InputRecorder/Recording/RawInputWindow.cs` | Digitizer HID raw-input registration and message handling. |
| `GestureSign.InputRecorder/Recording/RecordingSession.cs` | Captures report/event data and read-only settings/environment metadata. |
| `GestureSign.InputRecorder/Recording/InputRecording.cs` | Versioned `*.gsrec.json` data model and serializer. |
| `GestureSign.InputRecorder/Recording/ContactDecoder.cs` | Decodes HID reports for live contact display. |
| `GestureSign.InputRecorder/Recording/DeviceCatalog.cs` | Enumerates and describes digitizer devices. |
| `GestureSign.InputRecorder/GestureSign.InputRecorder.csproj` | Framework, references, and executable project configuration. |
| `GestureSign.InputRecorder/README.md` | Usage, corpus workflow, and privacy contract. |
| `GestureSign.Tests/Replay/InputReplayer.cs` | Replays recordings through the daemon input pipeline. |
| `GestureSign.Tests/Replay/GoldenReplayTests.cs` | Compares replay transcripts with expected fixture outputs. |
