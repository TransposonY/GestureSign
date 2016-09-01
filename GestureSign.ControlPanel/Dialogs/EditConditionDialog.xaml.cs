using System.Windows;
using MahApps.Metro.Controls;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for EditConditionDialog.xaml
    /// </summary>
    public partial class EditConditionDialog : MetroWindow
    {
        public EditConditionDialog(string condition)
        {
            DataContext = condition;

            InitializeComponent();
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            string variable = VariableComboBox.Text;
            int caretIndex = ConditionTextBox.CaretIndex;
            ConditionTextBox.Text = ConditionTextBox.Text.Insert(caretIndex, variable);
            ConditionTextBox.CaretIndex = caretIndex + variable.Length;

            ConditionTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ConditionTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            ConditionTextBox.Text = DataContext as string;
            ConditionTextBox.Focus();
        }

        private void VariableComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 1; i <= 10; i++)
            {
                VariableComboBox.Items.Add($"finger_{i}_start_X");
                VariableComboBox.Items.Add($"finger_{i}_start_Y");
                VariableComboBox.Items.Add($"finger_{i}_end_X");
                VariableComboBox.Items.Add($"finger_{i}_end_Y");
                VariableComboBox.Items.Add($"finger_{i}_ID");
            }
            VariableComboBox.SelectedIndex = 0;
        }
    }
}
