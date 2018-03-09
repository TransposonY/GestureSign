using GestureSign.Common.Applications;
using GestureSign.Common.Extensions;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
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
    public partial class ExportImportDialog : TouchWindow
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
                    Filter = LocalizationProvider.Instance.GetTextValue("Action.ArchiveFile") + "|*" + GestureSign.Common.Constants.ArchivesExtension,
                    FileName = LocalizationProvider.Instance.GetTextValue("Action.ArchiveFile") + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Title = LocalizationProvider.Instance.GetTextValue("Common.Export"),
                    AddExtension = true,
                    DefaultExt = GestureSign.Common.Constants.ArchivesExtension.Remove(0, 1),
                    ValidateNames = true
                };
                if (sfdApplications.ShowDialog().Value)
                {
                    var seletedApplications = ApplicationSelector.SeletedApplications;
                    var gestures = seletedApplications.GetRelatedGestures(GestureManager.Instance.Gestures);

                    try
                    {
                        Archive.CreateArchive(seletedApplications, gestures, sfdApplications.FileName);

                        int actionCount = seletedApplications.Sum(app => app.Actions == null ? 0 : app.Actions.Count());
                        var message = actionCount == 0 ? String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ExportCompleteWithoutAction"), seletedApplications.Count) :
                           String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ExportComplete"), actionCount, seletedApplications.Count);

                        this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ExportCompleteTitle"), message);
                    }
                    catch (Exception exception)
                    {
                        this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Messages.Error"), exception.Message);
                    }
                    Close();
                }
            }
            else
            {
                int newActionCount = 0;
                List<IApplication> newApplications = new List<IApplication>();
                var seletedApplications = ApplicationSelector.SeletedApplications;

                var gestures = seletedApplications.GetRelatedGestures(ApplicationSelector.GestureMap.Values);
                GestureManager.Instance.ImportGestures(gestures, seletedApplications);

                foreach (IApplication newApp in seletedApplications)
                {
                    if (newApp is IgnoredApp)
                    {
                        var matchApp = ApplicationManager.Instance.FindMatchApplications<IgnoredApp>(newApp.MatchUsing, newApp.MatchString);
                        if (matchApp.Length == 0)
                        {
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
                                existingApp.AddAction(newAction);
                                newActionCount++;
                            }
                        }
                        else
                        {
                            newActionCount += newApp.Actions.Count();
                            newApplications.Add(newApp);
                        }
                    }
                }

                if (newApplications.Count != 0)
                {
                    ApplicationManager.Instance.AddApplicationRange(newApplications);
                }
                if (newApplications.Count + newActionCount != 0)
                    ApplicationManager.Instance.SaveApplications();

                this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ImportCompleteTitle"),
                    String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ImportComplete"), newActionCount, newApplications.Count));
                Close();
            }
        }
    }
}
