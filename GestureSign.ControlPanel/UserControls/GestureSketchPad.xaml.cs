using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GestureSign.ControlPanel.UserControls
{
    /// <summary>Drawing surface for the Gesture tab's "draw to find": records one stroke and reports it while it is being drawn.</summary>
    public partial class GestureSketchPad : System.Windows.Controls.UserControl
    {
        // Samples closer than this to the previous one add nothing but noise.
        private const double MinimumStep = 3;

        public static readonly DependencyProperty ShowAnchorDotProperty = DependencyProperty.Register(
            nameof(ShowAnchorDot), typeof(bool), typeof(GestureSketchPad),
            new PropertyMetadata(true, (d, e) => ((GestureSketchPad)d).AnchorDot.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

        private readonly List<Point> _stroke = new List<Point>();

        public GestureSketchPad()
        {
            InitializeComponent();
        }

        /// <summary>Draws the dot that stands for the finger held in place while the others move.</summary>
        public bool ShowAnchorDot
        {
            get { return (bool)GetValue(ShowAnchorDotProperty); }
            set { SetValue(ShowAnchorDotProperty, value); }
        }

        /// <summary>The stroke drawn so far, in the pad's own coordinates.</summary>
        public IReadOnlyList<Point> Stroke
        {
            get { return _stroke; }
        }

        public bool HasStroke
        {
            get { return _stroke.Count > 0; }
        }

        /// <summary>Raised whenever the stroke grows or is cleared.</summary>
        public event EventHandler StrokeChanged;

        public void Clear()
        {
            if (_stroke.Count == 0)
                return;
            _stroke.Clear();
            Trail.Points = new PointCollection();
            StrokeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // A new stroke replaces the old one: the pad searches for one shape at a time.
            _stroke.Clear();
            Trail.Points = new PointCollection();
            Surface.CaptureMouse();
            AddPoint(e.GetPosition(Surface));
            e.Handled = true;
        }

        private void Surface_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Surface.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
                return;
            var position = e.GetPosition(Surface);
            var last = _stroke[_stroke.Count - 1];
            if ((position - last).Length >= MinimumStep)
                AddPoint(position);
        }

        private void Surface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!Surface.IsMouseCaptured)
                return;
            var position = e.GetPosition(Surface);
            if ((position - _stroke[_stroke.Count - 1]).Length > 0)
                AddPoint(position);
            Surface.ReleaseMouseCapture();
        }

        private void AddPoint(Point point)
        {
            // Keep the trail inside the pad even while the mouse is dragged past its edge.
            point = new Point(Math.Max(0, Math.Min(Surface.ActualWidth, point.X)), Math.Max(0, Math.Min(Surface.ActualHeight, point.Y)));
            _stroke.Add(point);
            Trail.Points.Add(point);
            StrokeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
