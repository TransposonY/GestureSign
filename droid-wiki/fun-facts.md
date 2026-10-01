# Fun facts

## Four small surprises

- **2026-09-29 — The name is also the pitch.** The project README calls GestureSign “gesture recognition software for Windows tablet” and says gestures can be drawn with fingers or a mouse. Its feature list also includes launching programs, adjusting brightness and volume, and simulating keyboard and mouse actions (`README.md`).
- **2015-03-04 — An early release tag followed quickly.** The first recorded commit is dated 2015-02-26, and the earliest tag in the supplied history is `v0.8.7`, dated just six days later. See [Lore](lore.md) for the tag timeline.
- **2026-09-29 — The local test suite has hardware in its ancestry.** `TESTING.md` says the HID parser tests use 25 real Windows HID dumps, alongside synthetic descriptors and replay fixtures. It also describes an input recorder for collecting real device takes (`TESTING.md`, `GestureSign.Tests/Hid/PreparsedDataBuilderCorpusTests.cs`, `GestureSign.InputRecorder/README.md`). These tests and recorder are in the current worktree; they are not evidence that the same suite was on frozen `origin/master`.
- **2026-09-29 — One interop file is larger than several small projects.** `ManagedWinapi/Windows/SystemWindow.cs` is the longest measured `.cs`/`.xaml` source file in this worktree at 1,650 lines. Counts include generated source where present, so “longest” is a source-file measurement, not a handwritten-code claim. More metrics are in [By the numbers](by-the-numbers.md).

For more detail on releases and the files that span the history, see [Lore](lore.md).
