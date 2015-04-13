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

using GestureSign.Common.Gestures;
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

        /// <summary>
        /// Edit gesture or add new one.
        /// </summary>
        /// <param name="capturedPoints"></param>
        /// <param name="gestureName"></param>
        public GestureDefinition(List<List<System.Drawing.Point>> capturedPoints, string gestureName, bool reName)
            : this()
        {
            _CapturedPoints = capturedPoints;
            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            GestureManager.Instance.GestureName = gestureName;
            if (reName) ExistingGestureName = gestureName;
            this.ReName = reName;
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
            var accent = MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current);
            var brush = accent != null ? accent.Item2.Resources["HighlightBrush"] as Brush : SystemParameters.WindowGlassBrush;

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
                this.txtGestureName.Text = GName = GestureManager.Instance.GestureName;//this.txtGestureName.Text
                this.txtGestureName.SelectAll();
            }
            this.imgGestureThumbnail.Source = GestureImage.CreateImage(_CapturedPoints, new Size(65, 65), brush);
        }


        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            await GestureSign.Common.InterProcessCommunication.NamedPipe.SendMessageAsync("EnableTouchCapture", "GestureSignDaemon");
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
                ApplicationDialog ad = new ApplicationDialog(GestureManager.Instance.GestureName);
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
                if (ExistingGestureName.Equals(newGestureName)) return true;
                GestureManager.Instance.RenameGesture(ExistingGestureName, newGestureName);
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
                    GestureManager.Instance.AddGesture(new Gestures.Gesture(newGestureName, _CapturedPoints));
                    GestureManager.Instance.GestureName = newGestureName;
                    GestureManager.Instance.SaveGestures();
                }
                else
                {
                    if (newGestureName != GestureManager.Instance.GestureName)
                    {
                        // Add new gesture to gesture manager
                        GestureManager.Instance.AddGesture(new Gestures.Gesture(newGestureName, _CapturedPoints));
                        GestureManager.Instance.GestureName = newGestureName;
                    }
                    else
                    {
                        GestureManager.Instance.DeleteGesture(newGestureName);
                        GestureManager.Instance.AddGesture(new Gestures.Gesture(newGestureName, _CapturedPoints));
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
