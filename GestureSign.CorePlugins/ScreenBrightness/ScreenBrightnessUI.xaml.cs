using System.Windows.Controls;
using System.Windows.Input;
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
                _Settings = new BrightnessSettings { Method = cboMethod.SelectedIndex };
                //Using simple calculation based off selected index instead of building
                //a wrapper or arrayList just to make it more readable
                int percent;
                bool flag = int.TryParse(NumPercent.Text, out percent);
                _Settings.Percent = flag ? percent : 0;

                return _Settings;
            }
            set
            {
                _Settings = value ?? new BrightnessSettings();

                cboMethod.SelectedIndex = _Settings.Method;
                //Using simple calculation based off selected index instead of building
                //a wrapper or arrayList just to make it more readable
                if (_Settings.Percent != 0)  //If no setting exists, don't try to derive selected index (results in -1, nothing selected)
                    NumPercent.Text = _Settings.Percent.ToString();
            }
        }

        public IHostControl HostControl
        {
            get { return _HostControl; }
            set { _HostControl = value; }
        }

        private void NumPercent_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox txt = sender as TextBox;

            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Decimal)
            {
                if (txt != null && (txt.Text.Contains(".") && e.Key == Key.Decimal))
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
            }
            else if (((e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.OemPeriod) && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                if (txt != null && (txt.Text.Contains(".") && e.Key == Key.OemPeriod))
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void NumPercent_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            var change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);

            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                int num = 0;
                if (textBox == null || int.TryParse(textBox.Text, out num))
                {
                    if (num < 1)
                    {
                        if (textBox != null) textBox.Text = 1.ToString();
                    }
                    else if (num > 100) if (textBox != null) textBox.Text = 100.ToString();
                    return;
                }
                textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                textBox.Select(offset, 0);
            }
        }


    }
}
