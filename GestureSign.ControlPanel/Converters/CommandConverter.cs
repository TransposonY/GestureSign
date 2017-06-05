using GestureSign.Common.Applications;
using GestureSign.ControlPanel.ViewModel;
using System;
using System.Globalization;
using System.Windows.Data;

namespace GestureSign.ControlPanel.Converters
{
    [ValueConversion(typeof(ICommand), typeof(CommandInfo))]
    public class CommandConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var command = (ICommand)value;
            if (command == null) return null;
            return CommandInfo.FromCommand(command, null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}