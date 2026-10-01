using System.Collections.Generic;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;

namespace GestureSign.ControlPanel.ViewModel
{
    /// <summary>Which shape chip is selected on the Gesture tab.</summary>
    public enum GestureShapeFilter
    {
        Any,
        Line,
        Curve,
        Loop
    }

    /// <summary>Filter bar state of the Gesture tab and the rules that apply it to a <see cref="GestureItem"/>.</summary>
    public sealed class GestureListFilter
    {
        /// <summary>Gestures using this many contacts or more match the last finger chip.</summary>
        public const int MaximumFingerChip = 5;

        /// <summary>0 for every gesture, otherwise the number of fingers (the last chip means "or more").</summary>
        public int Fingers { get; set; }
        public GestureShapeFilter Shape { get; set; }
        public bool DotOnly { get; set; }

        /// <summary>Exact application name, or null for any application.</summary>
        public string Application { get; set; }

        public string[] Tokens { get; set; } = ActionListFilter.EmptyTokens;

        public bool IsActive
        {
            get { return Fingers != 0 || Shape != GestureShapeFilter.Any || DotOnly || Application != null || Tokens.Length != 0; }
        }

        public bool Matches(GestureItem item)
        {
            if (item == null)
                return false;
            return MatchesFingers(item.PatternCount, Fingers)
                && MatchesShape(item.Shape, Shape)
                && (!DotOnly || item.HasDot)
                && (Application == null || System.Array.IndexOf(item.ApplicationNames, Application) >= 0)
                && ActionListFilter.MatchesKey(item.SearchKey, Tokens);
        }

        public static bool MatchesFingers(int contacts, int selected)
        {
            if (selected <= 0)
                return true;
            if (selected >= MaximumFingerChip)
                return contacts >= selected;
            return contacts == selected;
        }

        public static bool MatchesShape(GestureShape shape, GestureShapeFilter selected)
        {
            switch (selected)
            {
                case GestureShapeFilter.Line: return shape == GestureShape.Line;
                case GestureShapeFilter.Curve: return shape == GestureShape.Curve;
                case GestureShapeFilter.Loop: return shape == GestureShape.Loop;
                default: return true;
            }
        }

        /// <summary>The words a user can search for: "2 fingers, with dot, curve, up-right".</summary>
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
