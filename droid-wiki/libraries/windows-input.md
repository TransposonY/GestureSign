# WindowsInput
Active contributors: TransposonY

`WindowsInput` is a .NET 10 Windows class library for synthesizing keyboard and mouse events and querying input-device state. Its simulator methods build native `INPUT` records and pass them to the Windows `SendInput` API; it is separate from GestureSign's input capture and gesture-recognition pipeline.

## Purpose and file layout

| Area | Contents |
| --- | --- |
| `WindowsInput/Native/` | Native input structures, flags, virtual-key codes, and P/Invoke declarations |
| `WindowsInput/` | Simulator interfaces and implementations, input builder, dispatcher, and device-state adapter |

## Key types

| Type | Role |
| --- | --- |
| `IInputSimulator`, `InputSimulator` (`WindowsInput/IInputSimulator.cs`, `WindowsInput/InputSimulator.cs`) | Compose keyboard, mouse, and device-state simulator APIs |
| `IKeyboardSimulator`, `KeyboardSimulator` (`WindowsInput/IKeyboardSimulator.cs`, `WindowsInput/KeyboardSimulator.cs`) | Build and dispatch key, modified-key, and text-entry sequences |
| `IMouseSimulator`, `MouseSimulator` (`WindowsInput/IMouseSimulator.cs`, `WindowsInput/MouseSimulator.cs`) | Build and dispatch pointer, button, and wheel sequences |
| `InputBuilder` (`WindowsInput/InputBuilder.cs`) | Converts high-level simulator operations into ordered native `INPUT` records |
| `IInputMessageDispatcher`, `WindowsInputMessageDispatcher` (`WindowsInput/IInputMessageDispatcher.cs`, `WindowsInput/WindowsInputMessageDispatcher.cs`) | Dispatch input records through the native API |
| `IInputDeviceStateAdaptor`, `WindowsInputDeviceStateAdaptor` (`WindowsInput/IInputDeviceStateAdaptor.cs`, `WindowsInput/WindowsInputDeviceStateAdaptor.cs`) | Expose keyboard and mouse device state |

## How input is dispatched

The default `InputSimulator` composes `KeyboardSimulator`, `MouseSimulator`, and `WindowsInputDeviceStateAdaptor`. Keyboard and mouse operations create `InputBuilder` records and pass the resulting array to `WindowsInputMessageDispatcher`, which submits it through `SendInput` in `WindowsInput/Native/NativeMethods.cs`. The `InputSimulator` constructor also accepts supplied simulator/adaptor implementations for substitution.

Windows may block synthesized input across integrity boundaries, including UIPI restrictions against sending input to a higher-integrity target.

## Integration points

- Core action implementations use `InputSimulator` for synthesized keystrokes, text, mouse operations, and related input actions.
- `GestureSign.CorePlugins` references the WindowsInput assembly; `WindowsInput/WindowsInput.csproj` targets .NET 10 Windows and retains strong-name signing.
- The [ManagedWinapi page](managed-winapi.md) covers separate HWND, hook, hotkey, and key-helper wrappers.

## Modification starting point

Add or change high-level keyboard behavior in `WindowsInput/KeyboardSimulator.cs`, mouse behavior in `WindowsInput/MouseSimulator.cs`, and native event construction in `WindowsInput/InputBuilder.cs`. For P/Invoke or native record layout changes, check `WindowsInput/Native/NativeMethods.cs` and the related types under `WindowsInput/Native/`. Update the explicit compile list in `WindowsInput/WindowsInput.csproj` for new source files.

## Key source files

| Path | Responsibility |
| --- | --- |
| `WindowsInput/InputSimulator.cs` | Composes keyboard, mouse, and device-state adaptors |
| `WindowsInput/KeyboardSimulator.cs` | Keyboard sequence, modified keystroke, and text-entry API |
| `WindowsInput/MouseSimulator.cs` | Mouse movement, button, and wheel API |
| `WindowsInput/InputBuilder.cs` | Builds native keyboard and mouse input records |
| `WindowsInput/WindowsInputMessageDispatcher.cs` | Dispatches input batches to the native API |
| `WindowsInput/Native/NativeMethods.cs` | Declares `SendInput` and device-state APIs |
| `WindowsInput/Native/INPUT.cs` | Native input record definition |
| `WindowsInput/WindowsInput.csproj` | Framework target, explicit compile list, and signing configuration |
| `WindowsInput/WindowsInput.nuspec` | Package metadata included by the project |

## Related pages

- [Core plugins](core-plugins.md)
- [ManagedWinapi](managed-winapi.md)
- [Library overview](index.md)
