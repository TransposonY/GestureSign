# GestureSign Input Recorder

Records the raw input GestureSign's daemon consumes, so the input pipeline (HID digitizer parsing,
`PointEventTranslator`, `PointCapture`) can be regression-tested on machines without the hardware.
Each recording is a `*.gsrec.json` fixture (schema: `Recording/InputRecording.cs`) containing:

- every `WM_INPUT` HID report from digitizer collections (usage page 0x0D: pen 0x02, touch screen 0x04,
  touch pad 0x05) with the device's preparsed data, including ROOT/virtual digitizers;
- low-level mouse hook events (down/up/wheel, optionally every move);
- `WM_INPUT_DEVICE_CHANGE` notifications;
- the monitor layout, display orientation and the current GestureSign input settings (read-only).

The recorder never blocks or alters input, and it does not need GestureSign to be running.

## Run

Build `GestureSign.InputRecorder.csproj` (output in `bin\Debug\`), then on the machine with the
touchpad / touch screen / pen:

```
GestureSign.InputRecorder.exe
```

1. Check that your devices appear under *Detected digitizers* (kind, contact slots per report, surface size,
   live report rate). Touch the device: the left panel draws every finger / pen path live, one color per
   contact ID, with the number of contacts down right now. If a touchpad is listed but the panel stays
   empty, its reports are not reaching raw input.
2. Pick a scenario (or type your own *File name* and *Description* for a custom gesture), press
   **Record** (F5), perform the gesture. With *Start on touch, stop when released* (default) the take
   starts at the next touch-down or mouse button press outside the recorder and stops automatically once
   every contact, digitizer button and mouse button has been released for the configured time (800 ms),
   so the recording contains only the gesture. Idle reports do not extend this deadline; renewed input
   cancels it and the next release starts a fresh delay. Without auto-take, press **Stop** (F6) yourself.
   Esc/F6 cancels an armed take.
3. Review the take in the left panel (paths, maximum simultaneous contacts), then **Save** (Ctrl+S), or
   tick *Auto-save* to save every take on auto-stop.
   Maximum simultaneous contacts are measured at completed report-frame boundaries, not from the
   temporary union of old and new contact IDs while a hybrid frame is arriving.
4. Files are written to `<output folder>\<file name>.gsrec.json`
   (default `%USERPROFILE%\Documents\GestureSignRecordings`); further takes of the same name become
   `<file name>-2.gsrec.json`, `-3`, ... and never overwrite earlier ones.

Use the keyboard shortcuts for touchpad gestures, so the clicks that control the recorder do not end up in the take.
Uncheck *Record mouse moves* for touch/pen scenarios if the mouse is not involved; keep it for the mouse scenarios.

The yellow banner warns when something else reacts to the gestures: Windows' own 3/4-finger touchpad
gestures (Settings > Bluetooth & devices > Touchpad; set them to *Nothing* to record undisturbed) or a
running GestureSign. Their input is recorded either way.

Headless mode for scripting (records everything for a fixed time):

```
GestureSign.InputRecorder.exe --out <file> --seconds <n> [--description <text>] [--no-mouse-move]
```

## What to send back

Zip the output folder and send it. Scenarios your hardware cannot perform can be skipped.

## Adding recordings to the test corpus

Copy the files into `GestureSign.Tests/Fixtures/Replay/recorded/<device-name>/`, then run
`$env:GESTURESIGN_UPDATE_GOLDEN = "1"; .\test.ps1` once to create the sibling `*.expected.json`
transcripts. Review them (strokes, recognized gesture, `handled` flags) before committing; from then on
`.\test.ps1` fails whenever the pipeline's behavior for that recording changes.

## Privacy

Recordings contain device interface names (vendor/product IDs, instance paths), the monitor layout and
names, OS version, the GestureSign input settings and cursor coordinates during the recording.
They contain no keyboard input, window titles or screen contents.
