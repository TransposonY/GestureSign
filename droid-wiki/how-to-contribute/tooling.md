# Tooling

GestureSign is an SDK-style .NET 10 Windows desktop solution. `GestureSign.sln` contains the application, plugins, recorder and tests; `Directory.Build.props` declares the shared target framework and prerelease version. The root scripts use the `dotnet` CLI.

## Prerequisites

- Windows 10 build 19041 or newer, or Windows 11.
- .NET 10 SDK. For running prebuilt framework-dependent binaries, install the .NET 10 Windows Desktop runtime.
- Internet access for the first NuGet restore.

## Build

From the repository root, run:

```powershell
.\build.ps1
.\build.ps1 -Configuration Debug
```

The default configuration is `Release`. `build.ps1` restores and builds the full solution via `dotnet`, writing application/plugin output under `bin\<Configuration>`. `Directory.Build.props` sets version `9.0.0-alpha.1` and target `net10.0-windows10.0.19041.0`; packages restore through standard NuGet `PackageReference`. A local `nuget.exe`, .NET Framework reference assemblies and manually copied Windows Runtime metadata are no longer needed.

## Solution configurations

`GestureSign.sln` defines `Debug`, `Release`, `Portable`, `Centennial`, and `uiAccessRelease`. Select the configuration appropriate to the work being built; solution configuration names should not be treated as descriptions of packaging or deployment requirements. The test and input-recorder projects build only in `Debug` and `Release`.

`test.ps1` accepts `Debug` and `Release`, defaults to `Debug`, and uses `dotnet test`. Filter with `.\test.ps1 --filter 'FullyQualifiedName~MouseCaptureTests'`. See [Testing](testing.md) for coverage.

Related: [Development workflow](development-workflow.md) · [Debugging](debugging.md).
