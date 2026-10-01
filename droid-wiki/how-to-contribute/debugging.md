# Debugging

Start by narrowing a failure to build/restore, test execution, input capture, gesture matching, or application behavior. The root `TESTING.md` explains the testable input pipeline and which behavior still needs a device or manual check.

## Build or restore failures

`build.ps1` requires the .NET 10 SDK on Windows and a successful NuGet package restore. If it fails before compiling source, check `dotnet --list-sdks`, the network/package feed, and the restore diagnostics. SDK-provided Windows Desktop reference packs and WinRT projections replace the old local `refpacks`/`winmd` inputs. See [Tooling](tooling.md).

## Test and replay failures

`test.ps1` runs `dotnet test` for `GestureSign.Tests` in `Debug` (or `Release` when selected). If discovery fails, check the SDK and the test project's restored packages and build; the old `xunit.console.exe` runner is not used.

The suite reads HID fixtures from `GestureSign.Tests/Fixtures/hidapi` and replay recordings/transcripts from `GestureSign.Tests/Fixtures/Replay`. A missing fixture is different from a replay behavior change: for a golden mismatch, the test reports the first differing line and writes a full `*.actual.json` beside the test binary. Compare that output with the corresponding `*.expected.json` before deciding whether to change implementation or intentionally regenerate snapshots. See [Testing](testing.md) for the update procedure.

## Hardware and UIAccess limitations

Synthetic tests do not establish that a specific touchpad, touch screen, or pen reports data in the same way as the corpus. Check that the device is delivering raw input and, when needed, capture a recording with `GestureSign.InputRecorder`; its procedure is in `GestureSign.InputRecorder/README.md`. Windows touchpad gestures or a running GestureSign instance can also react while a recording is being made, as explained in that README.

`PointerInputTargetWindow` handles UIAccess touch interception and injection, which the automated suite does not cover. Testing that path requires a signed build installed in a secure location and a touch screen. Overlay drawing, tray behavior, triggers, plugins, and Control Panel UI are likewise listed as manual checks in `TESTING.md`; a synthetic pipeline pass is not evidence that those UI paths work.

## Trace a gesture through the pipeline

For a gesture that is not recognized, distinguish raw input, translation/capture, gesture matching, and action dispatch rather than assuming one cause. The relevant starting points are:

- `GestureSign.Daemon/Input/RawInputProcessor.cs` and `GestureSign.Daemon/Input/PointEventTranslator.cs` for input decoding and event translation.
- `GestureSign.Daemon/Input/PointCapture.cs` for capture state.
- `GestureSign.Common/Gestures/GestureManager.cs` for gesture loading and matching.
- `GestureSign.Common/Applications/ApplicationManager.cs` for application/action definitions.

Related: [Testing](testing.md) · [Tooling](tooling.md).
