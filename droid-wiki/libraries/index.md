# Libraries
Active contributors: TransposonY

This section documents the reusable assemblies that support GestureSign's runtime: shared contracts and managers, point-pattern recognition, built-in and optional actions, Windows API wrappers, and simulated input. It is an organizational map rather than a call-by-call reference. See the [architecture overview](../overview/architecture.md) for how the application components fit together.

## Projects

| Library or project | Responsibility | Documentation |
| --- | --- | --- |
| GestureSign.Common | Shared models, managers, configuration, plugin contracts, logging, localization, and IPC | [Common](common.md) |
| GestureSign.PointPatterns | Interpolation and geometric comparison of captured strokes | [Point patterns](point-patterns.md) |
| GestureSign.CorePlugins | Built-in gesture actions | [Core plugins](core-plugins.md) |
| GestureSign.ExtraPlugins | Separately compiled ClipboardMatch and TextCopyer actions | [Extra plugins](extra-plugins.md) |
| ManagedWinapi | Managed wrappers around selected Win32 window, hook, hotkey, and key APIs | [ManagedWinapi](managed-winapi.md) |
| WindowsInput | Keyboard and mouse event synthesis through Windows input APIs | [WindowsInput](windows-input.md) |

## Runtime relationships

Common defines the plugin contract and manages application services; it uses PointPatterns for stroke comparison and ManagedWinapi for Windows-specific support. CorePlugins provides the built-in actions and depends on Common, ManagedWinapi, and WindowsInput. ExtraPlugins contains two optional plugin assemblies implementing Common's plugin contract; their build steps copy the assemblies to the runtime `Plugins` directory for discovery.

These are SDK-style .NET 10 Windows projects. Their project files define dependencies, resources and output conventions; ordinary C# sources are included automatically. Follow the project pages for extension points.

## Related pages

- [Application architecture](../overview/architecture.md)
- [Common](common.md) and [Core plugins](core-plugins.md)
- [Extra plugins](extra-plugins.md), [Point patterns](point-patterns.md), [ManagedWinapi](managed-winapi.md), and [WindowsInput](windows-input.md)
