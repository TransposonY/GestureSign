using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Extensions;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Dialogs;
using GestureSign.ControlPanel.ViewModel;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

namespace GestureSign.ControlPanel.MainWindowControls
{
    /// <summary>
    /// AvailableGestures.xaml 的交互逻辑
    /// </summary>
    public partial class AvailableGestures : UserControl, INotifyPropertyChanged
    {
        private enum SortMode
        {
            /// <summary>Closest to the drawn stroke first; only while something is drawn.</summary>
            Best,
            MostUsed,
            Default,
            /// <summary>A column header of the details view was clicked.</summary>
            Column
        }

        /// <summary>Below this window width the draw band has room for two matches instead of three.</summary>
        private const double NarrowWidth = 1000;
        private int _matchCount = 3;

        private readonly GestureListFilter _filter = new GestureListFilter();
        private readonly ToggleButton[] _padToggles;
        private List<GestureApplicationOption> _applicationOptions = new List<GestureApplicationOption>();
        private System.Drawing.Point[] _drawn;
        private SortMode _sortMode = SortMode.MostUsed;
        private string _sortColumn;
        private ListSortDirection _sortDirection;
        private bool _ready;
        private bool _updatingRail;
        private string _statusText;

        public event PropertyChangedEventHandler PropertyChanged;

        public AvailableGestures()
        {
            InitializeComponent();

            _padToggles = new[] { PadOneFinger, PadTwoFingers, PadThreeFingers, PadFourFingers };

            MatchCards.ItemsPanel = (ItemsPanelTemplate)Resources["MatchPanelWide"];
            SizeChanged += (o, e) => UpdateMatchLayout();

            _ready = true;
            GestureItemProvider.GestureMapChanged += (o, e) => OnGesturesChanged();
            OnGesturesChanged();
        }

        private void UpdateMatchLayout()
        {
            bool narrow = ActualWidth < NarrowWidth;
            int count = narrow ? 2 : 3;
            if (count == _matchCount)
                return;
            _matchCount = count;
            MatchCards.ItemsPanel = (ItemsPanelTemplate)Resources[narrow ? "MatchPanelNarrow" : "MatchPanelWide"];
            UpdateMatches();
        }

        /// <summary>Selection summary for the window's status bar.</summary>
        public string StatusText
        {
            get { return _statusText; }
            private set
            {
                if (_statusText == value)
                    return;
                _statusText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText)));
            }
        }

        private CollectionViewSource GestureViewSource
        {
            get { return (CollectionViewSource)Resources["GestureViewSource"]; }
        }

        private static string Text(string key)
        {
            return LocalizationProvider.Instance.GetTextValue(key);
        }

        private static string Text(string key, params object[] args)
        {
            return string.Format(LocalizationProvider.Instance.GetTextValue(key), args);
        }

        private static string ApplicationDisplayName(IApplication application)
        {
            return application is GlobalApp ? Text("Gesture.Browse.Global") : application.Name;
        }

        private void GestureViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = _filter.Matches(e.Item as GestureItem);
        }

        #region Rail

        private void InputList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _updatingRail)
                return;
            var option = InputList.SelectedItem as GestureInputOption;
            if (option == null)
            {
                SelectInputOption(0);
                return;
            }
            _filter.Fingers = option.Fingers;
            SyncPadToggles();
            RefreshFilter();
        }

        private void ApplicationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _updatingRail)
                return;
            var option = ApplicationList.SelectedItem as GestureApplicationOption;
            _filter.Application = option != null && !option.IsUnused ? option.Name : null;
            _filter.UnusedOnly = option != null && option.IsUnused;
            RefreshFilter();
        }

        /// <summary>Clicking the selected application again shows every application.</summary>
        private void ApplicationList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var container = ItemsControl.ContainerFromElement(ApplicationList, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (container != null && container.IsSelected)
            {
                ApplicationList.SelectedItem = null;
                e.Handled = true;
            }
        }

        private void ApplicationSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_ready)
                return;
            ShowApplicationOptions();
        }

        private void SelectInputOption(int fingers)
        {
            var option = InputList.Items.OfType<GestureInputOption>().FirstOrDefault(o => o.Fingers == fingers)
                         ?? InputList.Items.OfType<GestureInputOption>().FirstOrDefault();
            InputList.SelectedItem = option;
        }

        /// <summary>The pad's input switch mirrors the rail: a finger count the rail has selected is on, otherwise none is.</summary>
        private void SyncPadToggles()
        {
            foreach (var toggle in _padToggles)
                toggle.IsChecked = int.Parse((string)toggle.Tag) == _filter.Fingers;
            SketchPad.ShowAnchorDot = _filter.Fingers >= 2;
        }

        private void PadFingers_Click(object sender, RoutedEventArgs e)
        {
            var toggle = (ToggleButton)sender;
            int fingers = toggle.IsChecked == true ? int.Parse((string)toggle.Tag) : 0;
            // The rail owns the filter; selecting there updates the toggles and the list.
            SelectInputOption(fingers);
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            ApplicationList.SelectedItem = null;
            SelectInputOption(0);
        }

        /// <summary>Gestures, applications or actions changed: recount the rail, then re-apply the selection and the filter.</summary>
        private void OnGesturesChanged()
        {
            var items = GestureItemProvider.GestureItems;

            _updatingRail = true;
            try
            {
                var counts = new int[GestureListFilter.MaximumFingerChip + 1];
                var usage = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
                int unused = 0;
                foreach (var item in items)
                {
                    counts[Math.Max(0, Math.Min(item.PatternCount, GestureListFilter.MaximumFingerChip))]++;
                    foreach (var name in item.ApplicationNames)
                        usage[name] = usage.TryGetValue(name, out int count) ? count + 1 : 1;
                    if (item.ActionCount == 0)
                        unused++;
                }

                var inputs = new List<GestureInputOption> { new GestureInputOption { Label = Text("Gesture.Browse.AllGestures"), Fingers = 0, Count = items.Count } };
                for (int fingers = 1; fingers <= GestureListFilter.MaximumFingerChip; fingers++)
                {
                    if (counts[fingers] == 0)
                        continue;
                    string label = fingers == 1 ? Text("Gesture.Filter.MouseOrOneFinger")
                        : fingers == GestureListFilter.MaximumFingerChip ? Text("Gesture.Filter.FiveOrMoreFingers")
                        : Text("Gesture.Filter.Fingers", fingers);
                    inputs.Add(new GestureInputOption { Label = label, Fingers = fingers, Count = counts[fingers] });
                }
                InputList.ItemsSource = inputs;
                if (!inputs.Any(o => o.Fingers == _filter.Fingers))
                    _filter.Fingers = 0;
                InputList.SelectedItem = inputs.First(o => o.Fingers == _filter.Fingers);

                for (int i = 0; i < _padToggles.Length; i++)
                    _padToggles[i].IsEnabled = counts[i + 1] > 0;

                var applications = ApplicationManager.Instance.Applications.Where(app => !(app is IgnoredApp));
                var options = new List<GestureApplicationOption>();
                foreach (var application in applications)
                {
                    if (!usage.TryGetValue(application.Name, out int count))
                        continue;
                    options.Add(new GestureApplicationOption { Name = application.Name, DisplayName = ApplicationDisplayName(application), Application = application, Count = count });
                }
                // The global actions lead, then the busiest applications; the gestures nothing uses close the list.
                options = options
                    .OrderByDescending(o => o.Application is GlobalApp)
                    .ThenByDescending(o => o.Count)
                    .ThenBy(o => o.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                if (unused > 0)
                    options.Add(new GestureApplicationOption { DisplayName = Text("Gesture.Browse.Unused"), IsUnused = true, Count = unused });
                _applicationOptions = options;

                // A selection whose application lost its last gesture can no longer stay.
                if (_filter.Application != null && !options.Any(o => o.Name == _filter.Application))
                    _filter.Application = null;
                if (_filter.UnusedOnly && unused == 0)
                    _filter.UnusedOnly = false;

                ShowApplicationOptions();
                SyncPadToggles();
            }
            finally
            {
                _updatingRail = false;
            }

            RefreshFilter();
        }

        /// <summary>Lists the applications that match the search box, keeping the selected one selected.</summary>
        private void ShowApplicationOptions()
        {
            bool wasUpdating = _updatingRail;
            _updatingRail = true;
            try
            {
                string search = ApplicationSearchBox.Text?.Trim();
                var shown = string.IsNullOrEmpty(search)
                    ? _applicationOptions
                    : _applicationOptions.Where(o => o.DisplayName.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
                ApplicationList.ItemsSource = shown;
                ApplicationList.SelectedItem = shown.FirstOrDefault(o => o.IsUnused ? _filter.UnusedOnly : (_filter.Application != null && o.Name == _filter.Application));
            }
            finally
            {
                _updatingRail = wasUpdating;
            }
        }

        #endregion

        #region Draw to find

        private void SketchPad_StrokeChanged(object sender, EventArgs e)
        {
            _drawn = GestureSimilarity.Prepare(SketchPad.Stroke.Select(p => new PointF((float)p.X, (float)p.Y)));
            RefreshFilter();
        }

        private void ClearPad_Click(object sender, RoutedEventArgs e)
        {
            SketchPad.Clear();
        }

        private void MatchCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var match = ((FrameworkElement)sender).DataContext as GestureMatch;
            if (match == null)
                return;
            lstAvailableGestures.SelectedItem = match.Item;
            lstAvailableGestures.ScrollIntoView(match.Item);
        }

        private void UpdateSimilarity()
        {
            foreach (var item in GestureItemProvider.GestureItems)
                item.Similarity = _drawn == null ? 0 : GestureSimilarity.Score(item.Gesture, _drawn);
        }

        private void UpdateMatches()
        {
            var view = GestureViewSource.View;
            var best = _drawn == null || view == null
                ? new List<GestureItem>()
                : view.Cast<GestureItem>().Take(_matchCount).Where(i => i.Similarity > 0).ToList();

            if (best.Count == 0)
            {
                MatchCards.ItemsSource = null;
                MatchCards.Visibility = Visibility.Collapsed;
                MatchPlaceholder.Visibility = Visibility.Visible;
                MatchPlaceholderText.Text = _drawn != null && view != null && view.IsEmpty
                    ? Text("Gesture.Filter.NoMatches")
                    : Text("Gesture.Draw.Placeholder");
                return;
            }

            MatchCards.ItemsSource = best.Select((item, index) => new GestureMatch
            {
                Item = item,
                IsBest = index == 0,
                PercentText = Text("Gesture.Draw.Match", (int)Math.Round(item.Similarity)),
                ActionsText = ActionsText(item.ActionCount)
            }).ToList();
            MatchCards.Visibility = Visibility.Visible;
            MatchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private static string ActionsText(int count)
        {
            return count == 0 ? Text("Gesture.Browse.Unused")
                : count == 1 ? Text("Gesture.Draw.ActionsOne")
                : Text("Gesture.Draw.ActionsMany", count);
        }

        #endregion

        #region List

        private SortMode EffectiveSort
        {
            get
            {
                if (_drawn != null)
                    return SortMode.Best;
                return _sortMode == SortMode.Best ? SortMode.MostUsed : _sortMode;
            }
        }

        private void ApplySort(ICollectionView view)
        {
            using (view.DeferRefresh())
            {
                var sort = view.SortDescriptions;
                sort.Clear();
                switch (EffectiveSort)
                {
                    case SortMode.Best:
                        sort.Add(new SortDescription(nameof(GestureItem.Similarity), ListSortDirection.Descending));
                        sort.Add(new SortDescription(nameof(GestureItem.ActionCount), ListSortDirection.Descending));
                        break;
                    case SortMode.MostUsed:
                        sort.Add(new SortDescription(nameof(GestureItem.ActionCount), ListSortDirection.Descending));
                        sort.Add(new SortDescription(nameof(GestureItem.PatternCount), ListSortDirection.Ascending));
                        sort.Add(new SortDescription(nameof(GestureItem.Features), ListSortDirection.Ascending));
                        break;
                    case SortMode.Column:
                        sort.Add(new SortDescription(_sortColumn, _sortDirection));
                        sort.Add(new SortDescription(nameof(GestureItem.PatternCount), ListSortDirection.Ascending));
                        break;
                    default:
                        sort.Add(new SortDescription(nameof(GestureItem.PatternCount), ListSortDirection.Ascending));
                        sort.Add(new SortDescription(nameof(GestureItem.Features), ListSortDirection.Ascending));
                        break;
                }
            }
        }

        /// <summary>Filter, similarity or sort changed: rebuild the list, the match cards, the header and the inspector.</summary>
        private void RefreshFilter()
        {
            if (!_ready)
                return;

            var view = GestureViewSource.View;
            if (view == null)
                return;

            UpdateSimilarity();
            ApplySort(view);
            view.Refresh();

            int total = GestureItemProvider.GestureItems.Count;
            int shown = view.Cast<object>().Count();

            UpdateMatches();
            UpdateHeader(shown, total);
            NoMatchesText.Visibility = shown == 0 && total > 0 ? Visibility.Visible : Visibility.Collapsed;
            ClearFiltersButton.Visibility = _filter.IsActive ? Visibility.Visible : Visibility.Collapsed;
            UpdateInspector();
        }

        private void UpdateHeader(int shown, int total)
        {
            string title;
            if (_filter.Fingers == 0)
                title = Text("Gesture.List.AllGestures");
            else if (_filter.Fingers == 1)
                title = Text("Gesture.List.AllMouse");
            else if (_filter.Fingers >= GestureListFilter.MaximumFingerChip)
                title = Text("Gesture.List.AllFivePlus");
            else
                title = Text("Gesture.List.AllFingers", _filter.Fingers);

            if (_filter.Application != null)
            {
                var option = _applicationOptions.FirstOrDefault(o => o.Name == _filter.Application);
                title += " · " + (option?.DisplayName ?? _filter.Application);
            }
            else if (_filter.UnusedOnly)
            {
                title += " · " + Text("Gesture.Browse.Unused");
            }
            ListTitleText.Text = title;

            ListSubtitleText.Text = _drawn != null ? Text("Gesture.List.Ranked", shown)
                : _filter.IsActive ? Text("Gesture.Filter.Showing", shown, total)
                : Text("Gesture.List.Count", total);

            string sortName;
            switch (EffectiveSort)
            {
                case SortMode.Best: sortName = Text("Gesture.List.SortBest"); break;
                case SortMode.MostUsed: sortName = Text("Gesture.List.SortMostUsed"); break;
                case SortMode.Column: sortName = ColumnDisplayName(_sortColumn); break;
                default: sortName = Text("Gesture.List.SortDefault"); break;
            }
            SortButton.Tag = Text("Gesture.List.Sort", sortName);
        }

        private static string ColumnDisplayName(string column)
        {
            switch (column)
            {
                case nameof(GestureItem.Description): return Text("Gesture.DescriptionColumn");
                case nameof(GestureItem.Applications): return Text("Gesture.Applications");
                default: return Text("Gesture.Header");
            }
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)Resources["SortMenu"];
            foreach (MenuItem item in menu.Items)
            {
                string mode = (string)item.Tag;
                item.IsChecked = mode == EffectiveSort.ToString();
                // Best match only has meaning while something is drawn.
                item.IsEnabled = mode != nameof(SortMode.Best) || _drawn != null;
            }
            menu.PlacementTarget = SortButton;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void SortMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _sortMode = (SortMode)Enum.Parse(typeof(SortMode), (string)((MenuItem)sender).Tag);
            RefreshFilter();
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Role == GridViewColumnHeaderRole.Padding)
                return;

            string column = headerClicked.Tag as string;
            if (column == null)
                return;

            _sortDirection = _sortMode == SortMode.Column && _sortColumn == column && _sortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            _sortColumn = column;
            _sortMode = SortMode.Column;
            RefreshFilter();
        }

        #endregion

        #region Selection and inspector

        private void lstAvailableGestures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInspector();
        }

        private void UpdateInspector()
        {
            var selected = lstAvailableGestures.SelectedItems.OfType<GestureItem>().ToList();
            this.btnEditGesture.IsEnabled = this.btnDelGesture.IsEnabled = selected.Count > 0;

            if (selected.Count == 0)
            {
                InspectorContent.Visibility = Visibility.Collapsed;
                InspectorEmptyText.Visibility = Visibility.Visible;
                StatusText = Text("Gesture.Filter.Showing", lstAvailableGestures.Items.Count, GestureItemProvider.GestureItems.Count);
                return;
            }

            InspectorContent.Visibility = Visibility.Visible;
            InspectorEmptyText.Visibility = Visibility.Collapsed;

            var item = selected[0];
            var color = (System.Windows.Media.Color)Application.Current.Resources["HighlightColor"];
            InspectorImage.Source = GestureImage.CreateImage(item.Gesture.PointPatterns, new System.Windows.Size(112, 112), color);

            bool showMatch = _drawn != null && item.Similarity > 0;
            InspectorMatchBadge.Visibility = showMatch ? Visibility.Visible : Visibility.Collapsed;
            if (showMatch)
                InspectorMatchText.Text = Text("Gesture.Draw.Match", (int)Math.Round(item.Similarity));

            if (selected.Count > 1)
            {
                InspectorTitleText.Text = Text("Gesture.Inspector.Selected", selected.Count);
                InspectorSubtitleText.Text = string.Empty;
                UsedByHeaderText.Visibility = Visibility.Collapsed;
                UsageList.ItemsSource = null;
                UnusedText.Visibility = Visibility.Collapsed;
                StatusText = Text("Gesture.Status.Selected", selected.Count);
                return;
            }

            InspectorTitleText.Text = char.ToUpper(item.Description[0], System.Globalization.CultureInfo.CurrentUICulture) + item.Description.Substring(1);
            InspectorSubtitleText.Text = item.MovingStrokeCount == 1
                ? Text("Gesture.Inspector.StrokeOne")
                : Text("Gesture.Inspector.StrokeMany", item.MovingStrokeCount);

            var usages = UsagesOf(item);
            UsedByHeaderText.Visibility = usages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            UsedByHeaderText.Text = (usages.Count == 1 ? Text("Gesture.Inspector.UsedByOne") : Text("Gesture.Inspector.UsedBy", usages.Count))
                .ToUpper(System.Globalization.CultureInfo.CurrentUICulture);
            UsageList.ItemsSource = usages;
            UnusedText.Visibility = usages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            StatusText = Text("Gesture.Status.Selected", 1) + " · " + item.Description + " · "
                + (usages.Count == 0 ? Text("Gesture.Status.NotUsed") : Text("Gesture.Status.UsedBy", ActionsText(usages.Count)));
        }

        private static List<GestureUsage> UsagesOf(GestureItem item)
        {
            var usages = new List<GestureUsage>();
            foreach (var application in ApplicationManager.Instance.Applications.Where(app => !(app is IgnoredApp)))
            {
                foreach (var action in application.Actions.Where(a => a.GestureName == item.Gesture.Name))
                    usages.Add(new GestureUsage { Application = application, ApplicationName = ApplicationDisplayName(application), ActionName = action.Name });
            }
            return usages;
        }

        #endregion

        private void btnDelGesture_Click(object sender, RoutedEventArgs e)
        {
            // Make sure at least one item is selected
            if (lstAvailableGestures.SelectedItems.Count == 0) return;
            if (UIHelper.GetParentWindow(this)
                    .ShowModalMessageExternal(
                        LocalizationProvider.Instance.GetTextValue("Gesture.Messages.DeleteConfirmTitle"),
                        LocalizationProvider.Instance.GetTextValue("Gesture.Messages.DeleteGestureConfirm"),
                        MessageDialogStyle.AffirmativeAndNegative,
                        new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                        }) == MessageDialogResult.Affirmative)
            {
                foreach (GestureItem listItem in lstAvailableGestures.SelectedItems)
                    GestureManager.Instance.DeleteGesture(listItem.Gesture.Name);

                GestureManager.Instance.SaveGestures();
            }
        }
        private void btnEditGesture_Click(object sender, RoutedEventArgs e)
        {
            EditGesture();
        }

        private void ImportGestureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdGestures = new OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Gesture.GestureFile") + "|*" + GestureSign.Common.Constants.GesturesExtension,
                Title = LocalizationProvider.Instance.GetTextValue("Gesture.ImportGesture"),
                CheckFileExists = true
            };
            if (ofdGestures.ShowDialog().GetValueOrDefault())
            {
                var newGestures = GestureManager.LoadGesturesFromFile(ofdGestures.FileName);
                ImportGesture(newGestures);
            }
        }

        private void ExportGestureButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = Text("Gesture.GestureFile") + "|*" + GestureSign.Common.Constants.GesturesExtension,
                Title = Text("Gesture.ExportGesture"),
                FileName = GestureSign.Common.Constants.GesturesFileName,
                AddExtension = true,
                DefaultExt = GestureSign.Common.Constants.GesturesExtension
            };
            if (!dialog.ShowDialog().GetValueOrDefault())
                return;

            if (!FileManager.SaveObject(GestureManager.Instance.Gestures, dialog.FileName))
                UIHelper.GetParentWindow(this).ShowModalMessageExternal(Text("Messages.Error"), Text("Messages.FileWriteException"));
        }

        private void ImportGesture(List<IGesture> gestureList)
        {
            int count = GestureManager.Instance.ImportGestures(gestureList, null);

            UIHelper.GetParentWindow(this).ShowModalMessageExternal(
                    LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ImportCompleteTitle"),
                    String.Format(LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ImportComplete"),
                        gestureList.Count - count, count), settings: new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            ColorScheme = MetroDialogColorScheme.Accented,
                        });
        }

        private void ViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedMenuItem = (MenuItem)sender;
            if (!clickedMenuItem.IsChecked)
                clickedMenuItem.IsChecked = true;

            MenuItem parentMenuItem = clickedMenuItem.Parent as MenuItem;
            if (parentMenuItem != null)
                foreach (var item in parentMenuItem.Items)
                {
                    var current = item as MenuItem;
                    if (!ReferenceEquals(current, clickedMenuItem))
                        if (current != null)
                            current.IsChecked = false;
                }
        }

        private void ListViewItem_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.InvokeAsync(EditGesture, DispatcherPriority.Input);
        }

        private void EditGesture()
        {
            // Make sure at least one item is selected
            if (lstAvailableGestures.SelectedItems.Count == 0) return;

            GestureDefinition gd =
                new GestureDefinition(
                    GestureManager.Instance.GetNewestGestureSample(((GestureItem)lstAvailableGestures.SelectedItems[0]).Gesture.Name));
            var result = gd.ShowDialog();
            if (result != null && result.Value)
            {
                lstAvailableGestures.SelectedValue = gd.CurrentGesture;
                lstAvailableGestures.Dispatcher.Invoke(DispatcherPriority.Input,
                    new System.Action(() => lstAvailableGestures.ScrollIntoView(lstAvailableGestures.SelectedItem)));
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (var file in files)
                    {
                        if (file.EndsWith(GestureSign.Common.Constants.GesturesExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            var newGestures = GestureManager.LoadGesturesFromFile(file);
                            ImportGesture(newGestures);
                        }
                    }
                }
                catch (Exception exception)
                {
                    UIHelper.GetParentWindow(this).ShowModalMessageExternal(exception.GetType().Name, exception.Message);
                }
            }
            e.Handled = true;
        }
    }
}
