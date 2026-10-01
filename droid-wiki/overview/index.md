# GestureSign

GestureSign lets Windows users run configured actions by drawing gestures with a finger, pen, or mouse. The repository contains a background daemon, a WPF control panel, shared .NET 10 Windows libraries, built-in and optional plugins, and a recorder and test harness for the raw input pipeline.

## Start here

- [Architecture](architecture.md) follows input from Windows devices through recognition and action dispatch.
- [Getting started](getting-started.md) covers prerequisites, build, and test commands.
- [Glossary](glossary.md) defines GestureSign-specific terms.
- [Applications](../apps/index.md) documents the daemon, editor, and input recorder.
- [Features](../features/index.md) groups behavior by what users can do.
- [Libraries](../libraries/index.md) maps shared projects and plugin boundaries.

The code targets Windows 10 build 19041 or newer and .NET 10. The solution includes WPF and WinForms applications and uses native Windows input APIs. See [build and test setup](../how-to-contribute/tooling.md) before changing the input pipeline.