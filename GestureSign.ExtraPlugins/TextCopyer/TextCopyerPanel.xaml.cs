using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestureSign.ExtraPlugins.TextCopyer
{
    /// <summary>
    /// Interaction logic for TextCopyerPanel.xaml
    /// </summary>
    public partial class TextCopyerPanel : UserControl
    {
        public TextCopyerPanel()
        {
            InitializeComponent();
        }

        public Point? Position
        {
            get
            {
                if (PositionComboBox.SelectedIndex == 0)
                    return null;

                int x, y;
                bool flag = int.TryParse(XTextBox.Text, out x);
                x = flag ? x : 0;
                flag = int.TryParse(YTextBox.Text, out y);
                y = flag ? y : 0;

                return new Point(x, y);
            }
            set
            {
                if (value != null)
                {
                    PositionComboBox.SelectedIndex = 1;
                    XTextBox.Text = value.Value.X.ToString();
                    YTextBox.Text = value.Value.Y.ToString();
                }
                else
                {
                    PositionComboBox.SelectedIndex = 0;
                    XTextBox.Text = YTextBox.Text = string.Empty;
                }
            }
        }

        private void Crosshair_CrosshairDragging(object sender, MouseEventArgs e)
        {
            Point p = Crosshair.PointToScreen(e.GetPosition(Crosshair));
            XTextBox.Text = p.X.ToString();
            YTextBox.Text = p.Y.ToString();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;

            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Subtract)
            {
                if (comboBox != null && (comboBox.Text.Contains("-") && e.Key == Key.Subtract))
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
            }
            else if (((e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.OemMinus) && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                if (comboBox != null && (comboBox.Text.Contains("-") && e.Key == Key.OemMinus))
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);

            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                int num = 0;
                if (comboBox == null || int.TryParse(comboBox.Text, out num))
                {
                    if (num < -9999)
                    {
                        if (comboBox != null) comboBox.Text = (-9999).ToString();
                    }
                    else if (num > 10000) if (comboBox != null) comboBox.Text = 10000.ToString();
                    return;
                }
                if ("-".Equals(comboBox.Text)) return;
                comboBox.Text = comboBox.Text.Remove(offset, change[0].AddedLength);
            }
        }

        private void PositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PositionCanvas != null)
                PositionCanvas.Visibility = PositionComboBox.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
