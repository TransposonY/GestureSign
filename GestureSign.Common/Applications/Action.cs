using System.Collections.Generic;
using ManagedWinapi;
using ManagedWinapi.Hooks;

namespace GestureSign.Common.Applications
{
    public class Action : IAction
    {
        #region Public Properties

        public string Name { get; set; }

        public string GestureName { get; set; }

        public string Condition { get; set; }

        public bool? ActivateWindow { get; set; }

        public List<ICommand> Commands { get; set; }

        public Hotkey Hotkey { get; set; }

        public MouseActions MouseAction { get; set; }

        #endregion
    }
}
