# Dependencies

GestureSign 9 Alpha uses SDK-style .NET 10 Windows projects and NuGet `PackageReference`. All projects target `net10.0-windows10.0.19041.0` through `Directory.Build.props`; .NET Framework targeting packs, `packages.config`, local `winmd` and the old xUnit console runner are no longer build inputs.

## NuGet packages

| Package | Version | Direct users |
| --- | --- | --- |
| `MahApps.Metro` | `2.4.11` | ControlPanel UI |
| `Newtonsoft.Json` | `13.0.3` | Common, ControlPanel, InputRecorder, Tests |
| `System.Management` | `10.0.0` | ControlPanel, CorePlugins |
| `Microsoft.NET.Test.Sdk` | `17.13.0` | Tests |
| `xunit` | `2.9.3` | Tests |
| `xunit.runner.visualstudio` | `3.1.4` | Tests |

Common uses Newtonsoft.Json for configuration and saved data, and for typed local IPC payloads. Windows Desktop SDK reference assemblies supply WPF, Windows Forms, UI Automation and WinRT projections.

Opt-in feedback uses `HttpClient` to send Sentry store-protocol events directly. A send succeeds only after every report chunk receives an HTTP success response; rejected responses, network failures and the shared 30-second deadline return an error to the existing retry UI. No Sentry SDK/background queue is required.

## Solution projects and references

The solution includes `GestureSign.ControlPanel`, `GestureSign.Daemon`, `GestureSign.Common`, `GestureSign.CorePlugins`, `GestureSign.PointPatterns`, `GestureSign.InputRecorder`, `GestureSign.Tests`, `ManagedWinapi`, `WindowsInput`, and the `ClipboardMatch` and `TextCopyer` extra plugins (`GestureSign.sln`).

Direct project-reference relationships declared in the current project files include:

- Control Panel → Common, PointPatterns, ManagedWinapi (`GestureSign.ControlPanel/GestureSign.ControlPanel.csproj`).
- Daemon → Common, PointPatterns, ManagedWinapi, WindowsInput (`GestureSign.Daemon/GestureSign.Daemon.csproj`).
- Common → PointPatterns, ManagedWinapi (`GestureSign.Common/GestureSign.Common.csproj`).
- CorePlugins → Common, ManagedWinapi, WindowsInput (`GestureSign.CorePlugins/GestureSign.CorePlugins.csproj`).
- InputRecorder → Common, Daemon, ManagedWinapi (`GestureSign.InputRecorder/GestureSign.InputRecorder.csproj`).
- ClipboardMatch and TextCopyer → Common (`GestureSign.ExtraPlugins/ClipboardMatch/ClipboardMatch.csproj`, `GestureSign.ExtraPlugins/TextCopyer/TextCopyer.csproj`).
- Tests → Common, Daemon, InputRecorder, PointPatterns, ManagedWinapi (`GestureSign.Tests/GestureSign.Tests.csproj`).

The solution also declares a build dependency from the Control Panel to CorePlugins (`GestureSign.sln`).

## Framework targets and build setup

All eleven solution projects use the SDK-style `net10.0-windows10.0.19041.0` target. Windows 10 build 19041 or newer (or Windows 11) and the .NET 10 SDK are required for development; running a framework-dependent build requires the .NET 10 Windows Desktop runtime. `build.ps1` invokes `dotnet restore` and `dotnet build` for the solution, and `test.ps1` invokes `dotnet test` on the test project. `Directory.Build.props` declares the shared framework target and the `9.0.0-alpha.1` version. NuGet restores into its normal user package cache; no repo-local framework reference bootstrapping is necessary.

For configuration/data-file formats see [Configuration](configuration.md) and [Data models](data-models.md); for the contributor build instructions see [Build tooling](../how-to-contribute/tooling.md).
