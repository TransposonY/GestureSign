# Extra plugins
Active contributors: TransposonY

`GestureSign.ExtraPlugins` holds two optional gesture actions, each compiled as an independent .NET 10 Windows class library implementing Common's `IPlugin` contract.

## Purpose and file layout

| Area | Contents |
| --- | --- |
| `GestureSign.ExtraPlugins/ClipboardMatch/` | ClipboardMatch action, WPF settings editor, resources, and project |
| `GestureSign.ExtraPlugins/TextCopyer/` | TextCopyer action, WPF settings and crosshair UI, resources, and project |

Each project references `GestureSign.Common/GestureSign.Common.csproj`. The post-build event in each project creates `$(TargetDir)Plugins` when needed and copies its output assembly there; Common's `PluginManager` (`GestureSign.Common/Plugins/PluginManager.cs`) scans that directory for plugin assemblies.

## Key types

| Type or component | Role |
| --- | --- |
| `ClipboardMatch` (`GestureSign.ExtraPlugins/ClipboardMatch/ClipboardMatch.cs`) | Reads clipboard text, applies the configured regular expression, and puts a nonempty match back on the clipboard |
| `TextCopyer` (`GestureSign.ExtraPlugins/TextCopyer/TextCopyer.cs`) | Finds text through UI Automation at the first gesture point or configured point, extracts it, and copies it to the clipboard |
| `IPlugin` (`GestureSign.Common/Plugins/IPlugin.cs`) | Shared plugin contract for metadata, settings, UI, and gesture handling |
| `ClipboardMatchControl` (`GestureSign.ExtraPlugins/ClipboardMatch/ClipboardMatchControl.xaml`) | WPF editor for ClipboardMatch's expression |
| `TextCopyerPanel` (`GestureSign.ExtraPlugins/TextCopyer/TextCopyerPanel.xaml`) | WPF editor for TextCopyer's target position |

## Integration points

- Common's plugin manager loads assemblies from the runtime `Plugins` directory and discovers types implementing `IPlugin`.
- Both actions execute through the standard plugin gesture entry point and use Common's `PointInfo` context; they are not part of the built-in CorePlugins assembly.
- ClipboardMatch uses Windows Forms clipboard APIs and .NET regular expressions. TextCopyer uses WPF UI Automation APIs and writes extracted text using the clipboard.
- The projects target .NET 10 Windows with SDK-style source inclusion and copy their DLLs into the runtime `Plugins` directory after build.

## Modification starting point

Change action behavior or serialized settings in `GestureSign.ExtraPlugins/ClipboardMatch/ClipboardMatch.cs` for matching behavior, or `GestureSign.ExtraPlugins/TextCopyer/TextCopyer.cs` for UI Automation targeting and extraction. Change the configuration UI in its matching XAML and code-behind files. If changing how an assembly is built or deployed, update that plugin's `.csproj` and preserve the `Plugins` output convention.

## Key source files

| Path | Responsibility |
| --- | --- |
| `GestureSign.ExtraPlugins/ClipboardMatch/ClipboardMatch.cs` | Action execution, regex matching, and expression serialization |
| `GestureSign.ExtraPlugins/ClipboardMatch/ClipboardMatchControl.xaml` | ClipboardMatch configuration UI |
| `GestureSign.ExtraPlugins/ClipboardMatch/ClipboardMatch.csproj` | Assembly identity, Common reference, WPF page, framework target, and post-build deployment |
| `GestureSign.ExtraPlugins/TextCopyer/TextCopyer.cs` | UI Automation lookup, text extraction, clipboard copy, and position serialization |
| `GestureSign.ExtraPlugins/TextCopyer/TextCopyerPanel.xaml` | TextCopyer configuration UI |
| `GestureSign.ExtraPlugins/TextCopyer/Crosshair.xaml` | Target-position crosshair UI |
| `GestureSign.ExtraPlugins/TextCopyer/TextCopyer.csproj` | Assembly identity, Common reference, WPF pages, framework target, and post-build deployment |
| `GestureSign.Common/Plugins/PluginManager.cs` | Runtime assembly discovery and plugin registration |

## Related pages

- [Common plugin architecture](common.md)
- [Built-in actions](core-plugins.md)
- [Library overview](index.md)
