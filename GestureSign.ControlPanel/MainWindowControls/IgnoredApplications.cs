using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Dialogs;

namespace GestureSign.ControlPanel.MainWindowControls
{
    /// <summary>
    /// AvailableApplications.xaml 的交互逻辑
    /// </summary>
    public partial class IgnoredApplications : UserControl
    {
        public IgnoredApplications()
        {
            InitializeComponent();
        }

        private void btnDeleteIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            var ignoredApps = lstIgnoredApplications.SelectedItems.Cast<IgnoredApp>().ToList();
            foreach (var app in ignoredApps)
            {
                ApplicationManager.Instance.RemoveApplication(app);
            }
            ApplicationManager.Instance.SaveApplications();
        }

        private void btnEditIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            var ia = this.lstIgnoredApplications.SelectedItem as IgnoredApp;
            if (ia == null) return;

            ApplicationDialog applicationDialog = new ApplicationDialog(ia);
            applicationDialog.ShowDialog();
        }

        private void btnAddIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            this.lstIgnoredApplications.SelectedIndex = -1;

            ApplicationDialog applicationDialog = new ApplicationDialog(false);
            applicationDialog.ShowDialog();
        }



        private void lstIgnoredApplications_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.btnEditIgnoredApp.IsEnabled = this.btnDeleteIgnoredApp.IsEnabled =
                this.lstIgnoredApplications.SelectedItem != null;
        }

        private void EnabledIgnoredAppCheckBoxs_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = (sender as CheckBox).IsChecked.Value;
            foreach (IgnoredApp ia in this.lstIgnoredApplications.Items)
                ia.IsEnabled = isChecked;
        }

        private void IgnoredAppCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            (ApplicationManager.Instance.Applications.Find(app => app.Name == ((CheckBox)sender).Tag as string)
                as IgnoredApp).IsEnabled = true;
            ApplicationManager.Instance.SaveApplications();
        }

        private void IgnoredAppCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            (ApplicationManager.Instance.Applications.Find(app => app.Name == ((CheckBox)sender).Tag as string)
                as IgnoredApp).IsEnabled = false;
            ApplicationManager.Instance.SaveApplications();
        }

        private void ImportIgnoredAppsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + "|*.gsa",
                Title = LocalizationProvider.Instance.GetTextValue("Common.Import"),
                CheckFileExists = true
            };
            if (ofdApplications.ShowDialog().Value)
            {
                var newApps = FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, false, true);
                if (newApps != null)
                {
                    ExportImportDialog exportImportDialog = new ExportImportDialog(false, true, newApps, GestureSign.Common.Gestures.GestureManager.Instance.Gestures);
                    exportImportDialog.ShowDialog();
                }
            }
        }

        private void ExportIgnoredAppsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExportImportDialog exportImportDialog = new ExportImportDialog(true, true, ApplicationManager.Instance.Applications, GestureSign.Common.Gestures.GestureManager.Instance.Gestures);
            exportImportDialog.ShowDialog();
        }
    }
}
