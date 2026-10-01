# Getting started

GestureSign 9 Alpha is a Windows desktop application targeting .NET 10. The PowerShell scripts restore and build the SDK-style solution with the `dotnet` CLI.

## Prerequisites

- Windows 10 build 19041 or newer, or Windows 11.
- .NET 10 SDK.
- Internet access for the first NuGet restore.

`build.ps1` does not require Visual Studio Build Tools, legacy .NET Framework reference packs or manually copied Windows metadata. See [tooling](../how-to-contribute/tooling.md) for configurations and output paths.

## Build

From the repository root, run:

```powershell
.\build.ps1
```

The default is Release. Choose another solution configuration with `-Configuration`:

```powershell
.\build.ps1 -Configuration Debug
.\build.ps1 -Configuration Portable
.\build.ps1 -Configuration Centennial
.\build.ps1 -Configuration uiAccessRelease
```

The script restores the solution and builds `GestureSign.sln`; output is written under `bin\<Configuration>\`. The `Portable`, `Centennial`, and `uiAccessRelease` configurations change packaging or privilege behavior. A UIAccess release requires signing and deployment conditions not supplied by a normal local build.

## Run tests

Run the full xUnit suite with:

```powershell
.\test.ps1
```

The runner builds the test project and its dependencies in Debug. To select a test class:

```powershell
.\test.ps1 --filter 'FullyQualifiedName~MouseCaptureTests'
```

Some golden replay tests support updating expected files, which modifies fixtures. Use update mode only when intentionally changing pipeline behavior, then inspect the generated diff:

```powershell
$env:GESTURESIGN_UPDATE_GOLDEN = "1"
.\test.ps1
```

Read [testing](../how-to-contribute/testing.md) before updating recordings or expected transcripts. `TESTING.md` documents the fixture format and paths not covered by automation.