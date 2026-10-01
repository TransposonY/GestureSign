using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace GestureSign.ControlPanel.Converters
{
    /// <summary>
    /// Splits an action into the text pieces shown in the Name and Gesture columns.
    /// values[0]: IAction, values[1]: gesture map (only there so the cell refreshes when gestures change).
    /// parameter: Name | Placeholder | Primary | Secondary.
    /// </summary>
    public class ActionSummaryConverter : IMultiValueConverter
    {
        private struct Line
        {
            public string Label;
            public string Text;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var action = values.Length > 0 ? values[0] as IAction : null;
            if (action == null)
                return null;

            var gestureMap = values.Length > 1 ? values[1] as Dictionary<string, GestureItem> : null;

            switch (parameter as string)
            {
                case "Name":
                    return GetName(action);
                case "Placeholder":
                    return !HasGestureImage(action, gestureMap) && GetLines(action).Count == 0
                        ? LocalizationProvider.Instance.GetTextValue("Action.ClickToSelectGesture")
                        : null;
                case "Primary":
                    {
                        if (HasGestureImage(action, gestureMap))
                            return null;
                        var lines = GetLines(action);
                        return lines.Count == 0 ? null : lines[0].Text;
                    }
                case "Secondary":
                    {
                        var lines = GetLines(action);
                        if (lines.Count == 0)
                            return null;

                        if (HasGestureImage(action, gestureMap))
                            return string.Join("  ·  ", lines.Select(l => l.Label + ": " + l.Text));

                        // The first line already reads as the headline; caption it, then list the rest.
                        return string.Join("  ·  ", new[] { lines[0].Label }
                            .Concat(lines.Skip(1).Select(l => l.Label + ": " + l.Text)));
                    }
                default:
                    return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static string GetName(IAction action)
        {
            var name = string.IsNullOrWhiteSpace(action.Name)
                ? LocalizationProvider.Instance.GetTextValue("Action.NewAction")
                : action.Name;

            return string.IsNullOrWhiteSpace(action.Condition) ? name : name + " [Cond]";
        }

        private static bool HasGestureImage(IAction action, Dictionary<string, GestureItem> gestureMap)
        {
            GestureItem item;
            return gestureMap != null
                && action.GestureName != null
                && gestureMap.TryGetValue(action.GestureName, out item)
                && item?.GestureImage != null;
        }

        /// <summary>"name (N Fingers Swipe Dir)", or just the name when it is not in the catalog.</summary>
        public static string GetContinuousGestureText(string continuousGestureName)
        {
            var gesture = ContinuousGestureManager.Instance.Find(continuousGestureName);
            if (gesture == null)
                return continuousGestureName;

            var localization = LocalizationProvider.Instance;
            return gesture.Name + " (" + string.Format(localization.GetTextValue("Action.Fingers"), gesture.ContactCount).Trim()
                + " " + localization.GetTextValue("Action." + gesture.Direction) + ")";
        }

        private static List<Line> GetLines(IAction action)
        {
            var localization = LocalizationProvider.Instance;
            var lines = new List<Line>();

            if (!string.IsNullOrEmpty(action.ContinuousGestureName))
            {
                lines.Add(new Line
                {
                    Label = localization.GetTextValue("ActionDialog.Continuous"),
                    Text = GetContinuousGestureText(action.ContinuousGestureName)
                });
            }
            if (action.Hotkey != null)
            {
                lines.Add(new Line
                {
                    Label = localization.GetTextValue("ActionDialog.KeyboardHotKey"),
                    Text = new HotKey(KeyInterop.KeyFromVirtualKey(action.Hotkey.KeyCode), (ModifierKeys)action.Hotkey.ModifierKeys).ToString()
                });
            }
            if (action.MouseHotkey != ManagedWinapi.Hooks.MouseActions.None && AppConfig.DrawingButton != ManagedWinapi.Hooks.MouseActions.None)
            {
                lines.Add(new Line
                {
                    Label = localization.GetTextValue("ActionDialog.MouseHotKey"),
                    Text = ViewModel.MouseActionDescription.DescriptionDict[AppConfig.DrawingButton] + " + " + ViewModel.MouseActionDescription.DescriptionDict[action.MouseHotkey]
                });
            }

            return lines;
        }
    }
}
