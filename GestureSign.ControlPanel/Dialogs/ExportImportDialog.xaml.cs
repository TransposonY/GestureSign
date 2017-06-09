using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for ExportImportDialog.xaml
    /// </summary>
    public partial class ExportImportDialog : MetroWindow
    {
        private bool _isExportMode;

        public ExportImportDialog(bool isExportMode, bool showIgnore, IEnumerable<IApplication> apps, IEnumerable<IGesture> gestures)
        {
            _isExportMode = isExportMode;

            InitializeComponent();

            ApplicationSelector.Initialize(apps, gestures, showIgnore, !_isExportMode);

            Title = LocalizationProvider.Instance.GetTextValue(_isExportMode ? "Common.Export" : "Common.Import");
            OkButton.Content = LocalizationProvider.Instance.GetTextValue(_isExportMode ? "ExportImportDialog.ExportButton" : "ExportImportDialog.ImportButton");
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isExportMode)
            {
                Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog()
                {
                    Filter = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + "|*.gsa",
                    FileName = LocalizationProvider.Instance.GetTextValue("Action.ActionFile") + ".gsa",
                    Title = LocalizationProvider.Instance.GetTextValue("Common.Export"),
                    AddExtension = true,
                    DefaultExt = "gsa",
                    ValidateNames = true
                };
                if (sfdApplications.ShowDialog().Value)
                {
                    var seletedApplications = ApplicationSelector.SeletedApplications;
                    FileManager.SaveObject(seletedApplications, sfdApplications.FileName, true);

                    int actionCount = seletedApplications.Sum(app => app.Actions == null ? 0 : app.Actions.Count);
                    var message = actionCount == 0 ? String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ExportCompleteWithoutAction"), seletedApplications.Count) :
                       String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ExportComplete"), actionCount, seletedApplications.Count);

                    this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ExportCompleteTitle"), message);
                    Close();
                }
            }
            else
            {
                int newActionCount = 0, newAppCount = 0;
                List<IApplication> newApplications = new List<IApplication>();
                var seletedApplications = ApplicationSelector.SeletedApplications;

                foreach (IApplication newApp in seletedApplications)
                {
                    if (newApp is IgnoredApp)
                    {
                        var matchApp = ApplicationManager.Instance.FindMatchApplications<IgnoredApp>(newApp.MatchUsing, newApp.MatchString);
                        if (matchApp.Length == 0)
                        {
                            newAppCount++;
                            newApplications.Add(newApp);
                        }
                    }
                    else
                    {
                        var existingApp = ApplicationManager.Instance.Applications.Find(app => !(app is IgnoredApp) && app.MatchUsing == newApp.MatchUsing && app.MatchString == newApp.MatchString);
                        if (existingApp != null)
                        {
                            foreach (IAction newAction in newApp.Actions)
                            {
                                var existingAction = existingApp.Actions.Find(action => action.Name == newAction.Name);
                                if (existingAction != null)
                                {
                                    var result =
                                        MessageBox.Show(String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ReplaceConfirm"),
                                        existingAction.Name, existingApp.Name),
                                        LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ReplaceConfirmTitle"),
                                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        existingApp.Actions.Remove(existingAction);
                                        existingApp.AddAction(newAction);
                                        newActionCount++;
                                    }
                                    else if (result == MessageBoxResult.Cancel) goto End;
                                }
                                else
                                {
                                    existingApp.AddAction(newAction);
                                    newActionCount++;
                                }
                            }
                            newAppCount++;
                        }
                        else
                        {
                            newActionCount += newApp.Actions.Count;
                            newAppCount++;
                            newApplications.Add(newApp);
                        }
                    }
                }

                End:
                if (newApplications.Count != 0)
                {
                    ApplicationManager.Instance.AddApplicationRange(newApplications);
                }
                if (newAppCount + newActionCount != 0)
                    ApplicationManager.Instance.SaveApplications();

                this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ImportCompleteTitle"),
                    String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ImportComplete"), newActionCount, newAppCount));
                Close();
            }
        }
    }
}
