using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using GestureSign.Common.Input;
using GestureSign.Common.Drawing;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using System.ComponentModel;

namespace GestureSign.UI
{
    /// <summary>
    /// GestureDefinition.xaml 的交互逻辑
    /// </summary>
    public partial class GestureDefinition : MetroWindow, INotifyPropertyChanged, IDataErrorInfo
    {
        public GestureDefinition()
        {
            InitializeComponent();
        }

        public GestureDefinition(List<List<System.Drawing.Point>> capturedPoints)
            : this()
        {
            _CapturedPoints = capturedPoints;
        }

        public GestureDefinition(List<List<System.Drawing.Point>> capturedPoints, string gestureName)
            : this(capturedPoints)
        {
            ExistingGestureName = Gestures.GestureManager.Instance.GestureName = gestureName;
            this.ReName = true;
        }

        string ExistingGestureName;
        List<List<System.Drawing.Point>> _CapturedPoints = null;
        bool reName = false;
        public static event EventHandler GesturesChanged;
        string name;

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
        public string GName
        {
            get { return this.name; }
            set
            {
                if (Equals(value, name))
                {
                    return;
                }

                name = value;
                RaisePropertyChanged("NameProperty");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event if needed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == "GName" && String.IsNullOrWhiteSpace(GName))
                {
                    return "请输入一个手势名称！";
                }


                return null;
            }
        }

        public string Error { get { return string.Empty; } }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Gestures.GestureManager.Instance.GestureName))
            {
                this.ExistingTextBlock.Visibility = this.ExistingGestureImage.Visibility = Visibility.Collapsed;
                this.txtGestureName.Focus();
            }
            else
            {
                if (!reName)
                {
                    this.ExistingGestureImage.Source = GestureImage.CreateImage(Gestures.GestureManager.Instance.GetNewestGestureSample().Points, new Size(65, 65));
                }
                GName = Gestures.GestureManager.Instance.GestureName;//this.txtGestureName.Text
                this.txtGestureName.SelectAll();
            }
            // Disable drawing gestures
            Input.TouchCapture.Instance.DisableTouchCapture();
            this.imgGestureThumbnail.Source = GestureImage.CreateImage(_CapturedPoints, new Size(65, 65));
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Input.TouchCapture.Instance.EnableTouchCapture();
        }


        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            if (SaveGesture())
            {
                this.Close();

            }
        }

        private void cmdCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cmdNext_Click(object sender, RoutedEventArgs e)
        {
            if (SaveGesture())
            {
                this.Close();
                ApplicationDialog ad = new ApplicationDialog(Gestures.GestureManager.Instance.GestureName);
                ad.Show();
            }
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
                Gestures.GestureManager.Instance.RenameGesture(ExistingGestureName, newGestureName);
            }
            else
            {
                if (String.IsNullOrEmpty(Gestures.GestureManager.Instance.GestureName))
                {
                    if (Gestures.GestureManager.Instance.GestureExists(newGestureName))
                    {
                        this.ShowMessageAsync("手势已存在", "输入的手势名称已存在，请重新输入一个手势名称", MessageDialogStyle.Affirmative, new MetroDialogSettings() { AffirmativeButtonText = "确定" });
                        return false;
                    }
                    // Add new gesture to gesture manager
                    Gestures.GestureManager.Instance.AddGesture(new Gestures.Gesture(newGestureName, _CapturedPoints));
                    Gestures.GestureManager.Instance.GestureName = newGestureName;
                    Gestures.GestureManager.Instance.SaveGestures();
                }
                else
                {
                    if (newGestureName != Gestures.GestureManager.Instance.GestureName)
                    {
                        // Add new gesture to gesture manager
                        Gestures.GestureManager.Instance.AddGesture(new Gestures.Gesture(newGestureName, _CapturedPoints));
                        Gestures.GestureManager.Instance.GestureName = newGestureName;
                        Gestures.GestureManager.Instance.SaveGestures();
                    }
                }
            }

            if (GesturesChanged != null)
                GesturesChanged(this, new EventArgs());
            return true;
        }




        #endregion

    }
}
