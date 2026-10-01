# Core plugins
Active contributors: TransposonY

`GestureSign.CorePlugins` is GestureSign's built-in action library. Its implementations use `IPlugin` (`GestureSign.Common/Plugins/IPlugin.cs`) to expose action metadata, optional settings UI, configuration serialization, and gesture execution. The library is a separate assembly from the extra-plugin project; it is built as `GestureSign.CorePlugins.dll` by `GestureSign.CorePlugins/GestureSign.CorePlugins.csproj`.

## Directory layout

| Path | Contents |
| --- | --- |
| `GestureSign.CorePlugins/ActivateWindow/`, `GestureSign.CorePlugins/Delay/`, `GestureSign.CorePlugins/HotKey/`, `GestureSign.CorePlugins/KeyDownKeyUp/` | Window activation, delays, shortcut input, and key-down/key-up actions; settings and WPF editors are colocated with their implementations. |
| `GestureSign.CorePlugins/LaunchApp/`, `GestureSign.CorePlugins/OpenFile/`, `GestureSign.CorePlugins/RunCommand/`, `GestureSign.CorePlugins/SendMessage/` | Application activation, opening a path, running a command, and sending a message or key sequence to a window. |
| `GestureSign.CorePlugins/MouseActions/`, `GestureSign.CorePlugins/SendKeystrokes/`, `GestureSign.CorePlugins/TouchKeyboard/` | Pointer operations, text/key-string input, and touch-keyboard visibility. |
| `GestureSign.CorePlugins/ScreenBrightness/`, `GestureSign.CorePlugins/Volume/` | Display brightness and system volume actions. |
| `GestureSign.CorePlugins/` | Additional window/application and GestureSign controls, plus shared code such as `GestureSign.CorePlugins/KeyboardHelper.cs`, `GestureSign.CorePlugins/IconSource.cs`, and `GestureSign.CorePlugins/Common/EnvironmentVariablesParser.cs`. |
| `GestureSign.CorePlugins/Languages/`, `GestureSign.CorePlugins/Properties/` | Localization resources and assembly resources/metadata. |

## Plugin contract and runtime

| Type | Role |
| --- | --- |
| `IPlugin` (`GestureSign.Common/Plugins/IPlugin.cs`) | Contract for metadata (`Name`, `Category`, `Description`, `IsAction`, `GUI`, `Icon`, and `ActivateWindowDefault`), lifecycle (`Initialize`), execution (`Gestured(PointInfo)`), settings (`Deserialize`/`Serialize`), and `HostControl`. |
| `PluginManager` (`GestureSign.Common/Plugins/PluginManager.cs`) | Loads `GestureSign.CorePlugins.dll`, discovers types implementing `IPlugin`, supplies host control, initializes instances, and matches saved commands to plugins by class name and assembly filename. |
| `PluginHelper` (`GestureSign.Common/Plugins/PluginHelper.cs`) | Shared JSON settings serialization/deserialization helper used by configurable implementations. |
| `EnvironmentVariablesParser` (`GestureSign.CorePlugins/Common/EnvironmentVariablesParser.cs`) | Expands variables for the command and file-opening actions using gesture/window context. |

At runtime, the Common `PluginManager` creates instances from the core assembly. For a recognized gesture it resolves enabled commands, applies the command's window-activation behavior, deserializes the command settings, and calls `Gestured` with `PointInfo` (including point and target-window context). A plugin's `GUI` is its optional configuration editor; it is not the execution entry point. The runtime flow and contract are implemented in `GestureSign.Common/Plugins/PluginManager.cs` and `GestureSign.Common/Plugins/IPlugin.cs`.

## Implementations

The following concrete `IPlugin` implementations and behaviors are present in the current source. The table intentionally describes package capabilities, not the full user-facing feature guides.

| Implementation | Verified behavior | Source |
| --- | --- | --- |
| `ActivateWindowPlugin` | Finds a window by title/class (literal or regular-expression matching), restores a minimized match, and brings it to the foreground; settings allow a timeout. | `GestureSign.CorePlugins/ActivateWindow/ActivateWindowPlugin.cs` |
| `Delay` | Pauses for a configured interval, or waits for foreground-window, menu, or mouse-capture events up to the configured timeout. | `GestureSign.CorePlugins/Delay/Delay.cs` |
| `HotKeyPlugin` | Sends configured modifier/key combinations; its configured Windows+L case locks the workstation. | `GestureSign.CorePlugins/HotKey/HotKeyPlugin.cs` |
| `KeyDownKeyUpPlugin` | Presses or releases configured keys. | `GestureSign.CorePlugins/KeyDownKeyUp/KeyDownKeyUpPlugin.cs` |
| `MouseActionsPlugin` | Clicks, double-clicks, presses/releases buttons, scrolls horizontally/vertically, or moves the pointer to/by a configured location. | `GestureSign.CorePlugins/MouseActions/MouseActionsPlugin.cs`, `GestureSign.CorePlugins/MouseActions/MouseActionEnum.cs` |
| `SendKeystrokes` | Sends a configured key string through `SendKeys` or the WindowsInput text-entry path. | `GestureSign.CorePlugins/SendKeystrokes/SendKeystrokes.cs` |
| `TouchKeyboard` | Shows, hides, or toggles the Windows touch keyboard; includes an on-screen-keyboard fallback path. | `GestureSign.CorePlugins/TouchKeyboard/TouchKeyboard.cs` |
| `LaunchApp` | Activates a selected Windows application using its AppUserModelID. | `GestureSign.CorePlugins/LaunchApp/LaunchApp.cs` |
| `OpenFilePlugin` | Opens a configured path with shell execution and optional arguments; expands configured variables. | `GestureSign.CorePlugins/OpenFile/OpenFilePlugin.cs` |
| `RunCommandPlugin` | Runs configured command text through `cmd.exe`, optionally showing its window; expands configured variables. | `GestureSign.CorePlugins/RunCommand/RunCommandPlugin.cs` |
| `DefaultBrowser` | Reads the registered HTTP default-browser association and starts the corresponding browser. | `GestureSign.CorePlugins/DefaultBrowser.cs` |
| `SendMessagePlugin` | Sends or posts a configured Win32 message, or a configured hotkey sequence, to the current or selected window. | `GestureSign.CorePlugins/SendMessage/SendMessagePlugin.cs` |
| `ScreenBrightnessPlugin` | Raises or lowers brightness through Windows monitor/WMI brightness APIs where supported. | `GestureSign.CorePlugins/ScreenBrightness/ScreenBrightnessPlugin.cs` |
| `VolumePlugin` | Raises/lowers system volume by a configured amount or toggles mute. | `GestureSign.CorePlugins/Volume/VolumePlugin.cs` |
| `Minimize` | Minimizes the target window, with exclusions for specified immersive and tool windows. | `GestureSign.CorePlugins/Minimize.cs` |
| `MaximizeRestore` | Toggles the target window between maximized and normal states. | `GestureSign.CorePlugins/MaximizeRestore.cs` |
| `ToggleWindowTopmost` | Toggles the target window's topmost state. | `GestureSign.CorePlugins/ToggleWindowTopmost.cs` |
| `NextApplication`, `PreviousApplication` | Switches applications using Alt+Tab or Shift+Alt+Tab. | `GestureSign.CorePlugins/NextApplication.cs`, `GestureSign.CorePlugins/PreviousApplication.cs` |
| `TemporarilyDisable` | Temporarily disables point capture and toggles gesture disable through the host control. | `GestureSign.CorePlugins/TemporarilyDisable.cs` |
| `ToggleDisableGestures` | Toggles gesture disable through the tray manager. | `GestureSign.CorePlugins/ToggleDisableGestures.cs` |
| `Notifier` | A non-action plugin whose current `Initialize` notification hook is commented out; it has no active notification behavior in this source. | `GestureSign.CorePlugins/Notifier.cs` |

## Extending the package

1. Add a parameterless class implementing `IPlugin` (`GestureSign.Common/Plugins/IPlugin.cs`). Implement the lifecycle and execution methods; use `HostControl` only when the action needs host services.
2. Add its source file to the explicit `<Compile>` items in `GestureSign.CorePlugins/GestureSign.CorePlugins.csproj`. The project also lists WPF editor files as `<Page>` items.
3. For configurable actions, provide settings through `Serialize`/`Deserialize` (the existing settings classes commonly use `PluginHelper`) and, if needed, a WPF control through `GUI`. Add localization entries in `GestureSign.CorePlugins/Languages/en.xml` and `GestureSign.CorePlugins/Languages/zh.xml` when the action's metadata or UI uses localized strings.
4. Build the core project and ensure the resulting `GestureSign.CorePlugins.dll` is alongside the runtime. The Common manager discovers implementations by scanning the assembly, so there is no separate core-plugin registration list.

The core project references `GestureSign.Common`, `ManagedWinapi`, and `WindowsInput`; the implementation uses those packages for plugin contracts/settings, Windows window helpers, and synthesized keyboard/mouse input respectively. Extra actions are built separately and loaded from the runtime's `Plugins` directory; see [Extra plugins](extra-plugins.md).

## Related pages

- [Common library](common.md) — plugin contract, command settings, and loader.
- [Extra plugins](extra-plugins.md) — separately built extensions.
- [Plugin actions](../features/plugin-actions.md) — how action commands are configured.
- [Application-aware actions](../features/application-aware-actions.md) — command targeting and window context.
- [WindowsInput library](windows-input.md) and [ManagedWinapi library](managed-winapi.md) — input simulation and Windows helpers used by implementations.

## Key source files

| Full repository path | Responsibility |
| --- | --- |
| `GestureSign.CorePlugins/GestureSign.CorePlugins.csproj` | Assembly definition, explicit C# and WPF compile lists, project references, and output paths. |
| `GestureSign.Common/Plugins/IPlugin.cs` | Required metadata, lifecycle, serialization, and gesture-execution members. |
| `GestureSign.Common/Plugins/PluginManager.cs` | Assembly discovery and runtime command-to-plugin dispatch. |
| `GestureSign.Common/Plugins/PluginHelper.cs` | JSON settings serialization helper used across configurable plugins. |
| `GestureSign.CorePlugins/Common/EnvironmentVariablesParser.cs` | Shared variable expansion for command/file actions. |
| `GestureSign.CorePlugins/MouseActions/MouseActionsPlugin.cs` | Pointer action execution and configured reference-point handling. |
| `GestureSign.CorePlugins/HotKey/HotKeyPlugin.cs` | Configurable shortcuts and input execution paths. |
| `GestureSign.CorePlugins/SendMessage/SendMessagePlugin.cs` | Target-window selection and Windows message/key delivery. |
| `GestureSign.CorePlugins/ScreenBrightness/ScreenBrightnessPlugin.cs` | Windows brightness discovery and adjustment. |
| `GestureSign.CorePlugins/Volume/VolumePlugin.cs` | Volume and mute implementation. |
