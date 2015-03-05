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

namespace GestureSign.CorePlugins.RunCommand
{
    /// <summary>
    /// RunCommand.xaml 的交互逻辑
    /// </summary>
    public partial class RunCommand : UserControl
    {
        #region Private Variables

        private RunCommandSettings _Settings = null;

        #endregion
        public RunCommand()
        {
            InitializeComponent();
        }

        public RunCommandSettings Settings
        {
            get
            {
                _Settings = new RunCommandSettings();
                _Settings.Command = txtCommand.Text.Trim();
                _Settings.ShowCmd = this.showCmdCheckBox.IsChecked.Value;

                return _Settings;
            }
            set
            {
                _Settings = value;
                txtCommand.Text = _Settings.Command;
                this.showCmdCheckBox.IsChecked = _Settings.ShowCmd;
            }
        }

        private void cmdSelectFile_Click(object sender, RoutedEventArgs e)
        {
            // Create open file dialog to let user select file to open
            Microsoft.Win32.OpenFileDialog ofDialog = new Microsoft.Win32.OpenFileDialog();
            try
            {
                // Set initial directory if path is valid
                if (System.IO.File.Exists(txtCommand.Text.Trim()))
                    ofDialog.InitialDirectory = System.IO.Path.GetDirectoryName(txtCommand.Text.Trim());
            }
            catch { }
            if (ofDialog.ShowDialog().Value)
                txtCommand.Text = "\"" + ofDialog.FileName + "\"";

        }

        //private bool IsValidPath(string path)
        //{
        //    return !System.IO.Path.GetInvalidPathChars().Any(fnc => path.Contains(fnc)) && path != String.Empty;
        //}
    }
}
