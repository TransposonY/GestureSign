# Glossary

GestureSign uses these terms across its shared models, daemon, and control panel.

| Term | Meaning |
| --- | --- |
| **Action** | A response assigned to a gesture or trigger for an application profile. An action can contain ordered plugin commands (`GestureSign.Common/Applications/Action.cs`). |
| **Application profile** | A set of actions and matching rules associated with a window class, title, executable, or global context (`GestureSign.Common/Applications/ApplicationBase.cs`). |
| **Capture** | A sequence of input points tracked from stroke start to completion by `GestureSign.Daemon/Input/PointCapture.cs`. |
| **Command** | A configured plugin invocation within an action, including serialized plugin settings (`GestureSign.Common/Applications/Command.cs`). |
| **Continuous gesture** | A named directional movement associated with a contact count and action binding. The current worktree stores these in a separate catalog (`GestureSign.Common/Gestures/ContinuousGesture.cs`). |
| **Device frame** | A group of decoded contacts or pen state from a raw input report, passed to the point-event translator. |
| **Gesture sample** | A named set of point patterns used as templates for recognition (`GestureSign.Common/Gestures/Gesture.cs`, `GestureSign.Common/Gestures/PointPattern.cs`). |
| **HID** | Human Interface Device, the Windows device/report format used here for digitizer input. |
| **Plugin** | An implementation of the shared action contract that presents settings and performs an operation (`GestureSign.Common/Plugins/IPlugin.cs`). |
| **Profile matching** | The process of choosing the application profile that applies to the target window (`GestureSign.Common/Applications/MatchUsing.cs`). |
| **Raw input** | Windows input delivered through `WM_INPUT`, including touchpad, touchscreen, and pen HID reports. |
| **Trigger** | A non-drawn input such as a hotkey, mouse button or wheel action, or continuous movement that can dispatch an action (`GestureSign.Daemon/Triggers/TriggerManager.cs`). |
| **UIAccess** | A Windows manifest privilege used by the optional pointer-input interception path. It requires a signed binary in a trusted installation location. |

For how these pieces connect, see [Architecture](architecture.md) and the [feature guide](../features/index.md).