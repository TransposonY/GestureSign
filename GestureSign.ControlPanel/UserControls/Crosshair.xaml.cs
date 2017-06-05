using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GestureSign.ControlPanel.UserControls
{
    /// <summary>
    /// Crosshair.xaml 的交互逻辑
    /// </summary>
    public partial class Crosshair : UserControl, IDisposable
    {
        Cursor myCursor;
        BitmapImage imgCrosshair = new BitmapImage(new Uri(@"../../Resources/crosshair.ico", UriKind.Relative));

        public event EventHandler<MouseButtonEventArgs> CrosshairDragged;

        public event EventHandler<MouseEventArgs> CrosshairDragging;

        bool isMove = false;//是否需要移动
        public Crosshair()
        {
            InitializeComponent();
            // use file path to avoid System.IO.IOException caused by GetTempFileName in System.Windows.Input.Cursor.LoadFromStream
            myCursor = new Cursor(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\crosshair.cur"));
        }

        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                myCursor.Dispose();
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

        private void dragger_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(Image))
            {
                //Image image = e.OriginalSource as Image;
                isMove = true;
                dragger.Source = null;
                dragger.Cursor = myCursor;
                dragger.CaptureMouse();
            }

        }

        private void dragger_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.OriginalSource != null && e.OriginalSource.GetType() == typeof(Image) && isMove)
            {
                if (CrosshairDragging != null)
                {
                    CrosshairDragging(this, e);
                }
            }
        }

        private void dragger_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(Image))
            {
                dragger.Source = imgCrosshair;
                dragger.Cursor = Cursors.Cross;
                isMove = false;
                dragger.ReleaseMouseCapture();
                if (CrosshairDragged != null)
                {
                    CrosshairDragged(this, e);
                }
            }
        }
    }
}
