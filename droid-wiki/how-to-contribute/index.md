# How to contribute

This section documents the repository's current contributor workflow: preparing a Windows build environment, building GestureSign, testing changes, and diagnosing common failures. The root `README.md`, `build.ps1`, `test.ps1`, and `TESTING.md` are the authoritative starting points for these procedures.

## Guides

- [Development workflow](development-workflow.md) — a suggested change/build/test/review sequence and the limits of the repository's automation.
- [Testing](testing.md) — the xUnit input-pipeline suite, replay fixtures, and behavior that still needs manual or hardware checks.
- [Debugging](debugging.md) — practical checks for build, fixture, hardware, and UIAccess-related problems.
- [Tooling](tooling.md) — prerequisites, build configurations, bootstrap behavior, and generated build inputs.
- [Patterns and conventions](patterns-and-conventions.md) — implementation patterns used in the codebase.

GestureSign is a Windows C# solution described in `README.md`. The repository provides local build and test entry points (`build.ps1` and `test.ps1`); the test suite and its known coverage boundaries are described in `TESTING.md`. No CI configuration was found at the repository root, so these pages do not assume CI checks or a required pull-request process.

Related: [Getting started](../overview/getting-started.md).
