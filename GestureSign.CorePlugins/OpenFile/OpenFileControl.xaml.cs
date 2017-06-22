using GestureSign.Common.Localization;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestureSign.CorePlugins.OpenFile
{
    /// <summary>
    /// Interaction logic for OpenFileControl.xaml
    /// </summary>
    public partial class OpenFileControl : UserControl
    {
        private OpenFilePlugin.OpenFileSetting _settings = null;
        private TextBox _currentTextBox = null;

        internal OpenFilePlugin.OpenFileSetting Settings
        {
            get
            {
                _settings = new OpenFilePlugin.OpenFileSetting()
                {
                    Path = PathTextBox.Text.Trim(),
                    Variables = ArgumentsTextBox.Text.Trim()
                };
                return _settings;
            }
            set
            {
                _settings = value;
                PathTextBox.Text = _settings.Path;
                ArgumentsTextBox.Text = _settings.Variables;
            }
        }

        public OpenFileControl()
        {
            InitializeComponent();
        }

        private void VariableComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (VariableComboBox.SelectedIndex < 1) return;

            if (_currentTextBox == null)
                _currentTextBox = PathTextBox;

            string variable = $"%GS_{VariableComboBox.Text}%";
            int caretIndex = _currentTextBox.CaretIndex;
            _currentTextBox.Text = _currentTextBox.Text.Insert(caretIndex, variable);
            _currentTextBox.CaretIndex = caretIndex + variable.Length;

            VariableComboBox.SelectedIndex = 0;
            _currentTextBox.Focus();
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

        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _currentTextBox = e.NewFocus as TextBox;
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Create open file dialog to let user select file to open
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            try
            {
                // Set initial directory if path is valid
                if (System.IO.File.Exists(PathTextBox.Text.Trim()))
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(PathTextBox.Text.Trim());
            }
            catch { }
            if (dialog.ShowDialog().Value)
                PathTextBox.Text = dialog.FileName;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length != 0)
                        PathTextBox.Text = files[0];
                }
                catch
                {
                }
            }
            e.Handled = true;
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;

            e.Handled = true;
        }
    }
}
