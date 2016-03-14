using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using ManagedWinapi.Windows;

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

        void EditApplicationFlyout_OpenIgnoredRuningFlyout(object sender, EventArgs e)
        {
            this.IsOpen = !IsOpen;
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
            this.lstRunningApplications.Items.Clear();
            //    this.lstRunningApplications.ItemsSource = await GetValidWindows();
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetValidWindows));
            //await GetValidWindows();
        }

        private void GetValidWindows(object s)
        {
            // Get valid running windows
            var windows = SystemWindow.AllToplevelWindows.Where
                     (
                         w => w.Visible &&	// Must be a visible windows
                         w.Title != "" &&	// Must have a window title
                         IsProcessAccessible(w.Process) &&
                        Path.GetDirectoryName(w.Process.ProcessName) != Process.GetCurrentProcess().ProcessName &&	// Must not be a GestureSign window
                         (w.ExtendedStyle & WindowExStyleFlags.TOOLWINDOW) != WindowExStyleFlags.TOOLWINDOW	// Must not be a tool window
                     );

            Thread.Sleep(400);
            foreach (SystemWindow sWind in windows)
            {
                var icon = Imaging.CreateBitmapSourceFromHIcon(sWind.Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                icon.Freeze();
                ApplicationListViewItem lItem = new ApplicationListViewItem
                {
                    WindowClass = sWind.ClassName,
                    WindowTitle = sWind.Title,
                    WindowFilename = Path.GetFileName(sWind.Process.MainModule.FileName),
                    ApplicationIcon = icon
                };
                //lItem.ApplicationName = sWind.Process.MainModule.FileVersionInfo.FileDescription;
                this.lstRunningApplications.Dispatcher.BeginInvoke(new Action(() =>
               {
                   this.lstRunningApplications.Items.Add(lItem);
               }));
            }

        }

        private bool IsProcessAccessible(Process Process)
        {
            try
            {
                ProcessModule module = Process.MainModule;
                return true;
            }
            catch { return false; }
        }

        #endregion

    }
}
