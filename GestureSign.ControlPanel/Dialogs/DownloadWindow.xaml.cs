using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : MetroWindow
    {
        private object _thisLock = new object();
        private bool _isDownloaded;
        private string _tempDirectory;

        private string[] _source = new string[] { "https://coding.net/u/TransposonY/p/GestureSignSettings/git/archive/master",
            "https://github.com/TransposonY/GestureSignSettings/archive/master.zip" };

        public DownloadWindow()
        {
            InitializeComponent();
            _tempDirectory = Path.Combine(AppConfig.LocalApplicationDataPath, "Temp");
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var clientList = new List<WebClient>();

            Action<Task<byte[]>> checkData = (task) =>
            {
                if (task.Exception != null)
                {
                    Console.WriteLine($"{task.Exception.InnerException.GetType().Name}: {task.Exception.InnerException.Message}");
                    return;
                }

                var file = task.Result;
                if (file == null || file.Length == 0)
                    return;

                lock (_thisLock)
                {
                    if (_isDownloaded)
                        return;
                    _isDownloaded = true;
                }

                LoadSettingFile(file);
            };
            var observeExceptions = new Action<Task>(t =>
            {
                Dispatcher.InvokeAsync(() => this.ShowModalMessageExternal(t.Exception.InnerException.GetType().Name, t.Exception.InnerException.Message), DispatcherPriority.Input);
            });

            foreach (string url in _source)
            {
                var client = new WebClient();
                client.Headers.Add(HttpRequestHeader.Accept, "*/*");
                client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; .NET4.0E; .NET4.0C; rv:11.0) like Gecko");
                clientList.Add(client);
                var downloadTask = client.DownloadDataTaskAsync(url);
                downloadTask.ContinueWith(checkData).ContinueWith(observeExceptions, TaskContinuationOptions.OnlyOnFaulted);
            }
            Task.Run(async () =>
            {
                await Task.Delay(10000);
                Dispatcher.Invoke(() =>
                {
                    foreach (var client in clientList)
                    {
                        if (client.IsBusy)
                            client.CancelAsync();
                        client.Dispose();
                    }
                });
            });
        }

        private void LoadSettingFile(byte[] file)
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, true);
            Directory.CreateDirectory(_tempDirectory);

            string filePath = Path.Combine(_tempDirectory, "Setting.zip");

            File.WriteAllBytes(filePath, file);
            ZipFile.ExtractToDirectory(filePath, _tempDirectory);

            var newApps = new List<IApplication>();
            var gestures = new List<IGesture>();
            foreach (string settingFile in Directory.GetFiles(_tempDirectory, "*.*", SearchOption.AllDirectories))
            {
                switch (Path.GetExtension(settingFile))
                {
                    case ".gsa":
                        var currentApps = FileManager.LoadObject<List<IApplication>>(settingFile, false, true);
                        if (currentApps != null)
                        {
                            newApps.AddRange(currentApps);
                        }
                        break;
                    case ".gest":
                        var currentGestures = FileManager.LoadObject<List<Gesture>>(settingFile, false);
                        if (currentGestures != null)
                        {
                            gestures.AddRange(currentGestures);
                        }
                        break;
                }
            }

            Dispatcher.InvokeAsync(() =>
            {
                ApplicationSelector.Initialize(newApps, gestures);
                ProgressRing.Visibility = Visibility.Collapsed;
                ApplicationSelector.Visibility = Visibility.Visible;
            }
            , DispatcherPriority.Input);

            File.Delete(filePath);
            Directory.Delete(_tempDirectory, true);
        }

        private void FromFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = LocalizationProvider.Instance.GetTextValue("Action.ApplicationFile") + "|*.gsa",
                Title = LocalizationProvider.Instance.GetTextValue("Common.Import"),
                CheckFileExists = true
            };
            if (ofdApplications.ShowDialog().Value)
            {
                var newApps = FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, false, true);
                if (newApps != null)
                {
                    Close();
                    ExportImportDialog exportImportDialog = new ExportImportDialog(false, false, newApps, GestureManager.Instance.Gestures);
                    exportImportDialog.ShowDialog();
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            int newActionCount = 0, newAppCount = 0;
            bool saveGesture = false;
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
                            var existingAction = existingApp.Actions.FirstOrDefault(action => action.Name == newAction.Name);
                            if (existingAction != null)
                            {
                                var result =
                                    MessageBox.Show(String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ReplaceConfirm"),
                                    existingAction.Name, existingApp.Name),
                                    LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ReplaceConfirmTitle"),
                                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                                if (result == MessageBoxResult.Yes)
                                {
                                    saveGesture |= UpdateGesture(newAction);
                                    existingApp.RemoveAction(existingAction);
                                    existingApp.AddAction(newAction);
                                    newActionCount++;
                                }
                                else if (result == MessageBoxResult.Cancel) goto End;
                            }
                            else
                            {
                                saveGesture |= UpdateGesture(newAction);
                                existingApp.AddAction(newAction);
                                newActionCount++;
                            }
                        }
                        newAppCount++;
                    }
                    else
                    {
                        foreach (var action in newApp.Actions)
                        {
                            saveGesture |= UpdateGesture(action);
                            newActionCount++;
                        }
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
            if (saveGesture)
                GestureManager.Instance.SaveGestures();

            this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ImportCompleteTitle"),
                String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ImportComplete"), newActionCount, newAppCount));
            Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action">Related action</param>
        /// <returns>Indicates whether the new gesture was added</returns>
        private bool UpdateGesture(IAction action)
        {
            PointPattern[] gesture;
            if (ApplicationSelector.PatternMap.TryGetValue(action.GestureName, out gesture))
            {
                var existingGesture = GestureManager.Instance.GetMostSimilarGestureName(gesture);
                if (existingGesture != null)
                {
                    action.GestureName = existingGesture;
                }
                else
                {
                    if (GestureManager.Instance.GestureExists(action.GestureName))
                    {
                        action.GestureName = GestureManager.GetNewGestureName();
                    }
                    GestureManager.Instance.AddGesture(new Gesture(action.GestureName, gesture));
                    return true;
                }
            }
            return false;
        }
    }
}
