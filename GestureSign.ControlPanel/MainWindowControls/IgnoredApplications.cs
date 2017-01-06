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
            var ignoredApps = lstIgnoredApplications.SelectedItems.Cast<IgnoredApplication>().ToList();
            foreach (var app in ignoredApps)
            {
                ApplicationManager.Instance.RemoveApplication(app);
            }
            ApplicationManager.Instance.SaveApplications();
        }

        private void btnEditIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            IgnoredApplication ia = this.lstIgnoredApplications.SelectedItem as IgnoredApplication;
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
            foreach (IgnoredApplication ia in this.lstIgnoredApplications.Items)
                ia.IsEnabled = isChecked;
        }

        private void IgnoredAppCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            (ApplicationManager.Instance.Applications.Find(app => app.Name == ((CheckBox)sender).Tag as string)
                as IgnoredApplication).IsEnabled = true;
            ApplicationManager.Instance.SaveApplications();
        }

        private void IgnoredAppCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            (ApplicationManager.Instance.Applications.Find(app => app.Name == ((CheckBox)sender).Tag as string)
                as IgnoredApplication).IsEnabled = false;
            ApplicationManager.Instance.SaveApplications();
        }

        private void ImportIgnoredAppsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Ignored.IgnoredAppFile") + "|*.ign",
                Title = LocalizationProvider.Instance.GetTextValue("Ignored.ImportIgnoredApps"),
                CheckFileExists = true
            };
            if (ofdApplications.ShowDialog().Value)
            {
                int addcount = 0;
                List<IApplication> newApps = FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, false, true);

                if (newApps != null)
                    foreach (IApplication newApp in newApps)
                    {
                        if (newApp is IgnoredApplication &&
                            !ApplicationManager.Instance.ApplicationExists(newApp.Name))
                        {
                            ApplicationManager.Instance.AddApplication(newApp);
                            addcount++;
                        }
                    }
                if (addcount != 0)
                {
                    ApplicationManager.Instance.SaveApplications();
                }
                MessageBox.Show(
                    String.Format(LocalizationProvider.Instance.GetTextValue("Ignored.Messages.ImportComplete"),
                        addcount),
                    LocalizationProvider.Instance.GetTextValue("Ignored.Messages.ImportCompleteTitle"));
            }
        }

        private void ExportIgnoredAppsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Ignored.IgnoredAppFile") + "|*.ign",
                Title = LocalizationProvider.Instance.GetTextValue("Ignored.ExportIgnoredApps"),
                AddExtension = true,
                DefaultExt = "ign",
                ValidateNames = true
            };
            if (sfdApplications.ShowDialog().Value)
            {
                FileManager.SaveObject(ApplicationManager.Instance.Applications.Where(app => (app is IgnoredApplication)).ToList(), sfdApplications.FileName, true);
            }
        }
    }
}
