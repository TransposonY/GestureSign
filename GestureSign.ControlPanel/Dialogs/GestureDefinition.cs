using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        }

        /// <summary>
        /// Edit gesture or add new one.
        /// </summary>
        public GestureDefinition(IGesture gesture, bool reName)
            : this()
        {
            _capturedPointPatterns = gesture.PointPatterns;
            GestureManager.Instance.GestureName = gesture.Name;
            if (reName) _existingGestureName = gesture.Name;
            this.ReName = reName;
        }

        readonly string _existingGestureName;
        PointPattern[] _capturedPointPatterns = null;
        bool reName = false;
        public static event EventHandler GesturesChanged;

        public bool ReName
        {
            get { return reName; }
            set
            {
                Title = LocalizationProvider.Instance.GetTextValue(value ? "GestureDefinition.Rename" : "GestureDefinition.Title");
                reName = value;
            }
        }

        public string GestureName { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var color = (Color)Application.Current.Resources["HighlightColor"];

            this.imgGestureThumbnail.Source = GestureImage.CreateImage(_capturedPointPatterns, new Size(65, 65), color);

            if (String.IsNullOrEmpty(GestureManager.Instance.GestureName))
            {
                this.ExistingTextBlock.Visibility = this.ExistingGestureImage.Visibility = Visibility.Collapsed;
                this.txtGestureName.Focus();
            }
            else
            {
                this.txtGestureName.Text = GestureManager.Instance.GestureName;//this.txtGestureName.Text
                if (!reName)
                {
                    cmdDone.Content = LocalizationProvider.Instance.GetTextValue("Common.Overwrite");
                    txtGestureName.IsEnabled = false;

                    var gesture = GestureManager.Instance.GetNewestGestureSample();
                    if (gesture != null)
                        this.ExistingGestureImage.Source = GestureImage.CreateImage(gesture.PointPatterns, new Size(65, 65), color);
                    return;
                }

                OverlayGestureButton.Visibility = cmdNext.Visibility = ExistingTextBlock.Visibility = ExistingGestureImage.Visibility = Visibility.Collapsed;

                this.txtGestureName.Focus();
                this.txtGestureName.SelectAll();
            }
            cmdDone.Content = LocalizationProvider.Instance.GetTextValue("Common.Save");

            OverlayGestureButton.IsEnabled = _capturedPointPatterns.Length < 3;
        }


        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            await NamedPipe.SendMessageAsync("EnableTouchCapture", "GestureSignDaemon");
        }


        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            if (SaveGesture())
            {
                this.Close();
            }
            else txtGestureName.Focus();
        }

        private void cmdCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cmdNext_Click(object sender, RoutedEventArgs e)
        {
            if (SaveGesture())
            {
                this.Hide();
                ActionDialog ad = new ActionDialog(GestureManager.Instance.GestureName);
                ad.Show();
                this.Close();
            }
            else txtGestureName.Focus();
        }

        private async void OverlayGestureButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (await NamedPipe.SendMessageAsync("OverlayGesture", "GestureSignDaemon"))
            {
                Close();
            }
        }

        private void txtGestureName_TextChanged(object sender, TextChangedEventArgs e)
        {
            string newGestureName = txtGestureName.Text.Trim();
            if (_existingGestureName == newGestureName) return;

            txtGestureName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        #region Private Methods



        private bool SaveGesture()
        {
            string newGestureName = txtGestureName.Text.Trim();

            if (string.IsNullOrEmpty(newGestureName))
            {
                txtGestureName.Clear();
                return false;
            }

            if (reName)
            {
                if (_existingGestureName.Equals(newGestureName)) return true;
                if (GestureManager.Instance.GestureExists(newGestureName))
                {
                    txtGestureName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                    return false;
                }
                GestureManager.Instance.RenameGesture(_existingGestureName, newGestureName);
            }
            else
            {
                if (String.IsNullOrEmpty(GestureManager.Instance.GestureName))
                {
                    if (GestureManager.Instance.GestureExists(newGestureName))
                    {
                        txtGestureName.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                        return false;
                    }
                    // Add new gesture to gesture manager
                    GestureManager.Instance.AddGesture(new Gesture(newGestureName, _capturedPointPatterns));
                    GestureManager.Instance.GestureName = newGestureName;
                }
                else
                {
                    if (newGestureName == GestureManager.Instance.GestureName)
                    {
                        GestureManager.Instance.DeleteGesture(newGestureName);
                        GestureManager.Instance.AddGesture(new Gesture(newGestureName, _capturedPointPatterns));
                    }
                    else
                    {
                        GestureManager.Instance.DeleteGesture(GestureManager.Instance.GestureName);
                        // Add new gesture to gesture manager
                        GestureManager.Instance.AddGesture(new Gesture(newGestureName, _capturedPointPatterns));
                        GestureManager.Instance.GestureName = newGestureName;
                    }
                }
            }
            GestureManager.Instance.SaveGestures();

            GesturesChanged?.Invoke(this, new EventArgs());
            return true;
        }

        #endregion

    }
}
