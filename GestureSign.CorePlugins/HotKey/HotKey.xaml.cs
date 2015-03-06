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

using System.Xaml;
using GestureSign.Common.Plugins;

namespace GestureSign.CorePlugins.HotKey
{
    /// <summary>
    /// HotKey.xaml 的交互逻辑
    /// </summary>
    public partial class HotKey : UserControl
    {
        #region Private Variables

        HotKeySettings _Settings = null;
        System.Windows.Forms.Keys _KeyCode;
        IHostControl _HostControl = null;

        #endregion
        public HotKey()
        {
            InitializeComponent();
        }



        private void txtKey_GotFocus(object sender, RoutedEventArgs e)
        {
            _HostControl.AllowEscapeKey = false;

        }

        private void txtKey_LostFocus(object sender, RoutedEventArgs e)
        {
            _HostControl.AllowEscapeKey = true;
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                chkControl.IsChecked = true;
            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                chkAlt.IsChecked = true;
            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                chkShift.IsChecked = true;
            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
                chkWin.IsChecked = true;
            _KeyCode = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key);
            txtKey.Text = new ManagedWinapi.KeyboardKey(_KeyCode).KeyName;
        }



        #region Public Properties

        public HotKeySettings Settings
        {
            get
            {
                _Settings = new HotKeySettings();
                _Settings.Windows = chkWin.IsChecked.Value;
                _Settings.Control = chkControl.IsChecked.Value;
                _Settings.Shift = chkShift.IsChecked.Value;
                _Settings.Alt = chkAlt.IsChecked.Value;
                _Settings.KeyCode = _KeyCode;

                return _Settings;
            }
            set
            {
                _Settings = value;

                if (_Settings == null)
                    _Settings = new HotKeySettings();

                chkWin.IsChecked = _Settings.Windows;
                chkControl.IsChecked = _Settings.Control;
                chkShift.IsChecked = _Settings.Shift;
                chkAlt.IsChecked = _Settings.Alt;
                _KeyCode = _Settings.KeyCode;
                txtKey.Text = _Settings.KeyCode.ToString();
            }
        }

        public IHostControl HostControl
        {
            get { return _HostControl; }
            set
            {
                _HostControl = value;
                this.txtKey.Focus();
            }
        }

        #endregion


    }
}
