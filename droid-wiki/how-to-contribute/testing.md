# Testing

`GestureSign.Tests` uses xUnit 2.9.3 on .NET 10 Windows. It covers the input pipeline from raw HID digitizer reports through gesture recognition and the local named-pipe protocol. `test.ps1` invokes `dotnet test` on the test project. The root `TESTING.md` describes coverage and manual-only boundaries.

## Run the tests

From the repository root in PowerShell:

```powershell
.\test.ps1
.\test.ps1 --filter 'FullyQualifiedName~MouseCaptureTests'
```

The optional `--filter` argument selects tests via the `dotnet test` filter syntax; `test.ps1` builds the test project and its dependencies. Build prerequisites are in [Tooling](tooling.md).

## Coverage

| Area | Representative tests | Test input |
|---|---|---|
| HID preparsed data | `Hid/PreparsedDataBuilderCorpusTests` | Windows HID dumps in `GestureSign.Tests/Fixtures/hidapi` |
| HID encode/decode | `Hid/HidDescriptorRoundTripTests` | Synthetic touchpad, touch screen, and pen descriptors; real `hid.dll` |
| Raw input decoding | `Input/RawInputProcessorTests` | Synthetic reports for screen mapping, hybrid frames, ROOT devices, pen/touch arbitration, and device selection |
| Translation and capture | `Input/MouseCaptureTests`, `Input/TouchAndPenCaptureTests` | Mouse hook messages and decoded touch/pen frames |
| Gesture matching | `Gestures/GestureMatchingTests` | `GestureSign.ControlPanel/Defaults/Gestures.gest` |
| End-to-end replay | `Replay/GoldenReplayTests` | `GestureSign.Tests/Fixtures/Replay/**/*.gsrec.json` recordings and expected transcripts |
| Recorder conversion and contact tracking | `Recording/RawInputConverterTests`, `Recording/ContactTrackerTests` | Fabricated RAWINPUT buffers and synthetic touchpad, touch screen, and pen reports |

## Golden replay fixtures

Replay tests feed `*.gsrec.json` recordings through the real input pipeline and compare the result with a sibling `*.expected.json` transcript. A failing golden test reports the first differing line and writes the complete `*.actual.json` beside the test binary. Inspect the actual and expected transcripts to determine which observable step changed before updating snapshots.

To intentionally regenerate expected transcripts, set the documented environment variable and run the test script:

```powershell
$env:GESTURESIGN_UPDATE_GOLDEN = "1"; .\test.ps1
```

Snapshot regeneration changes expected test data; review the diff before keeping it. Synthetic replay coverage is maintained under `GestureSign.Tests/Fixtures/Replay/synthetic/`. Real-device recordings belong under `GestureSign.Tests/Fixtures/Replay/recorded/<device-name>/`; the recording procedure and its data considerations are documented in `GestureSign.InputRecorder/README.md`.

## Coverage boundaries

The automated suite does not exercise all device and application behavior:

- Vendor-specific HID descriptors and real-world usage/contact ordering require recordings from actual hardware.
- `PointerInputTargetWindow` UIAccess touch interception and injection requires a signed build installed in a secure location and a touch screen.
- Overlay drawing (`SurfaceForm`), tray behavior, triggers (`HotKeyManager`, `MouseTrigger`, `ContinuousGestureTrigger`), plugins, and the Control Panel UI require manual checks.

For a missing or failing fixture, first verify the relevant fixture in `GestureSign.Tests/Fixtures/hidapi` or `GestureSign.Tests/Fixtures/Replay` is present. For a replay mismatch, compare the reported first difference and the generated `.actual.json` with the checked-in `.expected.json`. See [Debugging](debugging.md) for additional troubleshooting.
