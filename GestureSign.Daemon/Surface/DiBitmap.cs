using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using GestureSign.Daemon.Native;

namespace GestureSign.Daemon.Surface
{
    /// <summary>
    /// 对GDI hBitmap的封装。通过HBitmap属性方位原始hBitmap
    /// From https://github.com/yingDev/WGestures
    /// </summary>
    public class DiBitmap : IDisposable
    {
        //public delegate void DrawingHandler(Graphics g);

        public IntPtr HBitmap { get; private set; }

        public Size Size { get; }
        public PixelFormat PixelFormat { get; }

        private IntPtr _memDc;
        private IntPtr _oldObject;
        private Graphics _graphics;

        public DiBitmap(Size size, PixelFormat pixelFormat = PixelFormat.Format32bppArgb)
        {
            Size = size;
            PixelFormat = pixelFormat;

            var binfo = new NativeMethods.BITMAPINFO
            {
                bmiHeader =
                {
                    biSize = Marshal.SizeOf(typeof (NativeMethods.BITMAPINFOHEADER)),
                    biWidth = Size.Width,
                    biHeight = Size.Height,
                    biBitCount = (short) Image.GetPixelFormatSize(PixelFormat),
                    biPlanes = 1,
                    biCompression = 0
                }
            };
            //var screenDc = Native.GetDC(IntPtr.Zero);
            //var memDc = Native.CreateCompatibleDC(IntPtr.Zero);
            //if (memDc == IntPtr.Zero) throw new ApplicationException("初始化失败：创建MemDc失败(" + Native.GetLastError() + ")");

            _memDc = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
            if (_memDc == IntPtr.Zero)
                throw new ApplicationException("CreateCompatibleDC(IntPtr.Zero)fail(" + Marshal.GetLastWin32Error() + ")");

            IntPtr ptrBits;
            HBitmap = NativeMethods.CreateDIBSection(_memDc, ref binfo, 0, out ptrBits, IntPtr.Zero, 0);

            //HBitmap = Native.CreateCompatibleBitmap(MemDc, size.Width, size.Height);
            if (HBitmap == IntPtr.Zero)
                throw new ApplicationException("Initialization failure：CreateDIBSection(...)fail(" + Marshal.GetLastWin32Error() + ")");

            _oldObject = NativeMethods.SelectObject(_memDc, HBitmap);
            _graphics = Graphics.FromHdc(_memDc);
            _graphics.CompositingQuality = CompositingQuality.HighSpeed;
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            NativeMethods.SelectObject(_memDc, _oldObject);
        }

        public Graphics BeginDraw()
        {
            _oldObject = NativeMethods.SelectObject(_memDc, HBitmap);

            /*if (_graphics == null)
            {
                _graphics = Graphics.FromHdc(_memDc);

                _graphics.CompositingQuality = CompositingQuality.HighSpeed;
                _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            }*/

            return _graphics;
        }

        public void EndDraw()
        {
            NativeMethods.SelectObject(_memDc, _oldObject);
        }

        public void Dispose()
        {
            if (_graphics != null)
            {
                _graphics.Dispose();
                _graphics = null;
            }

            NativeMethods.SelectObject(_memDc, _oldObject);
            NativeMethods.DeleteDC(_memDc);

            if (HBitmap != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(HBitmap);
                HBitmap = IntPtr.Zero;
            }

        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                Dispose();
                GC.SuppressFinalize(this);
            }
            else
            {
                if (HBitmap != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(HBitmap);
                    HBitmap = IntPtr.Zero;
                }

                NativeMethods.SelectObject(_memDc, _oldObject);
                NativeMethods.DeleteDC(_memDc);

            }
        }

        ~DiBitmap()
        {
            Dispose(false);
        }
    }
}
