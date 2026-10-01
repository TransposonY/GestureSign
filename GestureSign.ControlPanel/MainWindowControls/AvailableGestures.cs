using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
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
    public partial class AvailableGestures : UserControl
    {
        private readonly GestureListFilter _filter = new GestureListFilter();
        private bool _ready;
        private bool _updatingApplications;

        public AvailableGestures()
        {
            InitializeComponent();

            var localization = LocalizationProvider.Instance;
            string fingers = localization.GetTextValue("Gesture.Filter.Fingers");
            TwoFingersLabel.Text = string.Format(fingers, 2);
            ThreeFingersLabel.Text = string.Format(fingers, 3);
            FourFingersLabel.Text = string.Format(fingers, 4);

            _ready = true;
            GestureItemProvider.GestureMapChanged += (o, e) => OnGesturesChanged();
            OnGesturesChanged();
        }

        private CollectionViewSource GestureViewSource
        {
            get { return (CollectionViewSource)Resources["GestureViewSource"]; }
        }

        private void GestureViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = _filter.Matches(e.Item as GestureItem);
        }

        private void FingerFilter_Checked(object sender, RoutedEventArgs e)
        {
            if (!_ready)
                return;
            int value;
            _filter.Fingers = int.TryParse(((RadioButton)sender).Tag as string, out value) ? value : 0;
            RefreshFilter();
        }

        private void ShapeFilter_Checked(object sender, RoutedEventArgs e)
        {
            if (!_ready)
                return;
            GestureShapeFilter value;
            _filter.Shape = Enum.TryParse(((RadioButton)sender).Tag as string, out value) ? value : GestureShapeFilter.Any;
            RefreshFilter();
        }

        private void DotFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (!_ready)
                return;
            _filter.DotOnly = DotToggle.IsChecked == true;
            RefreshFilter();
        }

        private void ApplicationFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _updatingApplications)
                return;
            _filter.Application = ApplicationFilterBox.SelectedIndex > 0 ? ApplicationFilterBox.SelectedItem as string : null;
            RefreshFilter();
        }

        private void DescriptionSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_ready)
                return;
            var tokens = ActionListFilter.Tokenize(DescriptionSearchBox.Text);
            if (ActionListFilter.SameTokens(_filter.Tokens, tokens))
                return;
            _filter.Tokens = tokens;
            RefreshFilter();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            AllFingersRadio.IsChecked = true;
            AnyShapeRadio.IsChecked = true;
            DotToggle.IsChecked = false;
            ApplicationFilterBox.SelectedIndex = 0;
            DescriptionSearchBox.Text = string.Empty;
        }

        /// <summary>Gestures, applications or actions changed: recount the chips, rebuild the application list, re-apply the filter.</summary>
        private void OnGesturesChanged()
        {
            var items = GestureItemProvider.GestureItems;

            var counts = new int[GestureListFilter.MaximumFingerChip + 1];
            var applications = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var item in items)
            {
                counts[Math.Max(0, Math.Min(item.PatternCount, GestureListFilter.MaximumFingerChip))]++;
                foreach (var name in item.ApplicationNames)
                    applications.Add(name);
            }

            AllFingersCount.Text = items.Count.ToString();
            OneFingerCount.Text = counts[1].ToString();
            TwoFingersCount.Text = counts[2].ToString();
            ThreeFingersCount.Text = counts[3].ToString();
            FourFingersCount.Text = counts[4].ToString();
            FiveFingersCount.Text = counts[5].ToString();
            OneFingerRadio.IsEnabled = counts[1] > 0;
            TwoFingersRadio.IsEnabled = counts[2] > 0;
            ThreeFingersRadio.IsEnabled = counts[3] > 0;
            FourFingersRadio.IsEnabled = counts[4] > 0;
            FiveFingersRadio.IsEnabled = counts[5] > 0;

            // A chip that just ran out of gestures can no longer stay selected.
            foreach (var radio in new[] { OneFingerRadio, TwoFingersRadio, ThreeFingersRadio, FourFingersRadio, FiveFingersRadio })
                if (!radio.IsEnabled && radio.IsChecked == true)
                    AllFingersRadio.IsChecked = true;

            string selectedApplication = _filter.Application;
            _updatingApplications = true;
            try
            {
                var list = new List<string> { LocalizationProvider.Instance.GetTextValue("Gesture.Filter.AnyApplication") };
                list.AddRange(applications);
                ApplicationFilterBox.ItemsSource = list;
                int index = selectedApplication == null ? 0 : list.IndexOf(selectedApplication);
                ApplicationFilterBox.SelectedIndex = index > 0 ? index : 0;
            }
            finally
            {
                _updatingApplications = false;
            }
            if (selectedApplication != null && ApplicationFilterBox.SelectedIndex == 0)
                _filter.Application = null;

            RefreshFilter();
        }

        private void RefreshFilter()
        {
            var view = GestureViewSource.View;
            view?.Refresh();

            int total = GestureItemProvider.GestureItems.Count;
            int shown = view == null ? total : view.Cast<object>().Count();
            ResultCountText.Text = string.Format(LocalizationProvider.Instance.GetTextValue("Gesture.Filter.Showing"), shown, total);
            ClearFiltersButton.Visibility = _filter.IsActive ? Visibility.Visible : Visibility.Collapsed;
            NoMatchesText.Visibility = shown == 0 && total > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void lstAvailableGestures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.btnEditGesture.IsEnabled = this.btnDelGesture.IsEnabled = lstAvailableGestures.SelectedItems.Count > 0;
        }

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

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction = ListSortDirection.Ascending;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    string header = headerClicked.Tag as string;
                    ICollectionView dataView = CollectionViewSource.GetDefaultView(lstAvailableGestures.ItemsSource);
                    if (dataView.SortDescriptions.Count > 1)
                    {
                        direction = dataView.SortDescriptions[0].Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                        dataView.SortDescriptions.Clear();
                        dataView.SortDescriptions.Add(new SortDescription("PatternCount", direction));
                    }
                    if (header != null)
                    {
                        SortDescription sd = new SortDescription(header, direction);
                        dataView.SortDescriptions.Add(sd);
                    }
                    dataView.Refresh();
                }
            }
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
                    new Action(() => lstAvailableGestures.ScrollIntoView(lstAvailableGestures.SelectedItem)));
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
