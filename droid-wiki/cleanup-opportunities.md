# Cleanup opportunities

This page records maintainability and verification boundaries visible in the current checkout. The observations are pointers for future changes, not a refactoring mandate.

## Automated test boundaries

The `GestureSign.Tests` xUnit suite is focused on the input path: HID parsing, raw-input decoding, point translation and capture, gesture matching, replay fixtures, and recorder conversion/contact decoding. It does not automate UIAccess pointer interception, overlay drawing, tray behavior, triggers, plugins, or the Control Panel UI. Those paths currently need manual checks on the relevant Windows setup and hardware. See [Testing](how-to-contribute/testing.md) for the test commands and coverage details and [Architecture](overview/architecture.md) for the input pipeline.

The current checkout also contains newly added untracked test files. They are local additions and are not yet part of the repository-tracked test inventory; do not treat them as reproducible project coverage until they are tracked. For user-provided recordings, see the [Input Recorder README](../GestureSign.InputRecorder/README.md).

## Large source files

The largest `.cs` and `.xaml` files in this worktree, measured in lines, are:

| File | Lines | Area |
| --- | ---: | --- |
| `ManagedWinapi/Windows/SystemWindow.cs` | 1,650 | Window state and Win32 wrappers |
| `GestureSign.Daemon/Native/HidDeclarations.cs` | 1,152 | Native HID declarations |
| `WindowsInput/Native/VirtualKeyCode.cs` | 939 | Virtual-key enumeration |
| `GestureSign.ControlPanel/MainWindowControls/AvailableActions.xaml` | 853 | Action editor view |
| `GestureSign.ControlPanel/MainWindowControls/AvailableActions.cs` | 819 | Action editor behavior |
| `GestureSign.ControlPanel/Resources/Theme.xaml` | 769 | Shared control-panel styles |
| `GestureSign.Common/Applications/ApplicationManager.cs` | 697 | Profile matching and persistence |
| `GestureSign.Daemon/Input/PointCapture.cs` | 624 | Input capture lifecycle |
| `GestureSign.InputRecorder/MainForm.cs` | 623 | Recorder UI and take lifecycle |
| `ManagedWinapi/Hooks/LowLevelHook.cs` | 606 | Low-level mouse and keyboard hooks |
| `GestureSign.ControlPanel/MainWindowControls/Options.cs` | 585 | Settings UI behavior |
| `GestureSign.Tests/Hid/HidPreparsedDataBuilder.cs` | 576 | HID test-corpus builder |

Some of the `ManagedWinapi` and `WindowsInput` files are large because they contain API declarations or wrappers. `ApplicationManager`, `PointCapture`, and the action editor are higher-level behavior surfaces; when changing them, their existing tests, callers, and responsibilities are useful context rather than proof that a refactor is needed.

## Change concentration

There have been no commits in the 90 days before data collection, so this is not recent churn. Across the last 100 commits in the available history, the most frequently changed paths included `GestureSign.ControlPanel/MainWindowControls/Options.cs` (14 commits), `GestureSign.Common/Configuration/AppConfig.cs` (13), and `GestureSign.Daemon/Input/PointCapture.cs` (12). See [By the numbers](by-the-numbers.md) for the full list and collection date.

## Search coverage

A scan of tracked C# and XAML files found no `TODO`, `FIXME`, or `HACK` comments. This is a bounded search, not proof that generated, untracked, or other-language files contain no unfinished work.

For configuration and archive behavior, see [Import and export](features/import-export.md) and [Security](security.md). For package inventory, see [Dependencies](reference/dependencies.md).
