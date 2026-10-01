# ManagedWinapi
Active contributors: TransposonY

`ManagedWinapi` is a .NET 10 Windows class library of managed wrappers over selected Win32 APIs. GestureSign uses it for HWND-backed window inspection and operations, global hotkeys, low-level input hooks, and keyboard helpers; it does not provide the `SendInput` simulator described by the separate WindowsInput library.

## Purpose and file layout

| Area | Contents |
| --- | --- |
| `ManagedWinapi/Windows/` | Window wrappers, native types, and message dispatch |
| `ManagedWinapi/Hooks/` | Hook lifecycle and low-level keyboard/mouse event wrappers |
| `ManagedWinapi/` | Hotkey, keyboard-key, API helper, and SendKeys utility types |

## Key types

| Type | Role |
| --- | --- |
| `SystemWindow` (`ManagedWinapi/Windows/SystemWindow.cs`) | Wraps an HWND and exposes window lookup/enumeration, metadata, state, geometry, messages, and operations |
| `Hook` (`ManagedWinapi/Hooks/Hook.cs`) | Base hook lifecycle and native hook setup |
| `LowLevelKeyboardHook`, `LowLevelMouseHook` (`ManagedWinapi/Hooks/LowLevelHook.cs`) | Expose low-level keyboard and mouse callbacks/messages; handlers can mark events handled |
| `LowLevelKeyboardMessage`, `LowLevelMouseMessage` (`ManagedWinapi/Hooks/LowLevelHook.cs`) | Represent intercepted input and provide replay operations |
| `Hotkey` (`ManagedWinapi/Hotkey.cs`) | Registers/unregisters a global hotkey and raises `HotkeyPressed` through a shared native message window |
| `KeyboardKey` (`ManagedWinapi/KeyboardKey.cs`) | Key-state/name utilities and legacy keyboard or mouse event injection |

## Integration points

- Common uses ManagedWinapi for window-aware application matching and runtime services; built-in actions use its window, key, and hotkey helpers.
- `SystemWindow` wraps Win32 window handles and calls user32/gdi32/psapi APIs via P/Invoke. Hook types convert native callback structures into managed events/messages.
- `Hotkey` registers through `RegisterHotKey` and routes `WM_HOTKEY` notifications via `EventDispatchingNativeWindow`.
- `WindowsInput` is a separate library that builds `INPUT` records and dispatches them through `SendInput`; see the [WindowsInput page](windows-input.md).
- `ManagedWinapi/ManagedWinapi.csproj` uses SDK-style automatic source inclusion and targets .NET 10 Windows.

## Modification starting point

For window lookup, window state, or HWND operations, begin with `ManagedWinapi/Windows/SystemWindow.cs`; for keyboard/mouse interception and event representation, use `ManagedWinapi/Hooks/LowLevelHook.cs` with its base `ManagedWinapi/Hooks/Hook.cs`. For global hotkey registration start at `ManagedWinapi/Hotkey.cs`. Keep declarations, managed wrappers, and the project compile list in sync when adding source files.

## Key source files

| Path | Responsibility |
| --- | --- |
| `ManagedWinapi/Windows/SystemWindow.cs` | HWND wrapper, enumeration, lookup, properties, and window operations |
| `ManagedWinapi/Hooks/Hook.cs` | Base hook lifecycle and hook-type definitions |
| `ManagedWinapi/Hooks/LowLevelHook.cs` | Low-level keyboard/mouse callbacks and message representations |
| `ManagedWinapi/Hotkey.cs` | Global hotkey registration and event dispatch |
| `ManagedWinapi/KeyboardKey.cs` | Key state/name helpers and event injection |
| `ManagedWinapi/Windows/EventDispatchingNativeWindow.cs` | Native window message dispatch support |
| `ManagedWinapi/ManagedWinapi.csproj` | Framework target, references, and explicit source list |

## Related pages

- [Common](common.md) and [Core plugins](core-plugins.md)
- [WindowsInput](windows-input.md)
- [Library overview](index.md)
