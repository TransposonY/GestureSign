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

using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using GestureSign.Common.Plugins;
using GestureSign.Common.Gestures;
using GestureSign.Common.Applications;
using GestureSign.Common.Drawing;
using GestureSign.Common;
using GestureSign.Common.Input;

namespace GestureSign
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            SetAboutInfo();
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            this.availableAction.Dispose();
            this.availableGestures.Dispose();
            runingAppFlyout.Dispose();
            CustomAppFlyout.Dispose();
            ignoredApplications.Dispose();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }


        private void SetAboutInfo()
        {
            string version = "Version:   " + System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location).FileVersion;
            string releaseDate = "\r\nReleaseDate:   " +
                new DateTime(2000, 1, 1).AddDays(Application.ResourceAssembly.GetName().Version.Build).AddSeconds(Application.ResourceAssembly.GetName().Version.Revision * 2).ToString();
            this.AboutTextBox.Text = this.AboutTextBox.Text.Insert(0, version + releaseDate + "\r\n");
        }
    }
}
