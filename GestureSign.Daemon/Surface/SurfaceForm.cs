using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Daemon.Input;
using GestureSign.Daemon.Native;
using ManagedWinapi.Windows;
using Microsoft.Win32;

namespace GestureSign.Daemon.Surface
{
    public class SurfaceForm : Form
    {
        #region Private Variables

        Pen _drawingPen;
        private Pen _shadowPen;
        private Pen _dirtyMarkerPen;
        int[] _lastStroke;
        Size _screenOffset = default(Size);
        DiBitmap _bitmap;
        private GraphicsPath _graphicsPath = new GraphicsPath();
        private GraphicsPath _dirtyGraphicsPath = new GraphicsPath();

        private float _screenDpi;
        private bool _settingsChanged;

        private const Int32 ULW_ALPHA = 0x00000002;

        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        #endregion

        #region Constructors

        public SurfaceForm()
        {
            InitializeForm();

            TouchCapture.Instance.PointCaptured += MouseCapture_PointCaptured;
            TouchCapture.Instance.CaptureEnded += MouseCapture_CaptureEnded;
            TouchCapture.Instance.CaptureCanceled += MouseCapture_CaptureCanceled;
            TouchCapture.Instance.CaptureStarted += Instance_CaptureStarted;
            AppConfig.ConfigChanged += (o, e) => { ResetSurface(); };
            // Respond to system event changes by reinitializing the form
            SystemEvents.DisplaySettingsChanged += (o, e) => { ResetSurface(); };
            SystemEvents.UserPreferenceChanged += (o, e) => { ResetSurface(); };
            //this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            //this.UpdateStyles();
        }


        #endregion

        #region Events

        protected void MouseCapture_PointCaptured(object sender, PointsCapturedEventArgs e)
        {
            var touchCapture = (ITouchCapture)sender;
            if (touchCapture.Mode != CaptureMode.UserDisabled &&
                touchCapture.State == CaptureState.Capturing &&
                AppConfig.VisualFeedbackWidth > 0 &&
                !(e.Points.Count == 1 && e.Points.First().Count == 1))
            {
                if (_bitmap == null || _lastStroke == null)
                {
                    ClearSurfaces();
                    _bitmap = new DiBitmap(Screen.PrimaryScreen.Bounds.Size);
                }
                DrawSegments(e.Points);
            }
        }

        protected void MouseCapture_CaptureEnded(object sender, EventArgs e)
        {
            if (AppConfig.VisualFeedbackWidth > 0 && _lastStroke != null)
                EndDraw();
        }

        protected void MouseCapture_CaptureCanceled(object sender, PointsCapturedEventArgs e)
        {
            if (AppConfig.VisualFeedbackWidth > 0)
                EndDraw();
        }

        private void Instance_CaptureStarted(object sender, PointsCapturedEventArgs e)
        {
            var touchCapture = (ITouchCapture)sender;

            if (AppConfig.VisualFeedbackWidth <= 0 || touchCapture.Mode == CaptureMode.UserDisabled) return;

            if (_settingsChanged)
            {
                _settingsChanged = false;
                InitializeForm();
            }

            ClearSurfaces();
        }

        #endregion

        #region Public Methods


        public void DrawSegments(List<List<Point>> points)
        {
            // Ensure that surface is visible
            if (!Visible)
            {
                TopMost = true;
                Show();
            }
            if (_lastStroke == null) { _lastStroke = points.Select(p => p.Count).ToArray(); return; }
            if (_lastStroke.Length != points.Count) return;
            try
            {
                _dirtyGraphicsPath.Reset();
                var surfaceGraphics = _bitmap.BeginDraw();
                var translatedPointList = new List<Point[]>(_lastStroke.Length);

                for (int i = 0; i < _lastStroke.Length; i++)
                {
                    // Create list of points that are new this draw
                    List<Point> newPoints = new List<Point>();
                    // Get number of points added since last draw including last point of last stroke and add new points to new points list

                    var iDelta = points[i].Count - _lastStroke[i] + 1;

                    newPoints.AddRange(points[i].Skip(points[i].Count() - iDelta).Take(iDelta));
                    if (newPoints.Count < 2) continue;

                    var translatedPoints = newPoints.Select(TranslatePoint).ToArray();
                    // Draw new line segments to main drawing surface
                    _graphicsPath.AddLines(translatedPoints);

                    _dirtyGraphicsPath.AddLines(translatedPoints);
                    translatedPointList.Add(translatedPoints);
                }
                _dirtyGraphicsPath.Widen(_dirtyMarkerPen);
                surfaceGraphics.SetClip(_dirtyGraphicsPath);

                foreach (var pp in translatedPointList)
                    surfaceGraphics.DrawLines(_drawingPen, pp);

                _bitmap.EndDraw();
                UpdateDraw();
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                ClearSurfaces();
            }
            // this.CreateGraphics().DrawImage(bmp, 0, 0);

            // Set last stroke to copy of current stroke
            // ToList method creates value copy of stroke list and assigns it to last stroke
            _lastStroke = points.Select(p => p.Count).ToArray();
        }

        #endregion

        #region Private Methods

        private void ResetSurface()
        {
            if (_lastStroke == null)
            {
                if (InvokeRequired) Invoke(new Action(InitializeForm));
                else InitializeForm();
            }
            else
            {
                if (InvokeRequired) Invoke(new Action(() => _settingsChanged = true));
                else _settingsChanged = true;
            }
        }

        private void InitializeForm()
        {
            // Set basic variables
            FormBorderStyle = FormBorderStyle.None;
            Name = "SurfaceForm";
            ShowIcon = false;
            StartPosition = FormStartPosition.Manual;
            Hide();


            // Combine monitor screen sizes and set form size to combined size
            Rectangle rOutput = new Rectangle();

            foreach (Screen oScreen in Screen.AllScreens)
                rOutput = Rectangle.Union(rOutput, oScreen.Bounds);

            Left = Screen.AllScreens.Min(s => s.Bounds.Left);
            Top = Screen.AllScreens.Min(s => s.Bounds.Top);
            Width = rOutput.Width;
            Height = rOutput.Height;
            // Store offset in class field
            _screenOffset = new Size(Location);
            _screenDpi = NativeMethods.GetScreenDpi() / 96f;

            InitializePen();
        }

        private void InitializePen()
        {
            _drawingPen = new Pen(AppConfig.VisualFeedbackColor, AppConfig.VisualFeedbackWidth * _screenDpi);
            _drawingPen.StartCap = _drawingPen.EndCap = LineCap.Round;
            _drawingPen.LineJoin = LineJoin.Round;

            _shadowPen = new Pen(Color.FromArgb(30, 0, 0, 0), (_drawingPen.Width + 4f)) { EndCap = LineCap.Round, StartCap = LineCap.Round };

            _dirtyMarkerPen = (Pen)_shadowPen.Clone();
            _dirtyMarkerPen.Width *= 1.5f;
        }


        private Point TranslatePoint(Point point)
        {
            // Add point offset
            return Point.Subtract(point, _screenOffset);
        }

        private void ClearSurfaces()
        {
            _lastStroke = null;
            if (_bitmap != null)
            {
                using (_bitmap)
                {
                    var g = _bitmap.BeginDraw();

                    _graphicsPath.Widen(_dirtyMarkerPen);
                    g.SetClip(_graphicsPath);
                    g.Clear(Color.Transparent);
                    _bitmap.EndDraw();

                    var pathDirty = Rectangle.Ceiling(_graphicsPath.GetBounds());
                    pathDirty.Offset(Bounds.Location);
                    pathDirty.Intersect(Bounds);
                    pathDirty.Offset(-Bounds.X, -Bounds.Y); //挪回来变为基于窗口的坐标

                    SetDiBitmap(_bitmap, pathDirty);

                    _graphicsPath.Reset();
                    _dirtyGraphicsPath.Reset();

                }
                _bitmap = null;
            }
        }


        private void SetDiBitmap(DiBitmap bmp, Rectangle dirtyRect, byte opacity = 255)
        {
            SetHBitmap(bmp.HBitmap, Bounds, Point.Empty, dirtyRect, opacity);
        }

        //dirtyRect是绝对坐标（在多屏的情况下）
        private void SetHBitmap(IntPtr hBitmap, Rectangle newWindowBounds, Point drawAt, Rectangle dirtyRect, byte opacity)
        {
            // IntPtr screenDc = Win32.GDI32.GetDC(IntPtr.Zero);

            IntPtr memDc = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

                var winSize = new NativeMethods.Size(newWindowBounds.Width, newWindowBounds.Height);
                var winPos = new NativeMethods.Point(newWindowBounds.X, newWindowBounds.Y);

                var drawBmpAt = new NativeMethods.Point(drawAt.X, drawAt.Y);
                var blend = new NativeMethods.BLENDFUNCTION { BlendOp = AC_SRC_OVER, BlendFlags = 0, SourceConstantAlpha = opacity, AlphaFormat = AC_SRC_ALPHA };

                var updateInfo = new NativeMethods.UPDATELAYEREDWINDOWINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.UPDATELAYEREDWINDOWINFO)),
                    dwFlags = ULW_ALPHA,
                    hdcDst = IntPtr.Zero,
                    hdcSrc = memDc
                };
                //NativeMethods.GetDC(IntPtr.Zero);//IntPtr.Zero; //ScreenDC

                // dirtyRect.X -= _bounds.X;
                // dirtyRect.Y -= _bounds.Y;

                //dirtyRect.Offset(-_bounds.X, -_bounds.Y);
                var dirRect = new NativeMethods.RECT(dirtyRect.X, dirtyRect.Y, dirtyRect.Right, dirtyRect.Bottom);

                unsafe
                {
                    updateInfo.pblend = &blend;
                    updateInfo.pptDst = &winPos;
                    updateInfo.psize = &winSize;
                    updateInfo.pptSrc = &drawBmpAt;
                    updateInfo.prcDirty = &dirRect;
                }

                NativeMethods.UpdateLayeredWindowIndirect(Handle, ref updateInfo);
                // Debug.Assert(NativeMethods.GetLastError() == 0);

                //NativeMethods.UpdateLayeredWindow(Handle, IntPtr.Zero, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, GDI32.ULW_ALPHA);

            }
            finally
            {

                //GDI32.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    NativeMethods.SelectObject(memDc, oldBitmap);
                    //Windows.DeleteObject(hBitmap); // The documentation says that we have to use the Windows.DeleteObject... but since there is no such method I use the normal DeleteObject from Win32 GDI and it's working fine without any resource leak.
                    //Win32.DeleteObject(hBitmap);
                }
                NativeMethods.DeleteDC(memDc);
            }
        }

        private void UpdateDraw()
        {
            var pathDirty = Rectangle.Ceiling(_dirtyGraphicsPath.GetBounds());
            pathDirty.Offset(Bounds.Location);
            pathDirty.Intersect(Bounds);
            pathDirty.Offset(-Bounds.X, -Bounds.Y); //挪回来变为基于窗口的坐标

            SetDiBitmap(_bitmap, /*_pathDirtyRect*/pathDirty, (byte)(AppConfig.Opacity * 0xFF));
        }

        private void EndDraw()
        {
            Hide();
            TopMost = false;

            ClearSurfaces();
        }
        #endregion

        #region Base Method Overrides

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myParams = base.CreateParams;
                myParams.ExStyle = myParams.ExStyle | (int)WindowExStyleFlags.NOACTIVATE |
                                    (int)WindowExStyleFlags.TOOLWINDOW |
                                    (int)WindowExStyleFlags.LAYERED;
                return myParams;
            }
        }
        #endregion
    }
}