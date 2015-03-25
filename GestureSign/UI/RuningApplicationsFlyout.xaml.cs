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

using ManagedWinapi.Windows;

using MahApps.Metro.Controls;

namespace GestureSign.UI
{
    /// <summary>
    /// RuningApplicationsFlyout.xaml 的交互逻辑
    /// </summary>
    public partial class RuningApplicationsFlyout : Flyout, IDisposable
    {
        public static event EventHandler OpenIgnoredCustomFlyout;
        public static event EventHandler BindIgnoredApplications;
        MatchUsing _MatchUsing = MatchUsing.WindowClass;
        #region Public Instance Properties

        public string MatchString { get; set; }
        public MatchUsing MatchUsing
        {
            get { return _MatchUsing; }
            set
            {
                _MatchUsing = value;
            }
        }
        public ApplicationListViewItem SelectedApplication
        {
            get { return this.lstRunningApplications.SelectedItem as ApplicationListViewItem; }
        }

        private List<ApplicationListViewItem> ApplicationListViewItems = new List<ApplicationListViewItem>(5);

        #endregion
        public RuningApplicationsFlyout()
        {
            InitializeComponent();
            MatchString = null;
            MatchUsing = GestureSign.Common.Applications.MatchUsing.WindowClass;
            this.IsOpenChanged += RuningApplicationsFlyout_IsOpenChanged;
            IgnoredApplications.IgnoredRuningFlyout += IgnoredApplications_IgnoredRuningFlyout;
            CustomApplicationsFlyout.OpenIgnoredRuningFlyout += CustomApplicationsFlyout_OpenIgnoredRuningFlyout;
        }

        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

        void CustomApplicationsFlyout_OpenIgnoredRuningFlyout(object sender, EventArgs e)
        {
            this.IsOpen = true;
        }

        void IgnoredApplications_IgnoredRuningFlyout(object sender, ApplicationChangedEventArgs e)
        {
            this.IsOpen = !this.IsOpen;
        }

        void RuningApplicationsFlyout_IsOpenChanged(object sender, EventArgs e)
        {
            if (this.IsOpen)
            {
                RefreshApplications();
            }
        }

        private void SwitchToCustom_Click(object sender, RoutedEventArgs e)
        {
            this.IsOpen = false;
            OpenIgnoredCustomFlyout(this, new EventArgs());
        }

        private void btnAddRunning_Click(object sender, RoutedEventArgs e)
        {
            if (this.lstRunningApplications.SelectedItems.Count == 0)
                return;
            AddIgnoredApplication(MatchUsing.ExecutableFilename.ToString() + this.SelectedApplication.WindowFilename,
                this.SelectedApplication.WindowFilename, MatchUsing.ExecutableFilename, false);
            this.IsOpen = false;
        }

        private void AddIgnoredApplication(String Name, String MatchString, MatchUsing MatchUsing, bool IsRegEx)
        {
            if (ApplicationManager.Instance.ApplicationExists(Name))
                return;
            ApplicationManager.Instance.AddApplication(new IgnoredApplication(Name, MatchUsing, MatchString, IsRegEx, true));
            ApplicationManager.Instance.SaveApplications();
            BindIgnoredApplications(this, new EventArgs());
        }




        #region Public Instance Methods

        public void RefreshApplications()
        {
            this.lstRunningApplications.Items.Clear();
            //    this.lstRunningApplications.ItemsSource = await GetValidWindows();
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(GetValidWindows));
            //await GetValidWindows();
        }


        #endregion


        #region Private Instance Methods



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
