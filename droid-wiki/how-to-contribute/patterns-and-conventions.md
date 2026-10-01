# Patterns and conventions

GestureSign is an SDK-style .NET 10 Windows solution with shared contracts in `GestureSign.Common` and Windows-specific UI and interop code. Follow the existing project boundaries and keep native input behavior testable outside the Windows message loop where practical.

## Project and code structure

- Put application and action data contracts in `GestureSign.Common/Applications/` and `GestureSign.Common/Gestures/`; avoid coupling those models to WPF controls.
- Keep operating-system input capture in `GestureSign.Daemon/Input/`. `RawInputProcessor` uses device/environment interfaces and `PointCapture` has a host abstraction, allowing the tests to provide deterministic fakes.
- Implement actions through `GestureSign.Common/Plugins/IPlugin.cs`. Core actions belong in `GestureSign.CorePlugins/`; optional assemblies are built under `GestureSign.ExtraPlugins/`.
- Projects use SDK-style `.csproj` files and NuGet `PackageReference`; ordinary `.cs` files are included automatically. Keep resources and output files explicit only when needed.

## State and messaging

Use the shared managers for saved applications, gestures, and continuous gestures. They own loading, mutation, and file persistence. The control panel notifies the daemon through `GestureSign.Common/InterProcessCommunication/IpcCommands.cs`; daemon-side IPC work is marshaled through its synchronization context (`GestureSign.Daemon/MessageProcessor.cs`).

Settings are exposed through `AppConfig` and persisted through .NET configuration. Do not bypass its save/change events when adding settings, because the control panel and daemon need to observe updates.

## Input code and tests

Keep raw report parsing separate from `MessageWindow` registration and OS queries. The test seams `IRawInputDeviceSource` and `IRawInputEnvironment` are declared in `GestureSign.Daemon/Input/RawInputProcessor.cs`; input settings are exposed by `IInputSettings` in `GestureSign.Daemon/Input/InputSettings.cs`, and the capture host contract is in `GestureSign.Daemon/Input/CaptureHost.cs`. Use the replay harness when behavior depends on timing or multiple reports, and keep expected transcripts stable unless the intended behavior changes.

Run the narrow test class while iterating, then `.\test.ps1` before handing off. Golden-file regeneration is an intentional fixture write, not a routine test step. See [Testing](testing.md).

## Windows boundaries

Win32 declarations and helper wrappers live in `ManagedWinapi/`, `WindowsInput/`, and `GestureSign.Daemon/Native/`. Treat pointer injection, hooks, device handles, and UIAccess as platform boundaries; they need Windows-specific validation beyond pure unit tests. See [Security](../security.md) and [Debugging](debugging.md).