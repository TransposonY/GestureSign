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
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

namespace GestureSign.ControlPanel.MainWindowControls
{
    /// <summary>
    /// AvailableGestures.xaml 的交互逻辑
    /// </summary>
    public partial class AvailableGestures : UserControl
    {
        public AvailableGestures()
        {
            InitializeComponent();
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
            if (ofdGestures.ShowDialog().Value)
            {
                var gestures = FileManager.LoadObject<List<Gesture>>(ofdGestures.FileName, false);
                var newGestures = gestures?.Cast<IGesture>().ToList();

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
                    string header = (headerClicked.Column.DisplayMemberBinding as Binding)?.Path.Path;
                    ICollectionView dataView = CollectionViewSource.GetDefaultView(lstAvailableGestures.ItemsSource);
                    if (dataView.SortDescriptions.Count != 0)
                    {
                        var lastDirection = dataView.SortDescriptions[0].Direction;
                        direction = lastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                        dataView.SortDescriptions.Clear();
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
                            var gestures = FileManager.LoadObject<List<Gesture>>(file, false);
                            var newGestures = gestures?.Cast<IGesture>().ToList();
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
