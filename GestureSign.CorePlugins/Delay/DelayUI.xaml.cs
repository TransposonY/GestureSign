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

namespace GestureSign.CorePlugins.Delay
{
    /// <summary>
    /// DelayUI.xaml 的交互逻辑
    /// </summary>
    public partial class DelayUI : UserControl
    {
        private int _timeout;
        public DelayUI()
        {
            InitializeComponent();
        }

        public int Timeout
        {
            get
            {
                int timeout;
                bool flag = int.TryParse(DelayComboBox.Text, out timeout);
                _timeout = flag ? timeout : 0;
                return _timeout;
            }
            set
            {
                _timeout = value;
                DelayComboBox.Text = _timeout.ToString();
            }
        }
        private void DelayComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            ComboBox txt = sender as ComboBox;

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

        private void DelayComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ComboBox textBox = sender as ComboBox;
            var change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);

            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                int num = 0;
                if (textBox == null || int.TryParse(textBox.Text, out num))
                {
                    if (num < 0)
                    {
                        if (textBox != null) textBox.Text = 0.ToString();
                    }
                    //else if (num > 10000) if (textBox != null) textBox.Text = 10000.ToString();
                    return;
                }
                //textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
            }
        }
    }
}
