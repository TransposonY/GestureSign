using System;
using ManagedWinapi.Windows;
using System.Drawing;
using System.Collections.Generic;

namespace GestureSign.Common.Applications
{

    public interface IApplication
    {
        string Name { get; set; }

        bool AllowSingleStroke { get; set; }
        List<IAction> Actions { get; set; }
        MatchUsing MatchUsing { get; set; }
        string MatchString { get; set; }
        bool IsRegEx { get; set; }
        string Group { get; set; }

        void AddAction(IAction Action);
        void RemoveAction(IAction Action);
        void RemoveAllActions(Predicate<IAction> Match);
        bool IsSystemWindowMatch(SystemWindow Window);
    }
}
