using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
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
        TouchDevice _ellipseControlTouchDevice;
        System.Windows.Point _lastPoint;
        public static event EventHandler GesturesChanged;

        public bool ReName
        {
            get { return reName; }
            set
            {
                if (value)
                {
                    Title = LocalizationProvider.Instance.GetTextValue("GestureDefinition.Rename");
                    this.cmdNext.Visibility = Visibility.Hidden;
                    this.ExistingTextBlock.Visibility = this.ExistingGestureImage.Visibility = Visibility.Collapsed;
                    this.txtGestureName.Visibility = Visibility.Visible;
                    this.txtGestureName.Focus();
                }
                else Title = LocalizationProvider.Instance.GetTextValue("GestureDefinition.Title");
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
                this.txtGestureName.Text = GestureManager.Instance.GestureName;//this.txtGestureName.Text
                this.txtGestureName.SelectAll();
                if (!reName)
                {
                    txtGestureName.IsEnabled = false;
                    this.ExistingGestureImage.Source = GestureImage.CreateImage(GestureManager.Instance.GetNewestGestureSample().Points, new Size(65, 65), brush);
                }
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

        private void Window_TouchDown(object sender, TouchEventArgs e)
        {
            // Capture to the ellipse.  
            e.TouchDevice.Capture(this);

            // Remember this contact if a contact has not been remembered already.  
            // This contact is then used to move the ellipse around.
            if (_ellipseControlTouchDevice == null)
            {
                _ellipseControlTouchDevice = e.TouchDevice;

                // Remember where this contact took place.  
                _lastPoint = PointToScreen(_ellipseControlTouchDevice.GetTouchPoint(this).Position);
            }

            // Mark this event as handled.  
            e.Handled = true;
        }

        private void Window_TouchMove(object sender, TouchEventArgs e)
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null && e.TouchDevice == _ellipseControlTouchDevice)
            {
                // Get the current position of the contact.  
                var currentTouchPoint = PointToScreen(_ellipseControlTouchDevice.GetTouchPoint(this).Position);
                // Get the change between the controlling contact point and
                // the changed contact point.  
                double deltaX = currentTouchPoint.X - _lastPoint.X;
                double deltaY = currentTouchPoint.Y - _lastPoint.Y;

                // Get and then set a new top position and a new left position for the ellipse.  

                Top = Top + deltaY / source.CompositionTarget.TransformToDevice.M11;
                Left = Left + deltaX / source.CompositionTarget.TransformToDevice.M22;

                // Forget the old contact point, and remember the new contact point.  
                _lastPoint = currentTouchPoint;

                // Mark this event as handled.  
                e.Handled = true;
            }
        }

        private void Window_TouchUp(object sender, TouchEventArgs e)
        {
            // If this contact is the one that was remembered  
            if (e.TouchDevice == _ellipseControlTouchDevice)
            {
                // Forget about this contact.
                _ellipseControlTouchDevice = null;
            }

            // Mark this event as handled.  
            e.Handled = true;
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
