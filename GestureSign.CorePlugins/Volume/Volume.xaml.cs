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

            int[] items = new[] { 2, 4, 6, 8, 10, 20, 30, 50, 80, 100 };
            foreach (var i in items)
            {
                this.PercentComboBox.Items.Add(i);
            }
        }

        public VolumeSettings Settings
        {
            get
            {
                int percent;
                bool flag = int.TryParse(PercentComboBox.Text, out percent);
                _Settings.Percent = flag ? percent : 0;

                _Settings = new VolumeSettings
                {
                    Method = cboMethod.SelectedIndex,
                    Percent = percent
                };
                //Using simple calculation based off selected index instead of building
                //a wrapper or arrayList just to make it more readable

                return _Settings;
            }
            set
            {
                _Settings = value ?? new VolumeSettings();

                cboMethod.SelectedIndex = _Settings.Method;
                //Using simple calculation based off selected index instead of building
                //a wrapper or arrayList just to make it more readable
                if (_Settings.Percent != 0)  //If no setting exists, don't try to derive selected index (results in -1, nothing selected)
                    PercentComboBox.Text = _Settings.Percent.ToString();
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
                PercentComboBox.SelectedIndex = -1;
                PercentComboBox.IsEnabled = false;
            }
            else
            {   //If volume adjustment, set cboPercent to show first item (10%) if nothing is selected
                // and enable percent dropdown
                if (PercentComboBox.SelectedIndex == -1)
                    PercentComboBox.SelectedIndex = 0;
                PercentComboBox.IsEnabled = true;
            }
        }

        private void PercentComboBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox != null)
            {
                int num;
                if (int.TryParse(comboBox.Text, out num))
                {
                    if (num < 2)
                        comboBox.Text = 2.ToString();
                    else if (num > 100)
                        comboBox.Text = 100.ToString();
                }
            }
        }

        private void PercentComboBox_OnKeyDown(object sender, KeyEventArgs e)
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
    }
}
