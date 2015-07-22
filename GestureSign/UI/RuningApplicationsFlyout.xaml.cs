using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using GestureSign.Common.Applications;
using System.Windows.Interop;
using System.Diagnostics;
using System.Collections.ObjectModel;
using GestureSign.UI.Common;
using ManagedWinapi.Windows;

using MahApps.Metro.Controls;

namespace GestureSign.UI
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
            EditApplicationFlyout.OpenIgnoredRuningFlyout += EditApplicationFlyout_OpenIgnoredRuningFlyout;
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
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(GetValidWindows));
            //await GetValidWindows();
        }

        private void GetValidWindows(object s)
        {
            // Get valid running windows
            var Windows = SystemWindow.AllToplevelWindows.Where
                     (
                         w => w.Visible &&	// Must be a visible windows
                         w.Title != "" &&	// Must have a window title
                         IsProcessAccessible(w.Process) &&
                        System.IO.Path.GetDirectoryName(w.Process.ProcessName) != Process.GetCurrentProcess().ProcessName &&	// Must not be a GestureSign window
                         (w.ExtendedStyle & WindowExStyleFlags.TOOLWINDOW) != WindowExStyleFlags.TOOLWINDOW	// Must not be a tool window
                     );

            System.Threading.Thread.Sleep(550);
            foreach (SystemWindow sWind in Windows)
            {
                this.lstRunningApplications.Dispatcher.BeginInvoke(new System.Action(() =>
               {
                   ApplicationListViewItem lItem = new ApplicationListViewItem();

                   //    lItem.WindowClass = sWind.ClassName;
                   lItem.WindowTitle = sWind.Title;
                   lItem.WindowFilename = System.IO.Path.GetFileName(sWind.Process.MainModule.FileName);
                   //     lItem.ApplicationName = sWind.Process.MainModule.FileVersionInfo.FileDescription;
                   lItem.ApplicationIcon = Imaging.CreateBitmapSourceFromHIcon(sWind.Icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

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
