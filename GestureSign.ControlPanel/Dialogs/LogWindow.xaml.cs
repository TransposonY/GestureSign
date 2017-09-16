using MahApps.Metro.Controls;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : MetroWindow
    {
        public string Message { get { return MessageTextBox.Text; } }

        public LogWindow(string log)
        {
            InitializeComponent();
            LogTextBox.Text = log;
        }

        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!DialogResult.GetValueOrDefault())
                DialogResult = true;
            Close();
        }
    }
}
