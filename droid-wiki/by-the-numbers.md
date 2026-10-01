# By the numbers

Data collected on 2026-09-29

These source metrics describe the **WORKTREE**, not a clean checkout of `origin/master`. The checkout is at `15392172d6bdfd5f20d5ef86060be40e430edc78` on `redesign-ribbon-ui`, with modified, staged, and untracked files. Counts include generated `.cs` and `.xaml` source files; they are not handwritten-only. They exclude `bin`, `obj`, `packages`, docs, the wiki, and `.git`.

## Source size

| Language | Files | Lines |
| --- | ---: | ---: |
| C# (`.cs`) | 306 | 39,522 |
| XAML (`.xaml`) | 40 | 5,772 |
| **Combined** | **346** | **45,294** |

```mermaid
xychart-beta
    title "WORKTREE source lines by language"
    x-axis "Lines" 0 --> 40000
    y-axis ["C#", "XAML"]
    bar [39522, 5772]
```

There are 10 top-level code directories. The `GestureSign.Tests` and `GestureSign.InputRecorder` trees are present in this worktree but are not part of the frozen `origin/master` history described below. `GestureSign.ExtraPlugins` contains two projects; counting that pair gives 11 C# project files across the code directories.

## Test and configuration footprint

The worktree contains 23 C# files / 4,210 lines under `GestureSign.Tests`, compared with 283 C# files / 35,312 lines outside that test project. This is roughly 8.1% by C# file count and 11.9% by C# lines; both counts include generated source and test helpers. The test fixtures also include 25 HID preparsed-data samples and 38 replay JSON files (19 recordings and 19 expected transcripts).

The solution has 11 C# project files, counting the two extra-plugin projects separately. Root build entry points are `build.ps1`, `test.ps1`, `GestureSign.sln`, and `Directory.Build.props`; packages are declared in project-level `packages.config` files.

| Worktree code directory | C# and XAML files | Lines |
| --- | ---: | ---: |
| `GestureSign.Common` | 69 | 4,853 |
| `GestureSign.ControlPanel` | 82 | 12,323 |
| `GestureSign.CorePlugins` | 67 | 7,635 |
| `GestureSign.Daemon` | 38 | 6,071 |
| `GestureSign.ExtraPlugins` | 11 | 812 |
| `GestureSign.InputRecorder` | 16 | 2,378 |
| `GestureSign.PointPatterns` | 6 | 378 |
| `GestureSign.Tests` | 23 | 4,210 |
| `ManagedWinapi` | 10 | 3,565 |
| `WindowsInput` | 24 | 3,069 |

```mermaid
xychart-beta
    title "WORKTREE source lines by code directory"
    x-axis "Lines" 0 --> 13000
    y-axis ["Common", "ControlPanel", "CorePlugins", "Daemon", "ExtraPlugins", "InputRecorder", "PointPatterns", "Tests", "ManagedWinapi", "WindowsInput"]
    bar [4853, 12323, 7635, 6071, 812, 2378, 378, 4210, 3565, 3069]
```

## Default-branch activity and hotspots

History metrics below use **only `origin/master`**, whose recorded history ends at the `v8.1` tag on 2022-02-02. The branch's age should not be confused with current worktree activity: there are no commits in the 90 days before this collection date because the available default-branch history stops in 2022.

The 1,135-commit history runs from 2015-02-26 through 2022-02-02:

```mermaid
xychart-beta
    title "origin/master commits by year"
    x-axis "Commits" 0 --> 400
    y-axis [2015, 2016, 2017, 2018, 2019, 2020, 2021, 2022]
    bar [372, 203, 302, 136, 15, 40, 55, 12]
```

The closing 12 calendar months of available history contain 45 commits. This is a history trend, not a claim about current development.

| Month | Commits |
| --- | ---: |
| 2021-03 | 0 |
| 2021-04 | 14 |
| 2021-05 | 6 |
| 2021-06 | 0 |
| 2021-07 | 6 |
| 2021-08 | 0 |
| 2021-09 | 2 |
| 2021-10 | 0 |
| 2021-11 | 5 |
| 2021-12 | 0 |
| 2022-01 | 9 |
| 2022-02 | 3 |

Among the last 100 commits on the default branch, frequently touched paths included `GestureSign.ControlPanel/MainWindowControls/Options.cs` (14), `GestureSign.Common/Configuration/AppConfig.cs` (13), control-panel language files (13 each), `GestureSign.Daemon/Input/PointCapture.cs` (12), `GestureSign.Daemon/Program.cs` (10), and `GestureSign.Daemon/Surface/SurfaceForm.cs` (9). These are file hotspots, not contributor rankings.

**Explicit bot co-author trailer share:** 0 of 1,135 recorded commits (0%). This only counts trailers; it does not detect other forms of bot attribution.

## A few useful scale measures

- The average `.cs`/`.xaml` file in this worktree is 130.9 lines. The mean is pulled upward by some unusually large files, so it is not a measure of typical handwritten file size.
- The longest measured source file is `ManagedWinapi/Windows/SystemWindow.cs` at 1,650 lines (WORKTREE, including any generated source).
- The remaining longest files include `GestureSign.Daemon/Native/HidDeclarations.cs` (1,152 lines), `WindowsInput/Native/VirtualKeyCode.cs` (939), `GestureSign.ControlPanel/MainWindowControls/AvailableActions.xaml` (853), and `GestureSign.ControlPanel/MainWindowControls/AvailableActions.cs` (819).
- The worktree's `GestureSign.Tests` tree documents HID corpus and replay coverage in [`TESTING.md`](../TESTING.md); neither that test project nor the recorder should be mistaken for contents of frozen default-branch history.

Project means and public type-declaration counts provide a rougher view of size. Counts use a source-text scan for public class, interface, enum, struct, and delegate declarations; they do not count methods or properties as separate API symbols.

| Project | Average lines per C#/XAML file | Public type declarations |
| --- | ---: | ---: |
| `GestureSign.Common` | 70.3 | 70 |
| `GestureSign.ControlPanel` | 150.3 | 56 |
| `GestureSign.CorePlugins` | 114.0 | 57 |
| `GestureSign.Daemon` | 159.8 | 59 |
| `GestureSign.ExtraPlugins` | 73.8 | 5 |
| `GestureSign.InputRecorder` | 148.6 | 14 |
| `GestureSign.PointPatterns` | 63.0 | 5 |
| `GestureSign.Tests` | 183.0 | 20 |
| `ManagedWinapi` | 356.5 | 30 |
| `WindowsInput` | 127.9 | 10 |

Among production projects, project-reference paths can span two edges, for example ControlPanel → Common → PointPatterns; ControlPanel also references PointPatterns directly. `GestureSign.Tests` is excluded from this depth comparison because it references several production projects directly for test access.

For dated release context, see [Lore](lore.md); for repository-specific curiosities, see [Fun facts](fun-facts.md). See also [Architecture](overview/architecture.md) and [Common library](libraries/common.md).
