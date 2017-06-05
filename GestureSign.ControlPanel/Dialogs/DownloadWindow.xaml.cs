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
            }
            , DispatcherPriority.Input);

            File.Delete(filePath);
            Directory.Delete(_tempDirectory, true);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            int addcount = 0;
            List<IApplication> newApplications = new List<IApplication>();
            var seletedApplications = ApplicationSelector.SeletedApplications;

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
                else if (ApplicationManager.Instance.ApplicationExists(newApp.Name))
                {
                    var existingApp = ApplicationManager.Instance.Applications.Find(a => a.Name == newApp.Name);
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
                                addcount++;
                            }
                            else if (result == MessageBoxResult.Cancel) goto End;
                        }
                        else
                        {
                            existingApp.AddAction(newAction);
                            addcount++;
                        }
                    }
                }
                else
                {
                    addcount += newApp.Actions.Count;
                    newApplications.Add(newApp);
                }
            }

            End:
            if (newApplications.Count != 0)
            {
                ApplicationManager.Instance.AddApplicationRange(newApplications);
            }
            if (newApplications.Count != 0 || addcount != 0)
                ApplicationManager.Instance.SaveApplications();

            MessageBox.Show(
                String.Format(LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ImportComplete"), addcount, newApplications.Count),
                LocalizationProvider.Instance.GetTextValue("ExportImportDialog.ImportCompleteTitle"));
            Close();
        }
    }
}
