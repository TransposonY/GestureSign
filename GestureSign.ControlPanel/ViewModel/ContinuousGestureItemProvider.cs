using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;

namespace GestureSign.ControlPanel.ViewModel
{
    /// <summary>One row of the continuous gesture tab.</summary>
    public class ContinuousGestureItem : INotifyPropertyChanged
    {
        private string _usedBy;

        public ContinuousGestureItem(ContinuousGesture gesture, string usedBy)
        {
            Gesture = gesture;
            // Snapshot of the sort/group keys: a changed entry is replaced rather than updated so the view re-sorts it.
            Name = gesture.Name;
            ContactCount = gesture.ContactCount;
            Direction = gesture.Direction;
            FingersText = ContinuousGestureItemProvider.GetFingersText(gesture.ContactCount);
            DirectionText = ContinuousGestureItemProvider.GetDirectionText(gesture.Direction);
            _usedBy = usedBy;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ContinuousGesture Gesture { get; }
        public string Name { get; }
        public int ContactCount { get; }
        public ContinuousDirection Direction { get; }
        public string FingersText { get; }
        public string DirectionText { get; }

        /// <summary>"App: Action, App: Action" for every action bound to this gesture.</summary>
        public string UsedBy
        {
            get { return _usedBy; }
            set
            {
                if (_usedBy == value) return;
                _usedBy = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsedBy)));
            }
        }

        public bool IsCurrent(ContinuousGesture gesture)
        {
            return ReferenceEquals(gesture, Gesture) && gesture.Name == Name && gesture.ContactCount == ContactCount && gesture.Direction == Direction;
        }
    }

    /// <summary>
    /// Rows of the continuous gesture tab, kept in sync with <see cref="ContinuousGestureManager"/> and the actions bound to each entry.
    /// </summary>
    public static class ContinuousGestureItemProvider
    {
        private static int _refreshPending;

        static ContinuousGestureItemProvider()
        {
            Items = new ObservableCollection<ContinuousGestureItem>();

            ContinuousGestureManager.Saved += (o, e) => ScheduleRefresh();
            ContinuousGestureManager.LoadCompleted += (o, e) => ScheduleRefresh();
            ApplicationManager.ApplicationSaved += (o, e) => ScheduleRefresh();
            ApplicationManager.OnLoadApplicationsCompleted += (o, e) => ScheduleRefresh();

            // Covers loads that finished before the events above were subscribed.
            Task.WhenAll(ContinuousGestureManager.Instance.LoadingTask, ApplicationManager.Instance.LoadingTask)
                .ContinueWith(task => ScheduleRefresh());
        }

        public static ObservableCollection<ContinuousGestureItem> Items { get; }

        /// <summary>Global actions first, then user applications by name. Ignored applications carry no actions to bind.</summary>
        public static List<IApplication> GetBindableApplications()
        {
            return ApplicationManager.Instance.Applications
                .Where(app => app is GlobalApp || app is UserApp)
                .OrderBy(app => app is GlobalApp ? 0 : 1)
                .ThenBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static string GetFingersText(int contactCount)
        {
            return string.Format(LocalizationProvider.Instance.GetTextValue("Action.Fingers"), contactCount).Trim();
        }

        public static string GetDirectionText(ContinuousDirection direction)
        {
            return LocalizationProvider.Instance.GetTextValue("Action." + direction);
        }

        public static string GetMotionText(int contactCount, ContinuousDirection direction)
        {
            return GetFingersText(contactCount) + " " + GetDirectionText(direction);
        }

        public static string GetActionName(IAction action)
        {
            return string.IsNullOrWhiteSpace(action.Name) ? LocalizationProvider.Instance.GetTextValue("Action.NewAction") : action.Name;
        }

        /// <summary>Rebuilds the rows. Must run on the UI thread.</summary>
        public static void Refresh()
        {
            var gestures = ContinuousGestureManager.Instance.ContinuousGestures.ToList();
            var applications = GetBindableApplications();

            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (!gestures.Any(g => Items[i].IsCurrent(g)))
                    Items.RemoveAt(i);
            }

            foreach (var gesture in gestures)
            {
                string usedBy = string.Join(", ", applications
                    .Where(app => app.Actions != null)
                    .SelectMany(app => app.Actions
                        .Where(action => action != null && action.ContinuousGestureName == gesture.Name)
                        .Select(action => app.Name + ": " + GetActionName(action))));

                var item = Items.FirstOrDefault(i => ReferenceEquals(i.Gesture, gesture));
                if (item == null)
                    Items.Add(new ContinuousGestureItem(gesture, usedBy));
                else
                    item.UsedBy = usedBy;
            }
        }

        private static void ScheduleRefresh()
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || Interlocked.Exchange(ref _refreshPending, 1) == 1)
                return;

            dispatcher.BeginInvoke(new System.Action(() =>
            {
                Interlocked.Exchange(ref _refreshPending, 0);
                Refresh();
            }));
        }
    }
}
