# Lore

This timeline follows the available **default-branch history (`origin/master`)**, which is frozen at the `v8.1` release dated 2022-02-02. The current checkout is a separate worktree at `15392172d6bdfd5f20d5ef86060be40e430edc78`, with extensive local changes; those should not be read back into the historical record. Dates below refer to tags or recorded history, not necessarily the date a feature first existed outside the repository.

## 2015: The project takes shape

- **2015-02-26:** The first recorded commit contains foundational code in `GestureSign.Common`, `GestureSign.CorePlugins`, `GestureSign.PointPatterns`, and `ManagedWinapi`, alongside an application directory. These older areas still exist in the worktree.
- **2015-03-04:** The earliest tag in the supplied tag history is `v0.8.7`.
- **2015-08-18:** The names `GestureSign.Daemon` and `GestureSign.ControlPanel` appear in history, marking a recognizable arrangement of runtime and user-interface components.

## 2016: Major version milestones

- **2016-03-20:** The `v1.0` tag is recorded.
- **2016-03-31:** Commit `761e074` adds support for multiple gestures.
- **2016-07-31:** The `v3.0` tag is recorded.

## 2017–2018: Plugin expansion and 5.x–7.x

- **2017-03-23:** `GestureSign.ExtraPlugins` files begin appearing in the history. The current directory contains two plugin projects.
- **2017-06-17:** The `v5.0` tag is recorded.
- **2018-01-20:** The `v6.0` tag is recorded.
- **2018-06-06:** The `v7.0` tag is recorded.

## 2019–2022: Later releases

- **2019-05-04:** The `v7.2` tag is recorded.
- **2020-09-05:** The `v7.4` tag is recorded.
- **2021-07-25:** The `v8.0` tag is recorded.
- **2022-02-02:** The `v8.1` tag and latest recorded default-branch commit are dated. No later default-branch activity is represented in the available history as of **2026-09-29**.

The dates above are selected release and structural milestones, not every tag or every substantial change. Version numbers alone do not establish that a release was a rewrite.

## Features with long recorded lives

Several current paths span most of the available history:

| Path | Recorded history on `origin/master` | What the path concerns |
| --- | --- | --- |
| `GestureSign.Daemon/Input/PointCapture.cs` | 2015-02-26–2022-01-26; 105 commits touching it | Capturing input points |
| `GestureSign.Common/Applications/ApplicationManager.cs` | 2015-02-26–2021-11-14; 100 commits | Application/action management |
| `GestureSign.Common/Configuration/AppConfig.cs` | 2015-02-26–2022-01-31; 71 commits | Application configuration |
| `GestureSign.Common/Gestures/GestureManager.cs` | 2015-02-26–2021-05-09; 45 commits | Gesture matching/management |
| `GestureSign.Common/Plugins/PluginManager.cs` | 2015-02-26–2021-07-25; 38 commits | Plugin management |

“Long-lived” here means that the paths are traceable from the first commit through much of the repository's history. It does not imply that their implementations remained unchanged. See [Architecture](overview/architecture.md) for how the runtime parts fit together.

## What the available history does not establish

The selected history and tags show incremental evolution across long-lived projects, but do not by themselves document a wholesale rewrite or fully explain why individual changes happened. Although some compatibility code and obsolete types exist in the current worktree, this review does not establish the date or intent of a deprecation, so it does not assign one. Later worktree tests and recorder files belong to the current local state, not the frozen default-branch timeline.

For quantified worktree size and default-branch activity, see [By the numbers](by-the-numbers.md); for small details from the code and release history, see [Fun facts](fun-facts.md).
