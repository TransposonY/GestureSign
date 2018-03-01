using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.ControlPanel.Common;

namespace GestureSign.ControlPanel.ViewModel
{
    public class GestureItemProvider : INotifyPropertyChanged
    {
        private static ObservableCollection<GestureItem> _gestureItems;
        private static event EventHandler<string> GlobalPropertyChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        static GestureItemProvider()
        {
            _gestureItems = new ObservableCollection<GestureItem>();

            GestureManager.GestureSaved += (o, e) => { Update(); };

            ApplicationManager.Instance.LoadingTask.ContinueWith((task) =>
            {
                ApplicationManager.ApplicationSaved += (o, e) =>
                {
                    Application.Current.Dispatcher.Invoke(Update);
                };
                GestureManager.Instance.LoadingTask.Wait();
                Application.Current.Dispatcher.Invoke(Update);
            });
        }

        public GestureItemProvider()
        {
            GestureMap = GestureManager.Instance.LoadingTask.IsCompleted ? GestureItems.ToDictionary(gi => gi.Gesture.Name, gi => gi.Gesture) : new Dictionary<string, IGesture>();
            GlobalPropertyChanged += (sender, propertyName) =>
            {
                GestureMap = GestureItems.ToDictionary(gi => gi.Gesture.Name, gi => gi.Gesture);
                OnPropertyChanged(propertyName);
            };
        }

        public static ObservableCollection<GestureItem> GestureItems
        {
            get { return _gestureItems; }
            set { _gestureItems = value; }
        }

        public Dictionary<string, IGesture> GestureMap { get; set; }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void Update()
        {
            if (_gestureItems == null)
                _gestureItems = new ObservableCollection<GestureItem>();
            else
                _gestureItems.Clear();

            // Get all available gestures from gesture manager
            IEnumerable<IGesture> results = GestureManager.Instance.Gestures.OrderBy(g => g.PointPatterns?.Max(p => p.Points.Count));
            var apps = ApplicationManager.Instance.Applications.Where(app => !(app is IgnoredApp)).ToList();

            foreach (var g in results)
            {
                var gesture = (Gesture)g;
                string result = string.Empty;
                foreach (IApplication application in apps)
                {
                    if (application.Actions.Any(a => a.GestureName == gesture.Name))
                        result += $" {application.Name},";
                }
                result = result.TrimEnd(',');

                GestureItem newItem = new GestureItem()
                {
                    Applications = result,
                    Gesture = gesture
                };
                GestureItems.Add(newItem);
            }
            GlobalPropertyChanged?.Invoke(typeof(GestureItemProvider), nameof(GestureMap));
        }
    }
}
