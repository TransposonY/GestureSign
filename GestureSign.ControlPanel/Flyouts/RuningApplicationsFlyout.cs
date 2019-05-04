using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using ManagedWinapi.Windows;
using GestureSign.Common.Log;

namespace GestureSign.ControlPanel.Flyouts
{
    /// <summary>
    /// RuningApplicationsFlyout.xaml 的交互逻辑
    /// </summary>
    public partial class RuningApplicationsFlyout : Flyout
    {
        public static event EventHandler<ApplicationListViewItem> RuningAppSelectionChanged;

        public RuningApplicationsFlyout()
        {
            InitializeComponent();
            this.IsOpenChanged += RuningApplicationsFlyout_IsOpenChanged;
        }

        void RuningApplicationsFlyout_IsOpenChanged(object sender, EventArgs e)
        {
            if (this.IsOpen)
            {
                RefreshApplications();
            }
        }
        private void lstRunningApplications_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RuningAppSelectionChanged != null)
                RuningAppSelectionChanged(this, lstRunningApplications.SelectedItem as ApplicationListViewItem);
        }
        #region Private Instance Methods


        private void RefreshApplications()
        {
            lstRunningApplications.Dispatcher.InvokeAsync(() => lstRunningApplications.Items.Clear(), DispatcherPriority.Input);
            //    this.lstRunningApplications.ItemsSource = await GetValidWindows();
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetValidWindows));
            //await GetValidWindows();
        }

        private void GetValidWindows(object s)
        {
            var processInfoMap = new Dictionary<uint, string>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, Name FROM Win32_Process"))
                using (var results = searcher.Get())
                {
                    foreach (var item in results)
                    {
                        var id = item["ProcessID"];
                        var name = item["Name"] as string;

                        if (name != null)
                        {
                            processInfoMap.Add((uint)id, name);
                        }
                    }
                }
            }
            catch { }

            // Get valid running windows
            var windows = SystemWindow.AllToplevelWindows.Where
                (
                    w => w.Visible && // Must be a visible windows
                         w.Title != "" && // Must have a window title
                         (w.ExtendedStyle & WindowExStyleFlags.TOOLWINDOW) != WindowExStyleFlags.TOOLWINDOW	// Must not be a tool window
                     );

            foreach (SystemWindow sWind in windows)
            {
                try
                {
                    string className, title, fileName;
                    SystemWindow realWindow = GestureSign.Common.Applications.ApplicationManager.GetWindowInfo(sWind, out className, out title, out fileName);
                    if ("ApplicationFrameWindow".Equals(className) || realWindow == null)
                        continue;

                    var pid = (uint)realWindow.ProcessId;
                    if (!processInfoMap.TryGetValue(pid, out fileName))
                    {
                        try
                        {
                            fileName = realWindow.Process.MainModule.FileName;
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    BitmapSource iconSource;
                    try
                    {
                        iconSource = Imaging.CreateBitmapSourceFromHIcon(realWindow.Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        iconSource.Freeze();
                    }
                    catch
                    {
                        continue;
                    }

                    ApplicationListViewItem lItem = new ApplicationListViewItem
                    {
                        WindowClass = className,
                        WindowTitle = title,
                        WindowFilename = fileName,
                        ApplicationIcon = iconSource
                    };

                    //lItem.ApplicationName = sWind.Process.MainModule.FileVersionInfo.FileDescription;
                    this.lstRunningApplications.Dispatcher.InvokeAsync(new Action(() =>
                   {
                       this.lstRunningApplications.Items.Add(lItem);
                   }), DispatcherPriority.Input);

                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                }
            }

        }

        #endregion

    }
}
