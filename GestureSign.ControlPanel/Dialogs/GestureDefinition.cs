using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.ControlPanel.Common;
using GestureSign.Gestures;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Point = System.Drawing.Point;

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
        /// <param name="capturedPoints"></param>
        /// <param name="gestureName"></param>
        public GestureDefinition(List<List<Point>> capturedPoints, string gestureName, bool reName)
            : this()
        {
            _CapturedPoints = capturedPoints;
            GestureManager.Instance.GestureName = gestureName;
            if (reName) _existingGestureName = gestureName;
            this.ReName = reName;
        }

        readonly string _existingGestureName;
        List<List<Point>> _CapturedPoints = null;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var brush = Application.Current.Resources["HighlightBrush"] as Brush ?? Brushes.RoyalBlue;

            this.imgGestureThumbnail.Source = GestureImage.CreateImage(_CapturedPoints, new Size(65, 65), brush);

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
                    this.ExistingGestureImage.Source = GestureImage.CreateImage(GestureManager.Instance.GetNewestGestureSample().Points, new Size(65, 65), brush);
                    return;
                }

                cmdNext.Visibility = ExistingTextBlock.Visibility = ExistingGestureImage.Visibility = Visibility.Collapsed;

                this.txtGestureName.Focus();
                this.txtGestureName.SelectAll();
            }
            cmdDone.Content = LocalizationProvider.Instance.GetTextValue("Common.Save");
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

        #region Private Methods



        private bool SaveGesture()
        {
            string newGestureName = txtGestureName.Text.Trim();

            if (newGestureName == "")
            {
                return false;
            }

            if (reName)
            {
                if (_existingGestureName.Equals(newGestureName)) return true;
                if (GestureManager.Instance.GestureExists(newGestureName))
                {
                    this.ShowMessageAsync(LocalizationProvider.Instance.GetTextValue("GestureDefinition.Messages.GestureExistsTitle"),
                        LocalizationProvider.Instance.GetTextValue("GestureDefinition.Messages.GestureExists"),
                        MessageDialogStyle.Affirmative,
                        new MetroDialogSettings()
                        {
                            AnimateHide = false,
                            AnimateShow = false,
                            AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK")
                        });
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
                        this.ShowMessageAsync(
                            LocalizationProvider.Instance.GetTextValue("GestureDefinition.Messages.GestureExistsTitle"),
                            LocalizationProvider.Instance.GetTextValue("GestureDefinition.Messages.GestureExists"),
                            MessageDialogStyle.Affirmative,
                            new MetroDialogSettings()
                            {
                                AnimateHide = false,
                                AnimateShow = false,
                                AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.OK"),
                            });
                        return false;
                    }
                    // Add new gesture to gesture manager
                    GestureManager.Instance.AddGesture(new Gesture(newGestureName, _CapturedPoints));
                    GestureManager.Instance.GestureName = newGestureName;
                }
                else
                {
                    if (newGestureName == GestureManager.Instance.GestureName)
                    {
                        GestureManager.Instance.DeleteGesture(newGestureName);
                        GestureManager.Instance.AddGesture(new Gesture(newGestureName, _CapturedPoints));
                    }
                    else
                    {
                        GestureManager.Instance.DeleteGesture(GestureManager.Instance.GestureName);
                        // Add new gesture to gesture manager
                        GestureManager.Instance.AddGesture(new Gesture(newGestureName, _CapturedPoints));
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
