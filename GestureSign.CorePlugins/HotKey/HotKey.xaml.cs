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
        List<System.Windows.Forms.Keys> _KeyCode = new List<System.Windows.Forms.Keys>(2);
        IHostControl _HostControl = null;

        #endregion
        public HotKey()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _KeyCode.Clear();
            txtKey.Text = String.Empty;
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
            var keyCode = (e.Key == Key.System) ? (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.SystemKey) :
                (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(e.Key);
            if (_KeyCode.Contains(keyCode)) return;
            else
            {
                _KeyCode.Add(keyCode);
                string keyCodes = string.Empty;
                foreach (var k in _KeyCode)
                    keyCodes += new ManagedWinapi.KeyboardKey(k).KeyName + " + ";
                txtKey.Text = keyCodes.Substring(0, keyCodes.Length - 2);
            }
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

                if (_Settings.KeyCode != null)
                    _KeyCode = _Settings.KeyCode;
                else _KeyCode.Clear();
                //txtKey.Text = _Settings.KeyCode.ToString();
                if (_KeyCode.Count > 0)
                {
                    string keyCodes = string.Empty;
                    foreach (var k in _KeyCode)
                        keyCodes += new ManagedWinapi.KeyboardKey(k).KeyName + " + ";
                    txtKey.Text = keyCodes.Substring(0, keyCodes.Length - 2);
                }
                else txtKey.Text = "";
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
