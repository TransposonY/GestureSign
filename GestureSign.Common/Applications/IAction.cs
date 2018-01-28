using GestureSign.Common.Input;
using ManagedWinapi;
using ManagedWinapi.Hooks;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace GestureSign.Common.Applications
{
    public interface IAction : INotifyCollectionChanged
    {
        string GestureName { get; set; }
        string Name { get; set; }
        string Condition { get; set; }
        bool? ActivateWindow { get; set; }
        IEnumerable<ICommand> Commands { get; set; }
        Hotkey Hotkey { get; set; }
        MouseActions MouseHotkey { get; set; }
        ContinuousGesture ContinuousGesture { get; set; }
        Devices IgnoredDevices { get; set; }

        void AddCommand(ICommand command);
        void InsertCommand(int index, ICommand command);
        void RemoveCommand(ICommand command);
        void MoveCommand(int oldIndex, int newIndex);
        bool IsEmpty();
    }
}
