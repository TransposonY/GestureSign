using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace GestureSign.CorePlugins.SendMessage
{
    /// <summary>
    /// SendMessageView.xaml 的交互逻辑
    /// </summary>
    public partial class SendMessageView : UserControl
    {
        public SendMessageView()
        {
            InitializeComponent();
        }

        internal SendMessageSetting Settings
        {
            get
            {
                uint message;
                if (MessageComboBox.SelectedValue != null)
                {
                    message = (uint)MessageComboBox.SelectedValue;
                }
                else
                {
                    uint.TryParse(MessageComboBox.Text, out message);
                }
                int wParam, lParam;
                int.TryParse(WParamTextBox.Text, out wParam);
                int.TryParse(LParamTextBox.Text, out lParam);
                return new SendMessageSetting
                {
                    ClassName = ClassNameTextBox.Text,
                    Title = TitleTextBox.Text,
                    HotKey =HotKeyRadioButton.IsChecked.Value? HotKeyControl.Settings:null,
                    IsRegEx = IsRegExCheckBox.IsChecked.Value,
                    IsSendMessage = SendMessageRadioButton.IsChecked.Value,
                    IsSpecificWindow = SpecificWindowRadioButton.IsChecked.Value,
                    Message = message,
                    WParam = new IntPtr(wParam),
                    LParam = new IntPtr(lParam)
                };
            }
            set
            {
                SendMessageSetting setting = value ?? new SendMessageSetting();

                ClassNameTextBox.Text = setting.ClassName;
                TitleTextBox.Text = setting.Title;

                HotKeyControl.Settings = setting.HotKey;

                IsRegExCheckBox.IsChecked = setting.IsRegEx;
                SendMessageRadioButton.IsChecked = setting.IsSendMessage;
                SpecificWindowRadioButton.IsChecked = setting.IsSpecificWindow;
                CustomMessageRadioButton.IsChecked = setting.HotKey == null;

                MessageComboBox.Text = setting.Message.ToString();
                WParamTextBox.Text = setting.WParam.ToInt32().ToString();
                LParamTextBox.Text = setting.LParam.ToInt32().ToString();
            }
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class Bool2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
