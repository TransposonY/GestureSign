using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Gestures;
using GestureSign.UI.Common;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Point = System.Drawing.Point;

namespace GestureSign.UI
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
                if (value)
                {
                    this.cmdNext.Visibility = Visibility.Hidden;
                    this.ExistingTextBlock.Visibility = this.ExistingGestureImage.Visibility = Visibility.Collapsed;
                    this.txtGestureName.Visibility = Visibility.Visible;
                    this.txtGestureName.Focus();
                }
                reName = value;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var brush = Application.Current.Resources["HighlightBrush"] as Brush ?? Brushes.RoyalBlue;

            if (String.IsNullOrEmpty(GestureManager.Instance.GestureName))
            {
                this.ExistingTextBlock.Visibility = this.ExistingGestureImage.Visibility = Visibility.Collapsed;
                this.txtGestureName.Focus();
            }
            else
            {
                if (!reName)
                {
                    this.ExistingGestureImage.Source = GestureImage.CreateImage(GestureManager.Instance.GetNewestGestureSample().Points, new Size(65, 65), brush);
                }
                this.txtGestureName.Text = GestureManager.Instance.GestureName;//this.txtGestureName.Text
                this.txtGestureName.SelectAll();
            }
            this.imgGestureThumbnail.Source = GestureImage.CreateImage(_CapturedPoints, new Size(65, 65), brush);
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
                    this.ShowMessageAsync("手势已存在", "输入的手势名称已存在，请重新输入一个手势名称", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "确定" });
                    return false;
                }
                GestureManager.Instance.RenameGesture(_existingGestureName, newGestureName);
                GestureManager.Instance.SaveGestures();
            }
            else
            {
                if (String.IsNullOrEmpty(GestureManager.Instance.GestureName))
                {
                    if (GestureManager.Instance.GestureExists(newGestureName))
                    {
                        this.ShowMessageAsync("手势已存在", "输入的手势名称已存在，请重新输入一个手势名称", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "确定" });
                        return false;
                    }
                    // Add new gesture to gesture manager
                    GestureManager.Instance.AddGesture(new Gesture(newGestureName, _CapturedPoints));
                    GestureManager.Instance.GestureName = newGestureName;
                    GestureManager.Instance.SaveGestures();
                }
                else
                {
                    if (newGestureName != GestureManager.Instance.GestureName)
                    {
                        // Add new gesture to gesture manager
                        GestureManager.Instance.AddGesture(new Gesture(newGestureName, _CapturedPoints));
                        GestureManager.Instance.GestureName = newGestureName;
                    }
                    else
                    {
                        GestureManager.Instance.DeleteGesture(newGestureName);
                        GestureManager.Instance.AddGesture(new Gesture(newGestureName, _CapturedPoints));
                    }
                    GestureManager.Instance.SaveGestures();
                }
            }

            if (GesturesChanged != null)
                GesturesChanged(this, new EventArgs());
            return true;
        }




        #endregion

    }
}
