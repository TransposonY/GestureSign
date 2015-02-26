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

namespace GestureSign.CorePlugins.ScreenBrightness
{
    /// <summary>
    /// ScreenBrightnessUI.xaml 的交互逻辑
    /// </summary>
    public partial class ScreenBrightnessUI : UserControl
    { 
        #region Private Variables

        BrightnessSettings _Settings = null;
        //Keys _KeyCode;
        IHostControl _HostControl = null;

        #endregion
        public ScreenBrightnessUI()
        {
            InitializeComponent();
        }
        public BrightnessSettings Settings
        {
            get
            {
                _Settings = new BrightnessSettings();
                _Settings.Method = cboMethod.SelectedIndex;
                //Using simple calculation based off selected index instead of building
                //a wrapper or arrayList just to make it more readable
                _Settings.Percent =(int)numPercent.Value.Value;

                return _Settings;
            }
            set
            {
                _Settings = value;

                if (_Settings == null)
                    _Settings = new BrightnessSettings();

                cboMethod.SelectedIndex = _Settings.Method;
                //Using simple calculation based off selected index instead of building
                //a wrapper or arrayList just to make it more readable
                if (_Settings.Percent != 0)  //If no setting exists, don't try to derive selected index (results in -1, nothing selected)
                    numPercent.Value = _Settings.Percent ;
            }
        }

        public IHostControl HostControl
        {
            get { return _HostControl; }
            set { _HostControl = value; }
        }

     
    }
}
