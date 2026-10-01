# Security

GestureSign is a Windows desktop application that captures local input and can perform configured actions in the user's session. Its trust boundaries include local inter-process communication, plugins and configured commands, configuration and import/export files, optional UIAccess behavior, and data the user may send as feedback.

## Local IPC

The daemon and control panel use named pipes whose names append the current Windows user's SID (`GestureSign.Common/InterProcessCommunication/NamedPipe.cs`). The receiving server also explicitly grants `BuiltinUsersSid` read/write access to the pipe (`GestureSign.Common/InterProcessCommunication/CustomNamedPipeServer.cs`). A SID-qualified name is therefore not, by itself, an access-control guarantee that only the logged-in user's processes can connect.

The wire format is a command byte followed, for commands that require a payload, by typed UTF-8 JSON: gesture points (`Point[][][]`) or a `Devices` value. Unknown commands and unexpected payloads are rejected (`GestureSign.Common/InterProcessCommunication/NamedPipe.cs`). The channel is local, not a network API; the daemon and Control Panel must be upgraded together because older `BinaryFormatter`-based processes cannot exchange messages with version 9. Treat IPC as a local trust boundary: a SID-qualified pipe name does not validate a payload (`GestureSign.Common/InterProcessCommunication/IpcCommands.cs`, `GestureSign.Daemon/MessageProcessor.cs`, `GestureSign.ControlPanel/MessageProcessor.cs`).

## Plugins and configured actions

At startup, `PluginManager` loads `GestureSign.CorePlugins.dll` beside the application and loads every `*.dll` found in its runtime `Plugins` directory using `Assembly.Load` (`GestureSign.Common/Plugins/PluginManager.cs`). A plugin is executable code in the application process. Install plugins only from sources you trust, and review plugin DLLs before placing them in that directory.

Gesture definitions can trigger commands such as launching processes or synthesizing keyboard and mouse input (`GestureSign.CorePlugins/RunCommand/RunCommandPlugin.cs`, `GestureSign.CorePlugins/LaunchApp/LaunchApp.cs`, `GestureSign.CorePlugins/SendKeystrokes/SendKeystrokes.cs`, `GestureSign.CorePlugins/MouseActions/MouseActionsPlugin.cs`). Configuration files and imported actions can therefore encode behavior, not just display preferences. Review enabled actions and their settings before using configurations from another person or source. `RunAsAdmin` is a setting used by startup behavior; it does not mean every action is elevated (`GestureSign.Common/Configuration/AppConfig.cs`, `GestureSign.ControlPanel/Common/StartupHelper.cs`).

## UIAccess build

The `uiAccessRelease` daemon manifest requests `uiAccess="true"` at `asInvoker` execution level (`GestureSign.Daemon/Properties/app.uiAccessRelease.manifest`, `GestureSign.Daemon/GestureSign.Daemon.csproj`). This Windows accessibility privilege supports the pointer-input filtering and touch behavior implemented by `GestureSign.Daemon/Filtration/PointerInputTargetWindow.cs`; it is distinct from a request to run as administrator. Windows grants UIAccess only when its signing and secure-install-location requirements are met. Use a properly signed binary installed in a trusted location; a locally built or unsigned copy should not be expected to receive UIAccess. See [testing](how-to-contribute/testing.md) for the hardware and installation constraints of exercising this path.

## Configuration, recordings, and archives

In a standard install, application profiles, gesture data, and configuration are stored under `%APPDATA%\GestureSign`; logs and backups are stored under `%LOCALAPPDATA%\GestureSign`. Portable mode keeps these data under the installation's `AppData` directory (`GestureSign.Common/Configuration/AppConfig.cs`, `GestureSign.Common/Configuration/FileManager.cs`). Keep these files private if their contents matter to you: profiles can include configured action and plugin settings.

The Input Recorder saves raw HID reports, device interface names and IDs, mouse-hook events, cursor coordinates, monitor layout, OS version, and input settings in JSON recordings (`GestureSign.InputRecorder/Recording/InputRecording.cs`, `GestureSign.InputRecorder/Recording/RecordingSession.cs`). Its documentation notes that recordings do not include keyboard input, window titles, or screen contents, but the recorded device and environment details may still identify hardware or a setup. Review a recording before sharing it. See the [Input Recorder README](../GestureSign.InputRecorder/README.md) for its complete schema and privacy notes.

Profile and backup archives are ZIP files. Archive imports extract `.ges` or `.gsb` contents to a temporary directory and load profile, gesture, continuous-gesture, and sometimes configuration files; full restore replaces saved state (`GestureSign.ControlPanel/Common/Archive.cs`, `GestureSign.ControlPanel/MainWindowControls/Options.cs`, `GestureSign.ControlPanel/Dialogs/ExportImportDialog.xaml.cs`). The download window fetches ZIP files from configured HTTPS sources, extracts them, and reads included settings (`GestureSign.ControlPanel/Dialogs/DownloadWindow.xaml.cs`). The reviewed paths do not verify archive signatures or otherwise establish archive authenticity. Import only files from sources you trust, and make a backup before restoring an archive that may replace current configuration.

For the file types and restore flow, see [Import and export](features/import-export.md) and [saved data](reference/data-models.md).

## Feedback

Sending feedback is an explicit user action from the control panel. The collected report can contain the Windows release/build, application version and installation path, PC manufacturer/model/version, the local GestureSign log, and matching .NET Runtime Application event-log entries (`GestureSign.ControlPanel/Log/Feedback.cs`, `GestureSign.ControlPanel/MainWindow.xaml.cs`). Review the report before sending it; logs or exception text may include locally meaningful details. See the [Control panel](apps/control-panel/index.md) page for the UI entry point.

## Further reading

- [Architecture](overview/architecture.md) describes process and IPC boundaries.
- [Apps](apps/index.md) describes the daemon and control panel.
- [Testing](how-to-contribute/testing.md) documents automated coverage and manual-only paths.
- [Dependencies](reference/dependencies.md) lists declared packages and local project dependencies.
- [Plugin actions](features/plugin-actions.md) and [data models](reference/data-models.md) describe configured behavior and serialized data.
