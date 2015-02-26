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

using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins.Volume
{
    /// <summary>
    /// Volume.xaml 的交互逻辑
    /// </summary>
    public partial class Volume : UserControl
    {
        #region Private Variables

        VolumeSettings _Settings = null;
        //Keys _KeyCode;
        IHostControl _HostControl = null;

        #endregion
        public Volume()
        {
            InitializeComponent();
            for (int i = 1; i < 11; i++)
                this.cboPercent.Items.Add(i * 10);
        }

        public VolumeSettings Settings
        {
            get
            {
                _Settings = new VolumeSettings();
                _Settings.Method = cboMethod.SelectedIndex;
                //Using simple calculation based off selected index instead of building
                //a wrapper or arrayList just to make it more readable
                _Settings.Percent = ((cboPercent.SelectedIndex + 1) * 10);

                return _Settings;
            }
            set
            {
                _Settings = value;

                if (_Settings == null)
                    _Settings = new VolumeSettings();

                cboMethod.SelectedIndex = _Settings.Method;
                //Using simple calculation based off selected index instead of building
                //a wrapper or arrayList just to make it more readable
                if (_Settings.Percent != 0)  //If no setting exists, don't try to derive selected index (results in -1, nothing selected)
                    cboPercent.SelectedIndex = ((_Settings.Percent / 10) - 1);
            }
        }

        public IHostControl HostControl
        {
            get { return _HostControl; }
            set { _HostControl = value; }
        }

        private void cboMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).SelectedIndex == 2)
            {   //2 = Toggle mute, set cboPercent to nothing selected and disable it
                cboPercent.SelectedIndex = -1;
                cboPercent.IsEnabled = false;
            }
            else
            {   //If volume adjustment, set cboPercent to show first item (10%) if nothing is selected
                // and enable percent dropdown
                if (cboPercent.SelectedIndex == -1)
                    cboPercent.SelectedIndex = 0;
                cboPercent.IsEnabled = true;
            }
        }
    }
}
