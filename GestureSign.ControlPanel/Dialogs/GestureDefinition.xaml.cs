using GestureSign.Common.Applications;
using GestureSign.Common.Extensions;
using GestureSign.Common.Gestures;
using MahApps.Metro.Controls;
using System.Windows;

namespace GestureSign.ControlPanel.Dialogs
{
    /// <summary>
    /// GestureDefinition.xaml 的交互逻辑
    /// </summary>
    public partial class GestureDefinition : MetroWindow
    {
        private Gesture _oldGesture;

        public IGesture CurrentGesture
        {
            get { return (IGesture)GetValue(CurrentGestureProperty); }
            set { SetValue(CurrentGestureProperty, value); }
        }

        public static readonly DependencyProperty CurrentGestureProperty =
            DependencyProperty.Register(nameof(CurrentGesture), typeof(IGesture), typeof(GestureDefinition), new PropertyMetadata(new Gesture()));

        protected GestureDefinition()
        {
            InitializeComponent();
        }

        public GestureDefinition(IGesture gesture)
            : this()
        {
            CurrentGesture = _oldGesture = (Gesture)gesture;
            GestureSelector.OldGesture = _oldGesture;
        }

        private void cmdDone_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentGesture == null)
                return;

            if (_oldGesture != null)
            {
                //merge similar gesture
                if (CurrentGesture.Name != null)
                {
                    if (_oldGesture.Name != CurrentGesture.Name)
                    {
                        GestureManager.Instance.DeleteGesture(CurrentGesture.Name);
                        ApplicationManager.Instance.Applications.RenameGestures(CurrentGesture.Name, _oldGesture.Name);
                        ApplicationManager.Instance.SaveApplications();
                    }
                }
                CurrentGesture.Name = _oldGesture.Name;
            }

            if (SaveGesture(CurrentGesture))
            {
                if (!DialogResult.GetValueOrDefault())
                    DialogResult = true;
                Close();
            }
        }

        #region Private Methods

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

        #endregion
    }
}
