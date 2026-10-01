# Apps
Active contributors: TransposonY

GestureSign includes three Windows desktop tools: the background daemon that captures input and executes configured behavior, the WPF control panel for editing configuration, and an optional recorder for collecting reproducible input fixtures.

## Applications

| Application | Role |
| --- | --- |
| [Daemon](daemon/index.md) | Captures and translates input, recognizes gestures, handles triggers, and dispatches actions. |
| [Control panel](control-panel/index.md) | Edits application profiles, gesture assignments, commands, and settings; can start the daemon. |
| [Input recorder](input-recorder.md) | Optional WinForms utility that captures digitizer HID reports and mouse-hook events as JSON fixtures for input-pipeline replay tests. It does not block or alter input. |

The daemon and control panel are the primary runtime processes. They have separate single-instance mutexes and communicate through per-user named pipes. The recorder is an independent diagnostic/test-data tool; it can run without GestureSign's daemon. Shared application, gesture, configuration, plugin, input, and IPC contracts are in `GestureSign.Common`. See [Architecture](../overview/architecture.md) for project boundaries and the runtime flow.
