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

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _KeyCode.Clear();
            txtKey.Text = String.Empty;
            chkControl.IsChecked = chkAlt.IsChecked = chkShift.IsChecked = chkWin.IsChecked = false;
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
        }

        private void Grid_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            switch (e.Key)
            {
                case Key.System:
                    {
                        if (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt)
                            return;
                        break;
                    }
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LWin:
                case Key.RWin:
                    return;
            }

            var keyCode = (e.Key == Key.System) ? (Keys)KeyInterop.VirtualKeyFromKey(e.SystemKey) :
                (Keys)KeyInterop.VirtualKeyFromKey(e.Key);

            _KeyCode.Add(keyCode);
            string keyCodes = _KeyCode.Aggregate(string.Empty, (current, k) => current + (HotKeyPlugin.GetKeyName(k) + " + "));
            txtKey.Text = keyCodes.Substring(0, keyCodes.Length - 2);
        }

        private void ExtraKeysComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if ((!ExtraKeysComboBox.IsDropDownOpen || e.AddedItems.Count == 0)) return;

            _KeyCode.Add((Keys)ExtraKeysComboBox.SelectedValue);

            string keyCodes = _KeyCode.Aggregate(string.Empty, (current, k) => current + (HotKeyPlugin.GetKeyName(k) + " + "));
            txtKey.Text = keyCodes.Substring(0, keyCodes.Length - 2);

            ExtraKeysComboBox.IsDropDownOpen = false;
            ExtraKeysComboBox.SelectedIndex = 0;
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
                    KeyCode = _KeyCode,
                    SendByKeybdEvent = SendByKeybdEventCheckBox.IsChecked.Value
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
                SendByKeybdEventCheckBox.IsChecked = _Settings.SendByKeybdEvent;

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

                ExtraKeysComboBox.SelectedIndex = 0;
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
