using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Extensions;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
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
    public partial class DownloadWindow : TouchWindow
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
                    case GestureSign.Common.Constants.ActionExtension:
                        var currentApps = FileManager.LoadObject<List<IApplication>>(settingFile, false, true);
                        if (currentApps != null)
                        {
                            newApps.AddRange(currentApps);
                        }
                        break;
                    case GestureSign.Common.Constants.GesturesExtension:
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
                Filter = $"{LocalizationProvider.Instance.GetTextValue("Action.ArchiveFile")}|*{GestureSign.Common.Constants.ActionExtension};*{GestureSign.Common.Constants.ArchivesExtension}",
                Title = LocalizationProvider.Instance.GetTextValue("Common.Import"),
                CheckFileExists = true
            };
            if (ofdApplications.ShowDialog().Value)
            {
                try
                {
                    switch (Path.GetExtension(ofdApplications.FileName).ToLower())
                    {
                        case GestureSign.Common.Constants.ActionExtension:
                            var newApps = FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, false, true, true);
                            if (newApps != null)
                            {
                                Hide();
                                ExportImportDialog exportImportDialog = new ExportImportDialog(false, false, newApps, GestureManager.Instance.Gestures);
                                exportImportDialog.ShowDialog();
                                Close();
                            }
                            break;
                        case GestureSign.Common.Constants.ArchivesExtension:
                            {
                                IEnumerable<IApplication> applications;
                                IEnumerable<IGesture> gestures;
                                Archive.LoadFromArchive(ofdApplications.FileName, out applications, out gestures);
                                if (applications != null && gestures != null)
                                {
                                    Hide();
                                    ExportImportDialog exportImportDialog = new ExportImportDialog(false, false, applications, gestures);
                                    exportImportDialog.ShowDialog();
                                    Close();
                                }
                                break;
                            }
                    }
                }
                catch (Exception exception)
                {
                    this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("Messages.Error"), exception.Message);
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
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
