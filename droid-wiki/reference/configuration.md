# Configuration

GestureSign keeps scalar preferences in a `.config` file and stores application actions and gestures as separate JSON files. `AppConfig` chooses their paths and provides setting defaults; `FileManager` handles JSON reads, writes, and backups (`GestureSign.Common/Configuration/AppConfig.cs`, `GestureSign.Common/Configuration/FileManager.cs`).

## Settings and defaults

`AppConfig` reads/writes app settings and falls back to these defaults when a value is missing or cannot be parsed. `UiAccess` is an in-memory flag set under the `uiAccess` build symbol on supported Windows versions; it is not an ordinary persisted preference (`GestureSign.Common/Configuration/AppConfig.cs`).

| Setting | Default |
| --- | --- |
| `VisualFeedbackColor` | `DeepSkyBlue` unless a Windows glass color can be read |
| `VisualFeedbackWidth` | `9` |
| `HideTwoFingerTrail` | `true` |
| `MinimumPointDistance` | `20` |
| `Opacity` | `0.35` |
| `IsOrderByLocation` | `true` |
| `ShowTrayIcon` | `true` |
| `CultureName` | Empty string |
| `SendErrorReport` | `true` |
| `LastErrorTime` | `DateTime.MinValue` |
| `InitialTimeout` | `0` |
| `DrawingButton` | `0` (`MouseActions.None`) |
| `RegisterTouchPad` | `true` |
| `TouchPadTapToClick` | `false` |
| `RegisterTouchScreen` | `true` |
| `IgnoreFullScreen` | `false` |
| `IgnoreTouchInputWhenUsingPen` | `true` |
| `PenGestureButton` | `0` |
| `RunAsAdmin` | `false` |

Settings are converted to strings in the mapped `.config` file. Writes are debounced through a timer; malformed scalar values are discarded in favor of defaults. `RunAsAdmin` is also consumed by the Control Panel's startup helper (`GestureSign.Common/Configuration/AppConfig.cs`, `GestureSign.ControlPanel/Common/StartupHelper.cs`).

## Files and paths

| Data | Standard installation | Portable configuration |
| --- | --- | --- |
| Scalar settings | `%APPDATA%\GestureSign\GestureSign.config` | `<exe folder>\AppData\GestureSign.config` |
| Actions | `%APPDATA%\GestureSign\Actions.gsa` | `<exe folder>\AppData\Actions.gsa` |
| Drawn gestures | `%APPDATA%\GestureSign\Gestures.gest` | `<exe folder>\AppData\Gestures.gest` |
| Continuous gestures | `%APPDATA%\GestureSign\ContinuousGestures.gsc` | `<exe folder>\AppData\ContinuousGestures.gsc` |
| Backups | `%LOCALAPPDATA%\GestureSign\Backup` | `<exe folder>\AppData\Backup` |

In non-portable builds, `AppConfig.ApplicationDataPath` is the roaming `%APPDATA%\GestureSign` directory, while `LocalApplicationDataPath` is `%LOCALAPPDATA%\GestureSign`; backups are under the latter. With the `Portable` build symbol, both data paths point to the executable's `AppData` folder and backups are in its `Backup` subfolder. The application log path is under `LocalApplicationDataPath` (`GestureSign.Common/Configuration/AppConfig.cs`, `GestureSign.Common/Constants.cs`).

## Serialization and backup behavior

`FileManager` serializes persisted objects as JSON using Newtonsoft.Json. It omits null and default-valued properties when saving. When replacing an existing file, it first copies the previous file into `AppConfig.BackupPath` using a timestamp and the original extension; after a successful save, that temporary backup is deleted. Loaders can recover from backups where implemented, including the continuous-gesture catalog (`GestureSign.Common/Configuration/FileManager.cs`, `GestureSign.Common/Gestures/ContinuousGestureManager.cs`).

For the fields and compatibility rules in these files, see [Data models](data-models.md). For project/package restore and compilation, see [Dependencies](dependencies.md) and [Build tooling](../how-to-contribute/tooling.md).
