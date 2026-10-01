using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GestureSign.Common.Applications;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Dialogs;
using GestureSign.ControlPanel.ViewModel;
using MahApps.Metro.Controls.Dialogs;

namespace GestureSign.ControlPanel.MainWindowControls
{
    /// <summary>
    /// Continuous gesture catalog: create, edit and delete named continuous gestures and bind them to actions.
    /// </summary>
    public partial class ContinuousGestures : UserControl
    {
        public ContinuousGestures()
        {
            InitializeComponent();
        }

        private void ContinuousGestureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EditButton.IsEnabled = DeleteButton.IsEnabled = ContinuousGestureListView.SelectedItems.Count > 0;
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDialog(null);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditSelected();
        }

        private void ListViewItem_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.InvokeAsync(EditSelected, DispatcherPriority.Input);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ContinuousGestureListView.SelectedItems.Cast<ContinuousGestureItem>().Select(item => item.Gesture).ToList();
            if (selected.Count == 0) return;

            if (UIHelper.GetParentWindow(this)
                    .ShowModalMessageExternal(
                        LocalizationProvider.Instance.GetTextValue("ContinuousGesture.Messages.DeleteConfirmTitle"),
                        string.Format(LocalizationProvider.Instance.GetTextValue("ContinuousGesture.Messages.DeleteConfirm"),
                            string.Join(", ", selected.Select(g => g.Name))),
                        MessageDialogStyle.AffirmativeAndNegative,
                        new MetroDialogSettings()
                        {
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                        }) != MessageDialogResult.Affirmative)
                return;

            var names = new HashSet<string>(selected.Select(g => g.Name));
            bool unbound = false;
            foreach (var action in ApplicationManager.Instance.Applications.Where(app => app.Actions != null).SelectMany(app => app.Actions))
            {
                if (action?.ContinuousGestureName != null && names.Contains(action.ContinuousGestureName))
                {
                    action.ContinuousGestureName = null;
                    unbound = true;
                }
            }

            foreach (var gesture in selected)
                ContinuousGestureManager.Instance.Remove(gesture);

            ContinuousGestureManager.Instance.Save();
            if (unbound)
                ApplicationManager.Instance.SaveApplications();
            ContinuousGestureItemProvider.Refresh();
        }

        private void EditSelected()
        {
            var item = ContinuousGestureListView.SelectedItem as ContinuousGestureItem;
            if (item == null) return;
            OpenDialog(item.Gesture);
        }

        private void OpenDialog(ContinuousGesture gesture)
        {
            var dialog = new ContinuousGestureDialog(gesture) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;

            // Rebuild now so the edited row exists before selecting it; the save events only refresh it in place afterwards.
            ContinuousGestureItemProvider.Refresh();
            ContinuousGestureListView.SelectedValue = dialog.Result;
            if (ContinuousGestureListView.SelectedItem != null)
                ContinuousGestureListView.ScrollIntoView(ContinuousGestureListView.SelectedItem);
        }
    }
}
