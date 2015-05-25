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

namespace GestureSign.CorePlugins.ActivateWindow
{
    /// <summary>
    /// ActivateWindowUI.xaml 的交互逻辑
    /// </summary>
    public partial class ActivateWindowUI : UserControl
    {
        public ActivateWindowUI()
        {
            InitializeComponent();
        }

        public ActivateWindowSettings Settings
        {
            get
            {
                int timeout = 0;
                int.TryParse(TimeoutTextBox.Text, out timeout);
                return new ActivateWindowSettings()
                {
                    ClassName = ClassNameTextBox.Text.Trim(),
                    Caption = CaptionTextBox.Text.Trim(),
                    IsRegEx = IsRegExCheckBox.IsChecked.Value,
                    Timeout = timeout
                };
            }
            set
            {
                ClassNameTextBox.Text = value.ClassName;
                CaptionTextBox.Text = value.Caption;
                IsRegExCheckBox.IsChecked = value.IsRegEx;
                TimeoutTextBox.Text = value.Timeout.ToString();
            }
        }

        private void TimeoutTextBox_KeyDown(object sender, KeyEventArgs e)
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

        private void TimeoutTextBox_TextChanged(object sender, TextChangedEventArgs e)
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
                    if (num < 0)
                    {
                        if (textBox != null) textBox.Text = 0.ToString();
                    }
                    return;
                }
                textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                textBox.Select(offset, 0);
            }
        }
    }
}
