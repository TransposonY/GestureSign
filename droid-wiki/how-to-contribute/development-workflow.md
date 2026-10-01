# Development workflow

GestureSign is maintained as a Windows C# solution. The root `README.md` identifies `build.ps1` and `test.ps1` as the build and test entry points; `GestureSign.sln` contains the application, plugin, test, and input-recorder projects.

## Suggested local sequence

1. Choose a focused change and identify the affected solution projects and behavior. The repository does not prescribe branch naming or a branching model.
2. Build the solution with `.\build.ps1`. The default configuration is `Release`; use `Debug` when working with the test runner, which builds Debug by default. See [Tooling](tooling.md) for prerequisites and available configurations.
3. Run `.\test.ps1` for the xUnit input-pipeline suite. To narrow the run, pass `--filter 'FullyQualifiedName~MouseCaptureTests'`. See [Testing](testing.md) for coverage, replay-fixture updates, and manual checks.
4. Review the diff, including any generated or changed replay snapshots. Manually check changed UI, trigger, plugin, and hardware-dependent behavior where applicable; these areas are not covered by the automated suite.
5. Commit after reviewing the change. No commit-message format, mandatory review gate, or release process is defined by the repository documentation.

The test runner uses `dotnet test` to restore and build the test project and its dependencies; run `build.ps1` separately to build the whole solution, including the Control Panel and extra plugins.

## Repository automation and policy

No CI configuration was found at the repository root. No repository-level `CONTRIBUTING.md`, required pull-request template, branch naming rule, or approval policy is documented. Do not infer these requirements from the solution configurations in `GestureSign.sln`.

Related: [Testing](testing.md) · [Tooling](tooling.md) · [Debugging](debugging.md).
