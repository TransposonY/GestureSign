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

            int[] items = new[] { 2, 4, 6, 8, 10, 20, 30, 50, 80, 100 };
            foreach (var i in items)
            {
                NumPercent.Items.Add(i);
            }
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
            ComboBox comboBox = sender as ComboBox;

            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Decimal)
            {
                if (comboBox != null && (comboBox.Text.Contains(".") && e.Key == Key.Decimal))
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
            }
            else if (((e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.OemPeriod) && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                if (comboBox != null && (comboBox.Text.Contains(".") && e.Key == Key.OemPeriod))
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
            ComboBox comboBox = sender as ComboBox;

            if (comboBox != null)
            {
                int num;
                if (int.TryParse(comboBox.Text, out num))
                {
                    if (num < 1)
                        comboBox.Text = 1.ToString();
                    else if (num > 100)
                        comboBox.Text = 100.ToString();
                }
            }
        }


    }
}
