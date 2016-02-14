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

namespace GestureSign.CorePlugins.SendKeystrokes
{
    /// <summary>
    /// SendKeystrokesControl.xaml 的交互逻辑
    /// </summary>
    public partial class SendKeystrokesControl : UserControl
    {
        public SendKeystrokesControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            // Escape existing text in textbox
            TxtSendKeys.Text = ManagedWinapi.SendKeysEscaper.Instance.escape(TxtSendKeys.Text, false);
        }
    }
}
