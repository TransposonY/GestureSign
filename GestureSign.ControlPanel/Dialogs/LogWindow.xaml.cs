using MahApps.Metro.Controls;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : MetroWindow
    {
        public LogWindow(string log)
        {
            InitializeComponent();
            LogTextBox.Text = log;
        }
    }
}
