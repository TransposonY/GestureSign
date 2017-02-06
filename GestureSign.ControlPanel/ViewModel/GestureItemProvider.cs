using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GestureSign.Common.Gestures;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using ManagedWinapi.Hooks;

namespace GestureSign.ControlPanel.ViewModel
{
    public class GestureItemProvider
    {
        private static ObservableCollection<GestureItem> _gestureItems;

        static GestureItemProvider()
        {
            _gestureItems = new ObservableCollection<GestureItem>();

            GestureManager.GestureSaved += (o, e) => { Update(); };

            GestureManager.OnLoadGesturesCompleted += (o, e) => { Application.Current.Dispatcher.Invoke(Update); };

            if (GestureManager.FinishedLoading) Update();
        }

        public static ObservableCollection<GestureItem> GestureItems
        {
            get { return _gestureItems; }
            set { _gestureItems = value; }
        }

        private static void Update()
        {
            if (_gestureItems == null)
                _gestureItems = new ObservableCollection<GestureItem>();
            else
                _gestureItems.Clear();

            // Get all available gestures from gesture manager
            IEnumerable<IGesture> results = GestureManager.Instance.Gestures.OrderBy(g => g.PointPatterns?.Max(p => p.Points.Count));

            foreach (var g in results)
            {
                var gesture = (Gesture)g;
                GestureItem newItem = new GestureItem()
                {
                    MouseAction = gesture.MouseAction == MouseActions.None ? string.Empty : MouseActionDescription.DescriptionDict[gesture.MouseAction],
                    PointPattern = gesture.PointPatterns,
                    Name = gesture.Name,
                    HotKey = gesture.Hotkey != null ? new HotKey(KeyInterop.KeyFromVirtualKey(gesture.Hotkey.KeyCode), (ModifierKeys)gesture.Hotkey.ModifierKeys).ToString() : string.Empty
                };
                GestureItems.Add(newItem);
            }
        }
    }
}
