using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestureSign.ControlPanel.UserControls
{
    /// <summary>
    /// Interaction logic for GestureSelector.xaml
    /// </summary>
    public partial class GestureSelector : UserControl
    {
        public IGesture CurrentGesture
        {
            get { return (IGesture)GetValue(CurrentGestureProperty); }
            set { SetValue(CurrentGestureProperty, value); }
        }

        public static readonly DependencyProperty CurrentGestureProperty =
            DependencyProperty.Register(nameof(CurrentGesture), typeof(IGesture), typeof(GestureSelector), new PropertyMetadata(new Gesture()));

        public IGesture OldGesture { get; set; }

        public GestureSelector()
        {
            InitializeComponent();
            MessageProcessor.GotNewGesture += MessageProcessor_GotNewGesture;
        }

        private void MessageProcessor_GotNewGesture(object sender, Gesture e)
        {
            var existingSimilarGesture = (Gesture)GestureManager.Instance.GetNewestGestureSample(e.Name);
            if (existingSimilarGesture == null)
            {
                CurrentGesture = new Gesture(null, e.PointPatterns);
                ExistingTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (OldGesture?.Name == existingSimilarGesture.Name)
                {
                    CurrentGesture = new Gesture(existingSimilarGesture.Name, e.PointPatterns);
                }
                else
                {
                    ExistingTextBlock.Visibility = Visibility.Visible;
                    CurrentGesture = existingSimilarGesture;
                }

            }
            SetTrainingState(false);
        }

        private void SetTrainingState(bool state)
        {
            if (state)
            {
                CurrentGesture = null;
                DrawGestureTextBlock.Visibility = Visibility.Visible;
                ExistingTextBlock.Visibility = RedrawButton.Visibility = Visibility.Collapsed;
                NamedPipe.SendMessageAsync("StartTeaching", "GestureSignDaemon");
            }
            else
            {
                DrawGestureTextBlock.Visibility = Visibility.Collapsed;
                RedrawButton.Visibility = Visibility.Visible;
                NamedPipe.SendMessageAsync("StopTraining", "GestureSignDaemon");
            }
        }

        private void RedrawButton_Click(object sender, RoutedEventArgs e)
        {
            SetTrainingState(true);
        }

        private void imgGestureThumbnail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && OldGesture == null && CurrentGesture.PointPatterns?.Length < 3)
            {
                NamedPipe.SendMessageAsync("StackUpGesture", "GestureSignDaemon");
                SetTrainingState(true);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentGesture.PointPatterns == null)
            {
                SetTrainingState(true);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            NamedPipe.SendMessageAsync("StopTraining", "GestureSignDaemon");
        }
    }
}
