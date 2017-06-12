using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using System.Linq;
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
        private bool _stackUp;
        private PointPattern[] _tempPointPattern;

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
        }

        private void MessageProcessor_GotNewPattern(object sender, PointPattern[] newPattern)
        {
            var currentPatterns = newPattern;
            if (_stackUp && _tempPointPattern != null)
            {
                currentPatterns = _tempPointPattern.Concat(newPattern).ToArray();
                _stackUp = false;
                _tempPointPattern = null;
            }
            var existingSimilarGestureName = GestureManager.Instance.GetMostSimilarGestureName(currentPatterns);
            if (existingSimilarGestureName == null)
            {
                CurrentGesture = new Gesture(null, currentPatterns);
                ExistingTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (OldGesture?.Name == existingSimilarGestureName)
                {
                    CurrentGesture = new Gesture(existingSimilarGestureName, currentPatterns);
                }
                else
                {
                    ExistingTextBlock.Visibility = Visibility.Visible;
                    CurrentGesture = GestureManager.Instance.GetNewestGestureSample(existingSimilarGestureName);
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
                MessageProcessor.GotNewPattern += MessageProcessor_GotNewPattern;
                NamedPipe.SendMessageAsync("StartTeaching", "GestureSignDaemon");
            }
            else
            {
                DrawGestureTextBlock.Visibility = Visibility.Collapsed;
                RedrawButton.Visibility = Visibility.Visible;
                MessageProcessor.GotNewPattern -= MessageProcessor_GotNewPattern;
                NamedPipe.SendMessageAsync("StopTraining", "GestureSignDaemon");
            }
        }

        private void RedrawButton_Click(object sender, RoutedEventArgs e)
        {
            SetTrainingState(true);
        }

        private void imgGestureThumbnail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && CurrentGesture != null && CurrentGesture.PointPatterns?.Length < 3)
            {
                _stackUp = true;
                _tempPointPattern = CurrentGesture.PointPatterns;
                SetTrainingState(true);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentGesture?.PointPatterns == null)
            {
                SetTrainingState(true);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SetTrainingState(false);
        }
    }
}
