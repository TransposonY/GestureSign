using System;
using System.Collections.Generic;
using ManagedWinapi;
using ManagedWinapi.Hooks;

namespace GestureSign.Common.Applications
{
    public interface IAction
    {
        string GestureName { get; set; }
        string Name { get; set; }
        string Condition { get; set; }
        bool? ActivateWindow { get; set; }
        List<ICommand> Commands { get; set; }
        Hotkey Hotkey { get; set; }
        MouseActions MouseAction { get; set; }
    }
}
