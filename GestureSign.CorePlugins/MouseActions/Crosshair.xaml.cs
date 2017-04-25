using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GestureSign.CorePlugins.MouseActions
{
    /// <summary>
    /// Crosshair.xaml 的交互逻辑
    /// </summary>
    public partial class Crosshair : UserControl, IDisposable
    {
        Cursor _myCursor;
        BitmapImage _imgCrosshair = new BitmapImage(new Uri(@"crosshair.ico", UriKind.Relative));

        public event EventHandler<MouseButtonEventArgs> CrosshairDragged;

        public event EventHandler<MouseEventArgs> CrosshairDragging;

        bool _isMove = false;//是否需要移动

        public Crosshair()
        {
            InitializeComponent();
            // use file path to avoid System.IO.IOException caused by GetTempFileName in System.Windows.Input.Cursor.LoadFromStream
            _myCursor = new Cursor(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets\\crosshair.cur"));
        }

        // Flag: Has Dispose already been called?
        bool _disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                _myCursor.Dispose();
            }

            // Free any unmanaged objects here.
            //
            _disposed = true;
        }

        private void dragger_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(Image))
            {
                //Image image = e.OriginalSource as Image;
                _isMove = true;
                Dragger.Source = null;
                Dragger.Cursor = _myCursor;
                Dragger.CaptureMouse();
            }

        }

        private void dragger_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.OriginalSource != null && e.OriginalSource.GetType() == typeof(Image) && _isMove)
            {
                CrosshairDragging?.Invoke(this, e);
            }
        }

        private void dragger_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(Image))
            {
                Dragger.Source = _imgCrosshair;
                Dragger.Cursor = Cursors.Cross;
                _isMove = false;
                Dragger.ReleaseMouseCapture();
                CrosshairDragged?.Invoke(this, e);
            }
        }
    }
}
