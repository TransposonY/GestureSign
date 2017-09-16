using GestureSign.Common.Applications;
using GestureSign.Common.Localization;
using System.Collections.Generic;

namespace GestureSign.ControlPanel.ViewModel
{
    class ContinuousGestureDesc
    {
        public List<KeyValuePair<Gestures, string>> Gesture { get; set; }

        public ContinuousGestureDesc()
        {
            Gesture = new List<KeyValuePair<Gestures, string>>(5)
            {
                new KeyValuePair<Gestures, string>(Gestures.None, LocalizationProvider.Instance.GetTextValue("Action.SelectGesture")),
                new KeyValuePair<Gestures, string>(Gestures.Left, LocalizationProvider.Instance.GetTextValue("Action.Left")),
                new KeyValuePair<Gestures, string>(Gestures.Right, LocalizationProvider.Instance.GetTextValue("Action.Right")),
                new KeyValuePair<Gestures, string>(Gestures.Up, LocalizationProvider.Instance.GetTextValue("Action.Up")),
                new KeyValuePair<Gestures, string>(Gestures.Down, LocalizationProvider.Instance.GetTextValue("Action.Down"))
            };
        }
    }
}
