using GestureSign.Common.Localization;
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
                flag = int.TryParse(ScrollAmountComboBox.Text, out scrollAmount);
                scrollAmount = flag ? NegativeRadioButton.IsChecked.Value ? -scrollAmount : scrollAmount : 0;

                MouseActions action = MouseActions.None;
                if (ActionComboBox.SelectedIndex < 4)
                {
                    action = (MouseActions)(MouseButtonComboBox.SelectedValue ?? 0) | (MouseActions)(ActionComboBox.SelectedValue ?? 0);
                }
                else
                    action = (MouseActions)(ActionComboBox.SelectedValue ?? 0);

                _settings = new MouseActionsSettings
                {
                    ActionLocation = action == MouseActions.MoveMouseBy ? (ClickPositions)RelativePositionComboBox.SelectedValue : (ClickPositions)PositionComboBox.SelectedValue,
                    MouseAction = action,
                    MovePoint = new System.Drawing.Point(x, y),
                    ScrollAmount = scrollAmount
                };
                return _settings;
            }
            set
            {
                _settings = value ?? new MouseActionsSettings();

                var action = _settings.MouseAction.GetActions();
                var button = _settings.MouseAction.GetButtons();
                ActionComboBox.SelectedValue = action != 0 ? action : MouseActions.Click;
                MouseButtonComboBox.SelectedValue = button != 0 ? button : MouseActions.LeftButton;

                PositionComboBox.SelectedValue = _settings.ActionLocation != 0 && Enum.IsDefined(typeof(ClickPositions), _settings.ActionLocation) ? _settings.ActionLocation : ClickPositions.Custom;
                RelativePositionComboBox.SelectedValue = _settings.MouseAction == MouseActions.MoveMouseBy ? _settings.ActionLocation : ClickPositions.Current;

                XTextBox.Text = _settings.MovePoint.X.ToString();
                YTextBox.Text = _settings.MovePoint.Y.ToString();
                ScrollAmountComboBox.Text = Math.Abs(_settings.ScrollAmount).ToString();
                PositiveRadioButton.IsChecked = _settings.ScrollAmount >= 0;
                NegativeRadioButton.IsChecked = _settings.ScrollAmount < 0;
            }
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

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            switch (((KeyValuePair<MouseActions, string>)e.AddedItems[0]).Key)
            {
                case MouseActions.HorizontalScroll:
                    PositiveRadioButton.Content = LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Right");
                    NegativeRadioButton.Content = LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Left");
                    ScrollPanel.Visibility = Visibility.Visible;
                    ButtonPanel.Visibility = ReferencePositionText.Visibility = MoveMouseCanvas.Visibility = Visibility.Collapsed;
                    break;
                case MouseActions.VerticalScroll:
                    PositiveRadioButton.Content = LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Up");
                    NegativeRadioButton.Content = LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Down");
                    ScrollPanel.Visibility = Visibility.Visible;
                    ButtonPanel.Visibility = ReferencePositionText.Visibility = MoveMouseCanvas.Visibility = Visibility.Collapsed;
                    break;
                case MouseActions.MoveMouseBy:
                    ReferencePositionText.Visibility = MoveMouseCanvas.Visibility = Visibility.Visible;
                    ButtonPanel.Visibility = ScrollPanel.Visibility = Visibility.Collapsed;
                    break;
                case MouseActions.MoveMouseTo:
                    MoveMouseCanvas.Visibility = Visibility.Visible;
                    ButtonPanel.Visibility = ReferencePositionText.Visibility = ScrollPanel.Visibility = Visibility.Collapsed;
                    break;
                default:
                    MoveMouseCanvas.Visibility = PositionComboBox.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
                    ButtonPanel.Visibility = Visibility.Visible;
                    ReferencePositionText.Visibility = ScrollPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void PositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || ActionComboBox.SelectedIndex >= 4) return;
            if (((KeyValuePair<ClickPositions, string>)e.AddedItems[0]).Key == ClickPositions.Custom)
            {
                MoveMouseCanvas.Visibility = Visibility.Visible;
            }
            else
            {
                MoveMouseCanvas.Visibility = Visibility.Collapsed;
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
