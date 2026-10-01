using System.Collections.Generic;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;

namespace GestureSign.ControlPanel.ViewModel
{
    /// <summary>Rail selection of the Gesture tab (input and application) and the rules that apply it to a <see cref="GestureItem"/>.</summary>
    public sealed class GestureListFilter
    {
        /// <summary>Gestures using this many contacts or more match the last finger entry.</summary>
        public const int MaximumFingerChip = 5;

        /// <summary>0 for every gesture, otherwise the number of fingers (the last entry means "or more").</summary>
        public int Fingers { get; set; }

        /// <summary>Exact application name, or null for any application.</summary>
        public string Application { get; set; }

        /// <summary>Only gestures that no action uses.</summary>
        public bool UnusedOnly { get; set; }

        public bool IsActive
        {
            get { return Fingers != 0 || Application != null || UnusedOnly; }
        }

        public bool Matches(GestureItem item)
        {
            if (item == null)
                return false;
            return MatchesFingers(item.PatternCount, Fingers)
                && (Application == null || System.Array.IndexOf(item.ApplicationNames, Application) >= 0)
                && (!UnusedOnly || item.ActionCount == 0);
        }

        public static bool MatchesFingers(int contacts, int selected)
        {
            if (selected <= 0)
                return true;
            if (selected >= MaximumFingerChip)
                return contacts >= selected;
            return contacts == selected;
        }

        /// <summary>The words that describe a gesture: "2 fingers, with dot, curve, up-right".</summary>
        public static string Describe(GestureShapeInfo info)
        {
            var localization = LocalizationProvider.Instance;
            var parts = new List<string>(4);

            parts.Add(info.Contacts <= 1
                ? localization.GetTextValue("Gesture.Description.Mouse")
                : string.Format(localization.GetTextValue("Gesture.Description.Fingers"), info.Contacts));

            if (info.HasDot)
                parts.Add(localization.GetTextValue("Gesture.Description.WithDot"));

            switch (info.Shape)
            {
                case GestureShape.Line: parts.Add(localization.GetTextValue("Gesture.Description.Line")); break;
                case GestureShape.Curve: parts.Add(localization.GetTextValue("Gesture.Description.Curve")); break;
                case GestureShape.Loop: parts.Add(localization.GetTextValue("Gesture.Description.Loop")); break;
            }

            if (info.Direction != GestureDirection.None)
                parts.Add(localization.GetTextValue("Gesture.Description." + info.Direction));

            return string.Join(", ", parts);
        }
    }
}
