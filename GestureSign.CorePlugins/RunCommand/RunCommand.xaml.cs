using System;
using System.Windows;
using System.Windows.Controls;
using GestureSign.Common.Localization;

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

        private void VariableComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (VariableComboBox.SelectedIndex < 1) return;

            string variable = $"%GS_{VariableComboBox.Text}%";
            int caretIndex = txtCommand.CaretIndex;
            txtCommand.Text = txtCommand.Text.Insert(caretIndex, variable);
            txtCommand.CaretIndex = caretIndex + variable.Length;

            VariableComboBox.SelectedIndex = 0;
            txtCommand.Focus();
        }

        private void VariableComboBox_OnInitialized(object sender, EventArgs e)
        {
            VariableComboBox.Items.Add(LocalizationProvider.Instance.GetTextValue("CorePlugins.RunCommand.InsertVariable"));

            VariableComboBox.Items.Add("Clipboard");
            VariableComboBox.Items.Add("WindowHandle");
            VariableComboBox.Items.Add("PID");
            VariableComboBox.Items.Add("Title");
            VariableComboBox.Items.Add("ClassName");
            VariableComboBox.Items.Add("StartPoint_X");
            VariableComboBox.Items.Add("StartPoint_Y");
            VariableComboBox.Items.Add("EndPoint_X");
            VariableComboBox.Items.Add("EndPoint_Y");
            VariableComboBox.SelectedIndex = 0;
        }
    }
}
