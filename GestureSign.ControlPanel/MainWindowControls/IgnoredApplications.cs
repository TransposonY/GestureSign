using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Dialogs;
using IWshRuntimeLibrary;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
            ApplicationManager.Instance.SaveApplications();
        }

        private void IgnoredAppCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ApplicationManager.Instance.SaveApplications();
        }

        private void ImportIgnoredAppsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Action.ApplicationFile") + "|*"+ GestureSign.Common.Constants.ActionExtension,
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

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow DownloadWindow = new DownloadWindow();
            DownloadWindow.Show();
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var newApps = new List<IApplication>();
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (var file in files)
                    {
                        switch (Path.GetExtension(file).ToLower())
                        {
                            case GestureSign.Common.Constants.ActionExtension:
                                var apps = FileManager.LoadObject<List<IApplication>>(file, false, true);
                                if (apps != null)
                                {
                                    newApps.AddRange(apps);
                                }
                                break;
                            case ".exe":
                                Dispatcher.InvokeAsync(() => lstIgnoredApplications.SelectedItem = ApplicationManager.Instance.AddApplication(new IgnoredApp() { IsEnabled = true }, file), DispatcherPriority.Input);
                                break;
                            case ".lnk":
                                WshShell shell = new WshShell();
                                IWshShortcut link = (IWshShortcut)shell.CreateShortcut(file);
                                if (Path.GetExtension(link.TargetPath).ToLower() == ".exe")
                                {
                                    Dispatcher.InvokeAsync(() => lstIgnoredApplications.SelectedItem = ApplicationManager.Instance.AddApplication(new IgnoredApp() { IsEnabled = true }, link.TargetPath), DispatcherPriority.Input);
                                }
                                break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Common.UIHelper.GetParentWindow(this).ShowModalMessageExternal(exception.GetType().Name, exception.Message);
                }
                if (newApps.Count != 0)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        ExportImportDialog exportImportDialog = new ExportImportDialog(false, true, newApps, GestureSign.Common.Gestures.GestureManager.Instance.Gestures);
                        exportImportDialog.ShowDialog();
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            e.Handled = true;
        }
    }
}
