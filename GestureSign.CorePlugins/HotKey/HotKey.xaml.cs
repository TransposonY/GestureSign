using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using GestureSign.Common.Plugins;
using ManagedWinapi;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace GestureSign.CorePlugins.HotKey
{
    /// <summary>
    /// HotKey.xaml 的交互逻辑
    /// </summary>
    public partial class HotKey : UserControl
    {
        #region Private Variables

        HotKeySettings _Settings = null;
        List<Keys> _KeyCode = new List<Keys>(2);
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


        private void Canvas_PreviewKeyDown(object sender, KeyEventArgs e)
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
            var keyCode = (e.Key == Key.System) ? (Keys)KeyInterop.VirtualKeyFromKey(e.SystemKey) :
                (Keys)KeyInterop.VirtualKeyFromKey(e.Key);

            _KeyCode.Add(keyCode);
            string keyCodes = _KeyCode.Aggregate(string.Empty, (current, k) => current + (HotKeyPlugin.GetKeyName(k) + " + "));
            txtKey.Text = keyCodes.Substring(0, keyCodes.Length - 2);

        }

        private void Canvas_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Key == Key.PrintScreen)
            {
                _KeyCode.Add((Keys)KeyInterop.VirtualKeyFromKey(Key.PrintScreen));
                string keyCodes = _KeyCode.Aggregate(string.Empty, (current, k) => current + (HotKeyPlugin.GetKeyName(k) + " + "));
                txtKey.Text = keyCodes.Substring(0, keyCodes.Length - 2);
            }
        }



        #region Public Properties

        public HotKeySettings Settings
        {
            get
            {
                _Settings = new HotKeySettings
                {
                    Windows = chkWin.IsChecked.Value,
                    Control = chkControl.IsChecked.Value,
                    Shift = chkShift.IsChecked.Value,
                    Alt = chkAlt.IsChecked.Value,
                    KeyCode = _KeyCode
                };

                return _Settings;
            }
            set
            {
                _Settings = value ?? new HotKeySettings();

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
                    string keyCodes = _KeyCode.Aggregate(string.Empty, (current, k) => current + (HotKeyPlugin.GetKeyName(k) + " + "));
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
                txtKey.Focus();
            }
        }

        #endregion



    }
}
