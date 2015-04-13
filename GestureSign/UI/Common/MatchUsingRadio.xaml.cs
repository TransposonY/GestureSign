using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GestureSign.Common.Applications;

namespace GestureSign.UI
{
    /// <summary>
    /// MatchUsingRadio.xaml 的交互逻辑
    /// </summary>
    public partial class MatchUsingRadio : UserControl
    {
        public MatchUsingRadio()
        {
            InitializeComponent();

            MultiBinding multiBinding = new MultiBinding
            {
                Converter = new MatchUsingConverter(),
                Mode = BindingMode.TwoWay
            };
            multiBinding.Bindings.Add(new Binding("IsChecked") { ElementName = "FileNameRadio" });
            multiBinding.Bindings.Add(new Binding("IsChecked") { ElementName = "TitleRadio" });
            multiBinding.Bindings.Add(new Binding("IsChecked") { ElementName = "ClassRadio" });

            SetBinding(MatchUsingProperty, multiBinding);
        }
        public MatchUsing MatchUsing
        {
            get { return (MatchUsing)GetValue(MatchUsingProperty); }
            set { SetValue(MatchUsingProperty, value); }
        }
        public static readonly DependencyProperty MatchUsingProperty =
            DependencyProperty.Register("MatchUsing", typeof(MatchUsing), typeof(MatchUsingRadio), new FrameworkPropertyMetadata(MatchUsing.ExecutableFilename));

        public class MatchUsingConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                return (bool)values[0] ?
                 MatchUsing.ExecutableFilename : (bool)values[1] ?
                   MatchUsing.WindowTitle : MatchUsing.WindowClass;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                if ((MatchUsing)value == MatchUsing.ExecutableFilename)
                {
                    return new object[3] { true, false, false };
                }
                if ((MatchUsing)value == MatchUsing.WindowTitle)
                    return new object[3] { false, true, false };
                return new object[3] { false, false, true };
            }
        }
    }
}
