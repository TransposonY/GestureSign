# GestureSign

GestureSign is a gesture recognition software for Windows tablet. You can automate repetitive tasks by simply drawing a gesture with your fingers or mouse.

## 9.0.0-alpha.1

GestureSign 9 Alpha runs on .NET 10 for Windows 10 (version 2004 / build 19041) and Windows 11. It retains the existing profiles, gestures, actions and plugins while moving all solution projects to SDK-style builds. The Control Panel, daemon, optional plugins, input recorder and test suite use the same runtime; .NET Framework 4.x and the old `packages.config` build are no longer required.

Existing `.gsa`, `.gest`, `.gsc` and `.config` files remain in their original locations. Upgrade both the daemon and Control Panel together: their local named-pipe protocol now uses typed JSON payloads instead of `BinaryFormatter`. Third-party plugins must be rebuilt for .NET 10; their persisted class and DLL names are unchanged.

[v9 Alpha branch](https://github.com/mancevd/GestureSign/tree/alpha)

## Feature

- Activate Window
- Window Control
- Touch Keyboard Control
- Keyboard simulation
- Key Down/Up
- Mouse Simulation
- Send Keystrokes
- Open Default Browser
- Screen Brightness
- Volume Adjustment
- Run Command or Program
- Launch Windows Store App
- Send Message
- Toggle Window Topmost

## Build and test

Install the .NET 10 SDK on Windows, then run `.\build.ps1` for the Release solution or `.\test.ps1` for the xUnit suite. Use `.\build.ps1 -Configuration Portable` for a portable build. See [TESTING.md](TESTING.md) for output paths, input-pipeline coverage, and recording real touchpad, touch-screen or pen input.
