using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace GestureSign.CorePlugins.KeyDownKeyUp
{
    /// <summary>
    /// KeyDownKeyUpUI.xaml 的交互逻辑
    /// </summary>
    public partial class KeyDownKeyUpUI : UserControl
    {
        #region Private Variables

        KeyDownKeyUpSettings _settings = null;
        List<Keys> _keyCode = new List<Keys>(2);

        #endregion


        public KeyDownKeyUpUI()
        {
            InitializeComponent();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _keyCode.Clear();
            KeysTextBox.Text = String.Empty;
        }

        private void KeysTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void KeysTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            var keyCode = (e.Key == Key.System) ? (Keys)KeyInterop.VirtualKeyFromKey(e.SystemKey) :
                (Keys)KeyInterop.VirtualKeyFromKey(e.Key);

            _keyCode.Add(keyCode);
            string keyCodes = _keyCode.Aggregate(string.Empty, (current, k) => current + (KeyDownKeyUpPlugin.GetKeyName(k) + " + "));
            KeysTextBox.Text = keyCodes.Substring(0, keyCodes.Length - 2);
        }

        public KeyDownKeyUpSettings Settings
        {
            get
            {
                _settings = new KeyDownKeyUpSettings
                {
                    IsKeyDown = KeyDownRadioButton.IsChecked != null && KeyDownRadioButton.IsChecked.Value,
                    KeyCode = _keyCode
                };
                return _settings;
            }
            set
            {
                _settings = value ?? new KeyDownKeyUpSettings();

                if (_settings.IsKeyDown) KeyDownRadioButton.IsChecked = true;
                else KeyUpRadioButton.IsChecked = true;

                if (_settings.KeyCode != null)
                    _keyCode = _settings.KeyCode;
                else _keyCode.Clear();
                if (_keyCode.Count > 0)
                {
                    string keyCodes = _keyCode.Aggregate(string.Empty, (current, k) => current + (KeyDownKeyUpPlugin.GetKeyName(k) + " + "));
                    KeysTextBox.Text = keyCodes.Substring(0, keyCodes.Length - 2);
                }
                else KeysTextBox.Text = "";
            }
        }
    }
}
