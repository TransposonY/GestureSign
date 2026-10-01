using GestureSign.Common.Applications;
using GestureSign.ControlPanel.Common;

namespace GestureSign.ControlPanel.ViewModel
{
    /// <summary>A row of the "By input" list: all gestures, or those drawn with a given number of fingers.</summary>
    public sealed class GestureInputOption
    {
        public string Label { get; set; }

        /// <summary>0 for all gestures; <see cref="GestureListFilter.MaximumFingerChip"/> means "that many or more".</summary>
        public int Fingers { get; set; }

        public int Count { get; set; }
    }

    /// <summary>A row of the "By application" list: an application that has gestures, or the gestures no action uses.</summary>
    public sealed class GestureApplicationOption
    {
        /// <summary>Matches <see cref="GestureItem.ApplicationNames"/>; null for the "Unused" row.</summary>
        public string Name { get; set; }

        public string DisplayName { get; set; }

        /// <summary>Source of the icon; null for the "Unused" row.</summary>
        public IApplication Application { get; set; }

        public bool IsUnused { get; set; }

        public int Count { get; set; }
    }

    /// <summary>One of the closest matches to the stroke drawn on the sketch pad.</summary>
    public sealed class GestureMatch
    {
        public GestureItem Item { get; set; }

        public string PercentText { get; set; }

        public string ActionsText { get; set; }

        /// <summary>The top match is highlighted.</summary>
        public bool IsBest { get; set; }
    }

    /// <summary>An action that runs when the selected gesture is drawn.</summary>
    public sealed class GestureUsage
    {
        public IApplication Application { get; set; }

        public string ApplicationName { get; set; }

        public string ActionName { get; set; }
    }
}
