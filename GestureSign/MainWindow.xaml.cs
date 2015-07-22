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
using GestureSign.Common;
using GestureSign.Common.Input;
using GestureSign.Common.Localization;

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

        private void SetAboutInfo()
        {
            string version = LocalizationProvider.Instance.GetTextValue("About.Version") +
                             System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location)
                                 .FileVersion;
            string releaseDate = LocalizationProvider.Instance.GetTextValue("About.ReleaseDate") +
                                 new DateTime(2000, 1, 1).AddDays(Application.ResourceAssembly.GetName().Version.Build)
                                     .AddSeconds(Application.ResourceAssembly.GetName().Version.Revision*2);
            this.AboutTextBox.Text = this.AboutTextBox.Text.Insert(0, version + "\r\n"+ releaseDate + "\r\n");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(LocalizationProvider.Instance.GetTextValue("About.HelpPageUrl"));
        }
    }
}
