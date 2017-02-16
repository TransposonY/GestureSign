using System;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.ControlPanel.ViewModel;
using ManagedWinapi;
using ManagedWinapi.Hooks;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// GestureDefinition.xaml 的交互逻辑
    /// </summary>
    public partial class GestureDefinition : MetroWindow
    {
        public GestureDefinition()
        {
            InitializeComponent();
            MessageProcessor.GotNewGesture += MessageProcessor_GotNewGesture;
        }

        public GestureDefinition(IGesture gesture)
            : this()
        {
            _oldGesture = (Gesture)gesture;
            _currentPointPatterns = gesture.PointPatterns;
            GestureManager.Instance.GestureName = gesture.Name;
        }

        public GestureDefinition(bool selectGesture)
        : this()
        {
            _selectGesture = selectGesture;
        }

        private Gesture _oldGesture;
        private Gesture _similarGesture;
        private PointPattern[] _currentPointPatterns;
        private Color _color;
        private bool _selectGesture;

        public string GestureName { get; set; }

        public Gesture SimilarGesture
        {
            get { return _similarGesture; }
            set
            {
                _similarGesture = value;
                if (value == null)
                {
                    cmdDone.Content = LocalizationProvider.Instance.GetTextValue("Common.Save");
                    UseExistingButton.Visibility = ExistingGestureImage.Visibility = Visibility.Collapsed;
                }
                else
                {
                    cmdDone.Content = LocalizationProvider.Instance.GetTextValue("Common.Overwrite");

                    ExistingGestureImage.Source = GestureImage.CreateImage(value.PointPatterns, new Size(65, 65), _color);
                    var hotkey = value.Hotkey;
                    if (hotkey != null)
                        HotKeyTextBox.HotKey = new HotKey(KeyInterop.KeyFromVirtualKey(hotkey.KeyCode), (ModifierKeys)hotkey.ModifierKeys);
                    MouseActionComboBox.SelectedValue = value.MouseAction;

                    ExistingGestureImage.Visibility = Visibility.Visible;
                    UseExistingButton.Visibility = _selectGesture ? Visibility.Visible : Visibility.Collapsed;

                    GestureName = value.Name;
                }
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (MouseActionDescription.DescriptionDict.ContainsKey(AppConfig.DrawingButton))
                DrawingButtonTextBlock.Text = MouseActionDescription.DescriptionDict[AppConfig.DrawingButton] + "  +  ";
            _color = (Color)Application.Current.Resources["HighlightColor"];

            if (_currentPointPatterns != null)
                imgGestureThumbnail.Source = GestureImage.CreateImage(_currentPointPatterns, new Size(65, 65), _color);

            if (_oldGesture == null)
            {
                await SetTrainingState(true);
                Title = _selectGesture
                    ? LocalizationProvider.Instance.GetTextValue("GestureDefinition.SelectGesture")
                    : LocalizationProvider.Instance.GetTextValue("GestureDefinition.NewGesture");
            }
            else
            {
                MouseActionComboBox.SelectedValue = _oldGesture.MouseAction;
                Title = LocalizationProvider.Instance.GetTextValue("GestureDefinition.Edit");
                GestureName = _oldGesture.Name; //this.txtGestureName.Text
                var hotkey = ((Gesture)_oldGesture).Hotkey;
                if (hotkey != null)
                    HotKeyTextBox.HotKey = new HotKey(KeyInterop.KeyFromVirtualKey(hotkey.KeyCode), (ModifierKeys)hotkey.ModifierKeys);
            }
        }


        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            await NamedPipe.SendMessageAsync("StopTraining", "GestureSignDaemon");
        }

        private void UseExistingButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveGesture(_similarGesture.Name, _similarGesture.PointPatterns))
            {
                DialogResult = true;
                Close();
            }
        }

        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            //merge similar gesture
            if (_oldGesture != null && _similarGesture != null && _oldGesture.Name != _similarGesture.Name)
            {
                GestureManager.Instance.DeleteGesture(_similarGesture.Name);
                ApplicationManager.Instance.RenameGesture(_oldGesture.Name, _similarGesture.Name);
            }

            if (SaveGesture(_oldGesture != null ? _oldGesture.Name : GestureName, _currentPointPatterns))
            {
                DialogResult = true;
                Close();
            }
        }

        private async void MessageProcessor_GotNewGesture(object sender, Gesture e)
        {
            string similarGestureName = e.Name;
            SimilarGesture = (Gesture)GestureManager.Instance.GetNewestGestureSample(similarGestureName);

            _currentPointPatterns = e.PointPatterns;
            imgGestureThumbnail.Source = GestureImage.CreateImage(_currentPointPatterns, new Size(65, 65), _color);

            await SetTrainingState(false);
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            await SetTrainingState(true);
        }

        private async void imgGestureThumbnail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && _oldGesture == null && _currentPointPatterns?.Length < 3)
            {
                await NamedPipe.SendMessageAsync("StackUpGesture", "GestureSignDaemon");
                await SetTrainingState(true);
            }
        }

        #region Private Methods

        private async Task<bool> SetTrainingState(bool state)
        {
            if (state)
            {
                SimilarGesture = null;
                _currentPointPatterns = null;
                imgGestureThumbnail.Source = null;
                GestureName = string.Empty;

                DrawGestureTextBlock.Visibility = Visibility.Visible;
                ResetButton.Visibility = Visibility.Collapsed;
                return await NamedPipe.SendMessageAsync("StartTeaching", "GestureSignDaemon");
            }
            else
            {
                DrawGestureTextBlock.Visibility = Visibility.Collapsed;
                ResetButton.Visibility = Visibility.Visible;
                return await NamedPipe.SendMessageAsync("StopTraining", "GestureSignDaemon");
            }
        }

        private bool SaveGesture(string newName, PointPattern[] newPointPatterns)
        {
            if (newPointPatterns == null || newPointPatterns.Length == 0)
                return false;

            if (string.IsNullOrEmpty(newName))
            {
                newName = GestureManager.GetNewGestureName();
            }

            var newGesture = new Gesture()
            {
                Name = newName,
                PointPatterns = newPointPatterns,
                MouseAction = (MouseActions?)MouseActionComboBox.SelectedValue ?? MouseActions.None,
                Hotkey = HotKeyTextBox.HotKey != null ?
                  new Hotkey()
                  {
                      KeyCode = KeyInterop.VirtualKeyFromKey(HotKeyTextBox.HotKey.Key),
                      ModifierKeys = (int)HotKeyTextBox.HotKey.ModifierKeys
                  } : null
            };

            if (GestureManager.Instance.GestureExists(newName))
            {
                GestureManager.Instance.DeleteGesture(newName);
            }
            GestureManager.Instance.AddGesture(newGesture);

            GestureManager.Instance.GestureName = newName;
            GestureManager.Instance.SaveGestures();

            return true;
        }

        #endregion
    }
}
