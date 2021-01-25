using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace GestureSign.ControlPanel.UserControls
{
    /// <summary>
    /// Crosshair.xaml 的交互逻辑
    /// </summary>
    public partial class Crosshair : UserControl
    {
        public event EventHandler<MouseButtonEventArgs> CrosshairDragged;

        public event EventHandler<MouseEventArgs> CrosshairDragging;

        bool isMove = false;//是否需要移动

        public Crosshair()
        {
            InitializeComponent();
        }

        private void Crosshair_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as Ellipse;
            if (source != null)
            {
                isMove = true;
                Dragger.Visibility = System.Windows.Visibility.Hidden;
                source.Cursor = Cursors.Cross;
                source.CaptureMouse();
            }

        }

        private void Crosshair_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.OriginalSource != null && e.OriginalSource.GetType() == typeof(Ellipse) && isMove)
            {
                if (CrosshairDragging != null)
                {
                    CrosshairDragging(this, e);
                }
            }
        }

        private void Crosshair_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as Ellipse;
            if (source != null)
            {
                source.ReleaseMouseCapture();
                source.Cursor = null;
                Dragger.Visibility = System.Windows.Visibility.Visible;
                isMove = false;
                CrosshairDragged?.Invoke(this, e);
            }
        }
    }
}
