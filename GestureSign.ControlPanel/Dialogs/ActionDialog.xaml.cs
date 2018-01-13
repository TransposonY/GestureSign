using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.ViewModel;
using MahApps.Metro.Controls;
using ManagedWinapi;
using ManagedWinapi.Hooks;
using System.Linq;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// Interaction logic for ActionDialog.xaml
    /// </summary>
    public partial class ActionDialog : MetroWindow
    {
        #region Private Instance Fields

        private readonly IAction _sourceAction;
        private IApplication _sourceApplication;

        #endregion

        #region Public Instance Fields

        public IAction NewAction { get; private set; } = new GestureSign.Common.Applications.Action();

        #endregion

        #region Dependency Properties

        public IGesture CurrentGesture
        {
            get { return (IGesture)GetValue(CurrentGestureProperty); }
            set { SetValue(CurrentGestureProperty, value); }
        }

        public static readonly DependencyProperty CurrentGestureProperty =
            DependencyProperty.Register(nameof(CurrentGesture), typeof(IGesture), typeof(ActionDialog), new FrameworkPropertyMetadata(new Gesture(), new PropertyChangedCallback(OnCurrentGestureChanged)));

        #endregion

        #region Constructors

        protected ActionDialog()
        {
            InitializeComponent();
        }

        public ActionDialog(IAction sourceAction, IApplication sourceApplication) : this()
        {
            _sourceAction = sourceAction;
            _sourceApplication = sourceApplication;
        }

        #endregion

        #region Events

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (MouseActionDescription.DescriptionDict.ContainsKey(AppConfig.DrawingButton))
                DrawingButtonTextBlock.Text = MouseActionDescription.DescriptionDict[AppConfig.DrawingButton] + "  +  ";
            if (AppConfig.DrawingButton == MouseActions.None)
                MouseHotKeyTextBlock.Text += LocalizationProvider.Instance.GetTextValue("ActionDialog.MouseGestureNotEnabled");

            SetValue(_sourceAction);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Save new gesture
            if (CurrentGesture != null && string.IsNullOrEmpty(CurrentGesture.Name))
            {
                SaveGesture(CurrentGesture);
            }

            if (SaveAction())
            {
                if (!DialogResult.GetValueOrDefault())
                    DialogResult = true;
                Close();
            }
        }

        private void ConditionTextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            EditConditionDialog editConditionDialog = new EditConditionDialog(ConditionTextBox.Text);
            if (editConditionDialog.ShowDialog().Value)
            {
                ConditionTextBox.Text = editConditionDialog.ConditionTextBox.Text;
            }
        }

        private static void OnCurrentGestureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
                return;
            var actionDialog = (ActionDialog)sender;
            var existingAction = actionDialog._sourceApplication.Actions.FirstOrDefault(a => a.GestureName == ((IGesture)e.NewValue).Name);
            if (existingAction != null)
            {
                actionDialog.SetValue(existingAction);
            }
        }

        private void ContactCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateContinuousGestureText();
        }

        private void GestureListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                UpdateContinuousGestureText();
        }

        #endregion

        #region Private Methods

        private void SetValue(IAction action)
        {
            if (action != null)
            {
                ActionNameTextBox.Text = action.Name;
                ConditionTextBox.Text = action.Condition;
                MouseActionComboBox.SelectedValue = action.MouseHotkey;
                ActivateWindowCheckBox.IsChecked = action.ActivateWindow;

                if (action.ContinuousGesture != null)
                {
                    ContinuousGestureSwitch.IsChecked = true;
                    ContactCountSlider.Value = action.ContinuousGesture.ContactCount;
                    GestureListBox.SelectedIndex = (int)Math.Log((int)action.ContinuousGesture.Gesture, 2);
                }
                else
                    ContinuousGestureSwitch.IsChecked = false;

                var gesture = GestureManager.Instance.GetNewestGestureSample(action.GestureName);
                if (gesture != null)
                    CurrentGesture = gesture;

                var hotkey = action.Hotkey;
                if (hotkey != null)
                    HotKeyTextBox.HotKey = new HotKey(KeyInterop.KeyFromVirtualKey(hotkey.KeyCode), (ModifierKeys)hotkey.ModifierKeys);
            }
        }

        private bool ShowErrorMessage(string title, string message)
        {
            MessageFlyoutText.Text = message;
            MessageFlyout.Header = title;
            MessageFlyout.IsOpen = true;
            return false;
        }

        private bool SaveAction()
        {
            try
            {
                var regex = new Regex("finger_[0-9]+_start_[XY]?|finger_[0-9]+_end_[XY]?|finger_[0-9]+_ID");
                var replaced = regex.Replace(ConditionTextBox.Text, "10");

                DataTable dataTable = new DataTable();
                dataTable.Compute(replaced, null);
            }
            catch (Exception exception)
            {
                return ShowErrorMessage(LocalizationProvider.Instance.GetTextValue("ActionDialog.Messages.ConditionError"), exception.Message);
            }
            // Move command to existing action
            var sameAction = _sourceApplication.Actions.FirstOrDefault(a => a.GestureName == CurrentGesture?.Name);
            if (sameAction != null)
                NewAction = sameAction;
            else
                _sourceApplication.AddAction(NewAction);

            // Store new values
            NewAction.Condition = string.IsNullOrWhiteSpace(ConditionTextBox.Text) ? null : ConditionTextBox.Text;
            NewAction.ActivateWindow = ActivateWindowCheckBox.IsChecked;
            NewAction.GestureName = CurrentGesture?.Name ?? string.Empty;
            NewAction.Name = ActionNameTextBox.Text.Trim();
            NewAction.MouseHotkey = (MouseActions?)MouseActionComboBox.SelectedValue ?? MouseActions.None;
            NewAction.Hotkey = HotKeyTextBox.HotKey != null
                ? new Hotkey()
                {
                    KeyCode = KeyInterop.VirtualKeyFromKey(HotKeyTextBox.HotKey.Key),
                    ModifierKeys = (int)HotKeyTextBox.HotKey.ModifierKeys
                }
                : null;
            int contactCount = (int)ContactCountSlider.Value;
            NewAction.ContinuousGesture = ContinuousGestureSwitch.IsChecked.GetValueOrDefault() && contactCount > 1 && GestureListBox.SelectedIndex >= 0 ? new ContinuousGesture(contactCount, (Gestures)(1 << GestureListBox.SelectedIndex)) : null;

            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();

            return true;
        }

        private bool SaveGesture(IGesture gesture)
        {
            if (string.IsNullOrEmpty(gesture.Name))
            {
                gesture.Name = GestureManager.GetNewGestureName();
            }

            if (GestureManager.Instance.GestureExists(gesture.Name))
            {
                GestureManager.Instance.DeleteGesture(gesture.Name);
            }
            GestureManager.Instance.AddGesture(gesture);

            GestureManager.Instance.SaveGestures();

            return true;
        }

        private void UpdateContinuousGestureText()
        {
            if (GestureListBox == null || ContactCountSlider == null || ContinuousGestureText == null) return;
            string direction;
            switch ((Gestures)(1 << GestureListBox.SelectedIndex))
            {
                case Gestures.Left:
                    direction = LocalizationProvider.Instance.GetTextValue("Action.Left");
                    break;
                case Gestures.Right:
                    direction = LocalizationProvider.Instance.GetTextValue("Action.Right");
                    break;
                case Gestures.Up:
                    direction = LocalizationProvider.Instance.GetTextValue("Action.Up");
                    break;
                case Gestures.Down:
                    direction = LocalizationProvider.Instance.GetTextValue("Action.Down");
                    break;
                default:
                    direction = null;
                    break;
            }
            ContinuousGestureText.Text = string.Format(LocalizationProvider.Instance.GetTextValue("Action.Fingers"), (int)ContactCountSlider.Value) + direction;
        }

        #endregion
    }
}
