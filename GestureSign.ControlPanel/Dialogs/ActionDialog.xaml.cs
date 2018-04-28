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
using GestureSign.Common.Input;

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
            DependencyProperty.Register(nameof(CurrentGesture), typeof(IGesture), typeof(ActionDialog), new PropertyMetadata(new Gesture()));

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

            if (_sourceAction != null)
            {
                ActionNameTextBox.Text = _sourceAction.Name;
                ConditionTextBox.Text = _sourceAction.Condition;
                MouseActionComboBox.SelectedValue = _sourceAction.MouseHotkey;
                ActivateWindowCheckBox.IsChecked = _sourceAction.ActivateWindow;
                MouseCheckBox.IsChecked = !_sourceAction.IgnoredDevices.HasFlag(Devices.Mouse);
                TouchScreenCheckBox.IsChecked = !_sourceAction.IgnoredDevices.HasFlag(Devices.TouchScreen);
                TouchPadCheckBox.IsChecked = !_sourceAction.IgnoredDevices.HasFlag(Devices.TouchPad);
                PenCheckBox.IsChecked = !_sourceAction.IgnoredDevices.HasFlag(Devices.Pen);

                if (_sourceAction.ContinuousGesture != null)
                {
                    ContinuousGestureSwitch.IsChecked = true;
                    ContactCountSlider.Value = _sourceAction.ContinuousGesture.ContactCount;
                    GestureListBox.SelectedIndex = (int)Math.Log((int)_sourceAction.ContinuousGesture.Gesture, 2);
                }
                else
                    ContinuousGestureSwitch.IsChecked = false;

                var gesture = GestureManager.Instance.GetNewestGestureSample(_sourceAction.GestureName);
                if (gesture != null)
                    CurrentGesture = gesture;

                var hotkey = _sourceAction.Hotkey;
                if (hotkey != null)
                    HotKeyTextBox.HotKey = new HotKey(KeyInterop.KeyFromVirtualKey(hotkey.KeyCode), (ModifierKeys)hotkey.ModifierKeys);
            }
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

            var sameAction = _sourceApplication.Actions.FirstOrDefault(a => a == _sourceAction);
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
            Devices ignoredDevices = Devices.None;
            if (!MouseCheckBox.IsChecked.GetValueOrDefault())
                ignoredDevices |= Devices.Mouse;
            if (!TouchScreenCheckBox.IsChecked.GetValueOrDefault())
                ignoredDevices |= Devices.TouchScreen;
            if (!TouchPadCheckBox.IsChecked.GetValueOrDefault())
                ignoredDevices |= Devices.TouchPad;
            if (!PenCheckBox.IsChecked.GetValueOrDefault())
                ignoredDevices |= Devices.Pen;
            NewAction.IgnoredDevices = ignoredDevices;

            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();

            return true;
        }

        private bool SaveGesture(IGesture gesture)
        {
            if (string.IsNullOrEmpty(gesture.Name))
            {
                gesture.Name = GestureManager.Instance.GetNewGestureName();
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
