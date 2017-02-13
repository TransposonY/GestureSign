using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GestureSign.Common.Configuration;
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

        private void NewGestureButton_Click(object sender, RoutedEventArgs e)
        {
            GestureDefinition gestureDefinition = new GestureDefinition();
            var result = gestureDefinition.ShowDialog();
            if (result != null && result.Value)
            {
                lstAvailableGestures.SelectedValue = GestureManager.Instance.GestureName;
                lstAvailableGestures.Dispatcher.Invoke(DispatcherPriority.Input, new Action(() => lstAvailableGestures.ScrollIntoView(lstAvailableGestures.SelectedItem)));
            }
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
                    GestureManager.Instance.DeleteGesture(listItem.Name);

                GestureManager.Instance.SaveGestures();
            }
        }
        private void btnEditGesture_Click(object sender, RoutedEventArgs e)
        {
            // Make sure at least one item is selected
            if (lstAvailableGestures.SelectedItems.Count == 0) return;

            GestureDefinition gd = new GestureDefinition(GestureManager.Instance.GetNewestGestureSample(((GestureItem)lstAvailableGestures.SelectedItems[0]).Name));
            var result = gd.ShowDialog();
            if (result != null && result.Value)
            {
                lstAvailableGestures.SelectedValue = GestureManager.Instance.GestureName;
                lstAvailableGestures.Dispatcher.Invoke(DispatcherPriority.Input, new Action(() => lstAvailableGestures.ScrollIntoView(lstAvailableGestures.SelectedItem)));
            }
        }

        private void ImportGestureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdGestures = new OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Gesture.GestureFile") + "|*.gest",
                Title = LocalizationProvider.Instance.GetTextValue("Gesture.ImportGesture"),
                CheckFileExists = true
            };
            if (ofdGestures.ShowDialog().Value)
            {
                int addcount = 0;

                var gestures =
                        FileManager.LoadObject<List<Gesture>>(ofdGestures.FileName, false);
                var newGestures = gestures?.Cast<IGesture>().ToList();

                if (newGestures != null)
                    foreach (IGesture newGesture in newGestures)
                    {
                        if (GestureManager.Instance.GestureExists(newGesture.Name))
                        {
                            var result =
                                MessageBox.Show(
                                    String.Format(
                                        LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ReplaceConfirm"),
                                        newGesture.Name),
                                    LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ReplaceConfirmTitle"),
                                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                GestureManager.Instance.DeleteGesture(newGesture.Name);
                                GestureManager.Instance.AddGesture(newGesture);
                                addcount++;
                            }
                            else if (result == MessageBoxResult.Cancel) goto End;
                        }
                        else
                        {
                            GestureManager.Instance.AddGesture(newGesture);
                            addcount++;
                        }
                    }
                End:
                if (addcount != 0)
                {
                    GestureManager.Instance.SaveGestures();
                }
                MessageBox.Show(
                    String.Format(LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ImportComplete"),
                        addcount), LocalizationProvider.Instance.GetTextValue("Gesture.Messages.ImportCompleteTitle"));
            }
        }

        private void ExportGestureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfdGestures = new SaveFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Gesture.GestureFile") + "|*.gest",
                Title = LocalizationProvider.Instance.GetTextValue("Gesture.ExportGestures"),
                AddExtension = true,
                DefaultExt = "gest",
                ValidateNames = true
            };
            if (sfdGestures.ShowDialog().Value)
            {
                FileManager.SaveObject(GestureManager.Instance.Gestures, sfdGestures.FileName);
            }
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
    }
}
