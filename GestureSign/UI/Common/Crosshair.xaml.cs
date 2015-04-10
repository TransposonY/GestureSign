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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;

namespace GestureSign.UI
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
            // dragger.Source = imgCrosshair;
            using (MemoryStream memStream = new MemoryStream(Properties.Resources.crosshair))
            {
                myCursor = new System.Windows.Input.Cursor(memStream);
            }
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
