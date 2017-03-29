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

namespace GestureSign.CorePlugins.MouseActions
{
    /// <summary>
    /// MouseClickUI.xaml 的交互逻辑
    /// </summary>
    public partial class MouseActionsUI : UserControl
    {
        #region Private Variables

        MouseActionsSettings _settings;

        #endregion
        public MouseActionsUI()
        {
            InitializeComponent();
        }

        public MouseActionsSettings Settings
        {
            get
            {
                int x, y, scrollAmount;

                bool flag = int.TryParse(XTextBox.Text, out x);
                x = flag ? x : 0;
                flag = int.TryParse(YTextBox.Text, out y);
                y = flag ? y : 0;
                flag = int.TryParse(ScrollAmountTextBox.Text, out scrollAmount);
                scrollAmount = flag ? scrollAmount : 0;

                _settings = new MouseActionsSettings
                {
                    ClickPosition = (ClickPositions)PositionComboBox.SelectedValue,
                    MouseAction = (MouseActions)ActionComboBox.SelectedValue,
                    MovePoint = new System.Drawing.Point(x, y),
                    ScrollAmount = scrollAmount
                };
                return _settings;
            }
            set
            {
                _settings = value ?? new MouseActionsSettings();
                ActionComboBox.SelectedValue = _settings.MouseAction;
                PositionComboBox.SelectedValue = _settings.ClickPosition;
                XTextBox.Text = _settings.MovePoint.X.ToString();
                YTextBox.Text = _settings.MovePoint.Y.ToString();
                ScrollAmountTextBox.Text = _settings.ScrollAmount.ToString();
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox txt = sender as TextBox;

            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Subtract)
            {
                if (txt != null && (txt.Text.Contains("-") && e.Key == Key.Subtract))
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = false;
            }
            else if (((e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.OemMinus) && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                if (txt != null && (txt.Text.Contains("-") && e.Key == Key.OemMinus))
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
            TextBox textBox = sender as TextBox;
            var change = new TextChange[e.Changes.Count];
            e.Changes.CopyTo(change, 0);

            int offset = change[0].Offset;
            if (change[0].AddedLength > 0)
            {
                int num = 0;
                if (textBox == null || int.TryParse(textBox.Text, out num))
                {
                    if (num < -9999)
                    {
                        if (textBox != null) textBox.Text = (-9999).ToString();
                    }
                    else if (num > 10000) if (textBox != null) textBox.Text = 10000.ToString();
                    return;
                }
                if ("-".Equals(textBox.Text)) return;
                textBox.Text = textBox.Text.Remove(offset, change[0].AddedLength);
                textBox.Select(offset, 0);
            }
        }

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            var dict = (KeyValuePair<MouseActions, String>)e.AddedItems[0];
            switch (dict.Key)
            {
                case MouseActions.HorizontalScroll:
                case MouseActions.VerticalScroll:
                    ScrollCanvas.Visibility = Visibility.Visible;
                    ClickPositionText.Visibility = ReferencePositionText.Visibility = PositionComboBox.Visibility = MoveMouseCanvas.Visibility = Visibility.Collapsed;
                    break;
                case MouseActions.MoveMouseBy:
                    ReferencePositionText.Visibility = MoveMouseCanvas.Visibility = PositionComboBox.Visibility = Visibility.Visible;
                    ClickPositionText.Visibility = ScrollCanvas.Visibility = Visibility.Collapsed;
                    break;
                case MouseActions.MoveMouseTo:
                    MoveMouseCanvas.Visibility = Visibility.Visible;
                    ClickPositionText.Visibility = ReferencePositionText.Visibility = PositionComboBox.Visibility = ScrollCanvas.Visibility = Visibility.Collapsed;
                    break;
                default:
                    ClickPositionText.Visibility = PositionComboBox.Visibility = Visibility.Visible;
                    ReferencePositionText.Visibility = MoveMouseCanvas.Visibility = ScrollCanvas.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void Crosshair_CrosshairDragging(object sender, MouseEventArgs e)
        {
            Point p = Crosshair.PointToScreen(e.GetPosition(Crosshair));
            XTextBox.Text = p.X.ToString();
            YTextBox.Text = p.Y.ToString();
        }
    }

}
