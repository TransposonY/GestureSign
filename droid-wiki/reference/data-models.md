# Data models

GestureSign's user-defined behavior is represented by applications containing actions, actions containing commands, and named gesture records. The Control Panel and daemon exchange additional control messages over a per-user named pipe (`GestureSign.Common/Applications/ApplicationBase.cs`, `GestureSign.Common/Applications/Action.cs`, `GestureSign.Common/Applications/Command.cs`, `GestureSign.Common/InterProcessCommunication/NamedPipe.cs`).

## Applications, actions, and commands

`ApplicationBase` stores a name, match type and match string, activation and regular-expression flags, grouping, optional icon bytes, and an action collection. Its matcher supports window class, title, executable filename, or all windows; text matching can use a case-insensitive regular expression or a trimmed case-insensitive literal (`GestureSign.Common/Applications/ApplicationBase.cs`).

An `Action` associates a `GestureName` and optional `Condition` with an ordered set of commands. It can also specify whether to activate the target window, a keyboard `Hotkey`, a mouse hotkey, ignored devices, and a `ContinuousGestureName` reference (`GestureSign.Common/Applications/Action.cs`).

Each `Command` records its display `Name`, enabled state, plugin class and file name, and plugin-specific `CommandSettings` (`GestureSign.Common/Applications/Command.cs`). The settings payload is interpreted by the selected plugin rather than by a universal settings schema. Application/action/command interfaces and concrete application types define additional application variants in `GestureSign.Common/Applications/`.

## Drawn and continuous gestures

A `Gesture` has a name and an array of `PointPattern` values. Each pattern contains an array of strokes, and each stroke is a sequence of `System.Drawing.Point` values (`GestureSign.Common/Gestures/Gesture.cs`, `GestureSign.Common/Gestures/PointPattern.cs`).

A `ContinuousGesture` catalog entry has a name, contact count from 2 through 10, and one direction (`Left`, `Right`, `Up`, or `Down`). An action references the named entry by `ContinuousGestureName`; the catalog is persisted separately and is loaded/saved by `ContinuousGestureManager` (`GestureSign.Common/Gestures/ContinuousGesture.cs`, `GestureSign.Common/Gestures/ContinuousGestureManager.cs`, `GestureSign.Common/Applications/Action.cs`).

Before the catalog format, continuous-gesture values could be embedded in an action. On load, `ContinuousGestureCatalog.MigrateLegacy` creates/reuses catalog entries and replaces the legacy value with a name reference. Invalid legacy motions are dropped; imported catalogs merge duplicate motions and update related action references (`GestureSign.Common/Gestures/ContinuousGestureManager.cs`, `GestureSign.Common/Applications/Action.cs`).

## Persisted files and JSON behavior

The standard data directory contains:

| File | Content |
| --- | --- |
| `Actions.gsa` | Application definitions and their actions/commands |
| `Gestures.gest` | Named drawn gestures and point patterns |
| `ContinuousGestures.gsc` | Named continuous-gesture catalog entries |

The filenames and extensions are declared in `GestureSign.Common/Constants.cs`; the data paths are described in [Configuration](configuration.md). JSON persistence is implemented by `FileManager` with Newtonsoft.Json. The action load path uses converters to construct concrete action and command implementations; legacy migration is handled after loading (`GestureSign.Common/Configuration/FileManager.cs`, `GestureSign.Common/Applications/Action.cs`, `GestureSign.Common/Gestures/ContinuousGestureManager.cs`).

## Inter-process messages

`IpcCommands` enumerates the daemon/Control Panel operations, including application, gesture, configuration, and continuous-gesture reload notifications. `NamedPipe` scopes a pipe name by appending the current user's SID. The version 9 wire format is a command byte followed, when required, by typed UTF-8 JSON (`Point[][][]` for `GotGesture`, `Devices` for `SynDeviceState`); it is distinct from the persisted JSON files. Both processes must be upgraded together; the previous `BinaryFormatter` pipe format is not compatible (`GestureSign.Common/InterProcessCommunication/IpcCommands.cs`, `GestureSign.Common/InterProcessCommunication/NamedPipe.cs`).

## Related pages

- [Configuration](configuration.md) — file locations and save/backup behavior.
- [Dependencies](dependencies.md) — serialization package and build targets.
- [Application-aware actions](../features/application-aware-actions.md)
- [Plugin actions](../features/plugin-actions.md)
