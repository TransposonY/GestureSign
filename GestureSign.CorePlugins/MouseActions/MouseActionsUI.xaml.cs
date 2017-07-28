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

                string action;
                if (ActionComboBox.SelectedIndex < 4)
                {
                    action = $"{MouseButtonComboBox.SelectedValue}{ActionComboBox.SelectedValue}";
                }
                else
                    action = (string)ActionComboBox.SelectedValue;

                _settings = new MouseActionsSettings
                {
                    ClickPosition = (ClickPositions)PositionComboBox.SelectedValue,
                    MouseAction = (MouseActions)Enum.Parse(typeof(MouseActions), action),
                    MovePoint = new System.Drawing.Point(x, y),
                    ScrollAmount = scrollAmount
                };
                return _settings;
            }
            set
            {
                _settings = value ?? new MouseActionsSettings();

                var action = _settings.MouseAction.ToString();
                if (action.Contains("Button"))
                {
                    ActionComboBox.SelectedValue = action.Split(MouseActionDescription.ButtonDescription.Keys.ToArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                    MouseButtonComboBox.SelectedValue = action.Split(MouseActionDescription.DescriptionDict.Keys.ToArray(), StringSplitOptions.RemoveEmptyEntries)[0];
                }
                else
                    ActionComboBox.SelectedValue = action;

                PositionComboBox.SelectedValue = _settings.ClickPosition;
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
            switch (((KeyValuePair<string, string>)e.AddedItems[0]).Key)
            {
                case "HorizontalScroll":
                    PositiveRadioButton.Content = LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Right");
                    NegativeRadioButton.Content = LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Left");
                    ScrollPanel.Visibility = Visibility.Visible;
                    ButtonPanel.Visibility = ClickPositionText.Visibility = ReferencePositionText.Visibility = PositionComboBox.Visibility = MoveMouseCanvas.Visibility = Visibility.Collapsed;
                    break;
                case "VerticalScroll":
                    PositiveRadioButton.Content = LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Up");
                    NegativeRadioButton.Content = LocalizationProvider.Instance.GetTextValue("CorePlugins.MouseActions.Description.Down");
                    ScrollPanel.Visibility = Visibility.Visible;
                    ButtonPanel.Visibility = ClickPositionText.Visibility = ReferencePositionText.Visibility = PositionComboBox.Visibility = MoveMouseCanvas.Visibility = Visibility.Collapsed;
                    break;
                case "MoveMouseBy":
                    ReferencePositionText.Visibility = MoveMouseCanvas.Visibility = PositionComboBox.Visibility = Visibility.Visible;
                    ButtonPanel.Visibility = ClickPositionText.Visibility = ScrollPanel.Visibility = Visibility.Collapsed;
                    break;
                case "MoveMouseTo":
                    MoveMouseCanvas.Visibility = Visibility.Visible;
                    ButtonPanel.Visibility = ClickPositionText.Visibility = ReferencePositionText.Visibility = PositionComboBox.Visibility = ScrollPanel.Visibility = Visibility.Collapsed;
                    break;
                default:
                    ButtonPanel.Visibility = ClickPositionText.Visibility = PositionComboBox.Visibility = Visibility.Visible;
                    ReferencePositionText.Visibility = MoveMouseCanvas.Visibility = ScrollPanel.Visibility = Visibility.Collapsed;
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
