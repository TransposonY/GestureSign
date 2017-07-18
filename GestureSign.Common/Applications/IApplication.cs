using ManagedWinapi.Windows;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace GestureSign.Common.Applications
{

    public interface IApplication : INotifyCollectionChanged
    {
        string Name { get; set; }

        IEnumerable<IAction> Actions { get; set; }
        MatchUsing MatchUsing { get; set; }
        string MatchString { get; set; }
        bool IsRegEx { get; set; }
        string Group { get; set; }

        void AddAction(IAction Action);
        void Insert(int index, IAction action);
        void RemoveAction(IAction Action);
        bool IsSystemWindowMatch(SystemWindow Window);
    }
}
