using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GestureSign.Common.Input;
using GestureSign.Common.UI;
using ManagedWinapi.Windows;
using Microsoft.Win32;

namespace GestureSignDaemon
{
    public partial class Surface : Form
    {
        #region Private Variables

        Color TransparentColor = Color.FromArgb(250, 0, 255);
        Graphics SurfaceGraphics = null;
        CompatibilitySurface[] CompatibilitySurfaces = null;
        RenderMode RenderMethod = RenderMode.Standard;
        Pen DrawingPen = null;
        int[] LastStroke = null;
        Size ScreenOffset = default(Size);
        //Bitmap bmp;//= new Bitmap(1280, 800);
        private enum RenderMode
        {
            Compatible,
            Standard
        }

        private class CompatibilitySurface : IDisposable
        {
            public Graphics SurfaceGraphics = null;
            public Size Offset = default(Size);

            public Point[] OffsetPoints(Point[] Points)
            {
                return Points.Select(p => Point.Subtract(p, Offset)).ToArray();
            }

            public void Dispose()
            {
                if (SurfaceGraphics != null)
                    SurfaceGraphics.Dispose();
            }
        }

        #endregion

        #region Constructors

        public Surface()
        {
            InitializeForm();

            Input.TouchCapture.Instance.PointCaptured += new PointsCapturedEventHandler(MouseCapture_PointCaptured);
            Input.TouchCapture.Instance.CaptureEnded += MouseCapture_CaptureEnded;
            Input.TouchCapture.Instance.CaptureCanceled += new PointsCapturedEventHandler(MouseCapture_CaptureCanceled);
            GestureSign.Common.Configuration.AppConfig.ConfigChanged += AppConfig_ConfigChanged;
            // Respond to system event changes by reinitializing the form
            SystemEvents.DisplaySettingsChanged += (o, e) => { InitializeForm(); };
            SystemEvents.UserPreferenceChanged += (o, e) => { InitializeForm(); };
            //this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            //this.UpdateStyles();
            //+= (o, se) => { InitializeForm(); };
        }

     

        #endregion

        #region Events

        protected void MouseCapture_PointCaptured(object sender, PointsCapturedEventArgs e)
        {
            if (GestureSign.Common.Configuration.AppConfig.VisualFeedbackWidth > 0 && e.State == CaptureState.Capturing)
                this.DrawSegments(e.Points);
        }

        protected void MouseCapture_CaptureEnded(object sender, EventArgs e)
        {
            if (GestureSign.Common.Configuration.AppConfig.VisualFeedbackWidth > 0)
                this.EndDraw();
        }

        protected void MouseCapture_CaptureCanceled(object sender, PointsCapturedEventArgs e)
        {
            if (GestureSign.Common.Configuration.AppConfig.VisualFeedbackWidth > 0)
                this.EndDraw();
        }

        void AppConfig_ConfigChanged(object sender, EventArgs e)
        {
            this.Invoke(new Action(() => { InitializeForm(); }));
        }

        #endregion

        #region Public Methods

        public void EndDraw()
        {
            ClearSurfaces();
            this.Hide();
        }
        public void DrawSegments(List<List<Point>> Points)
        {
            if (RenderMethod == RenderMode.Standard)
            {
                // Ensure that surface is visible
                if (!this.Visible)
                {
                    this.TopMost = true;
                    this.Show();
                }
                if (LastStroke == null) { LastStroke = Points.Select(p => p.Count).ToArray(); return; }
                if (LastStroke.Length != Points.Count) return;
                for (int i = 0; i < LastStroke.Length; i++)
                {
                    // Create list of points that are new this draw
                    List<Point> NewPoints = new List<Point>();
                    int iDelta = 0;
                    // Get number of points added since last draw including last point of last stroke and add new points to new points list

                    iDelta = Points[i].Count - LastStroke[i] + 1;


                    NewPoints.AddRange(Points[i].Skip(Points[i].Count() - iDelta).Take(iDelta));
                    if (NewPoints.Count < 2) continue;
                    // Draw new line segments to main drawing surface
                    SurfaceGraphics.DrawLines(DrawingPen, NewPoints.Select(p => TranslatePoint(p)).ToArray());
                }
                // this.CreateGraphics().DrawImage(bmp, 0, 0);

                // Set last stroke to copy of current stroke
                // ToList method creates value copy of stroke list and assigns it to last stroke
                LastStroke = Points.Select(p => p.Count).ToArray();
            }
            else
            {
                foreach (List<Point> Stroke in Points)
                {
                    if (Stroke.Count < 2) continue;
                    foreach (CompatibilitySurface surface in CompatibilitySurfaces)
                        surface.SurfaceGraphics.DrawLines(DrawingPen, surface.OffsetPoints(Stroke.ToArray()));
                }
            }

        }

        #endregion

        #region Private Methods

        private void InitializeForm()
        {
            // Set basic variables
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "Surface";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Hide();
            if (DesktopWindowManager.IsCompositionEnabled())
            {
                // Combine monitor screen sizes and set form size to combined size
                Rectangle rOutput = new Rectangle();

                foreach (Screen oScreen in Screen.AllScreens)
                    rOutput = Rectangle.Union(rOutput, oScreen.Bounds);

                this.Left = Screen.AllScreens.Min(s => s.Bounds.Left);
                this.Top = Screen.AllScreens.Min(s => s.Bounds.Top);
                this.Width = rOutput.Width;
                this.Height = rOutput.Height;
                // Store offset in class field
                ScreenOffset = new Size(this.Location);

                // Reset transparent color
                this.BackColor = this.TransparencyKey = TransparentColor;

                // Set opacity value
                this.Opacity = GestureSign.Common.Configuration.AppConfig.Opacity;

                // We have composition enabled, use standard mode
                RenderMethod = RenderMode.Standard;
            }
            else
            {
                // This window should not be on top, and should be hidden
                this.TopMost = false;

                // We don't have composition enabled, we need to render using compatiblity mode
                RenderMethod = RenderMode.Compatible;
            }

            // Update graphics object with new size
            InitializeGraphics();
        }
        private void InitializeGraphics()
        {
            if (RenderMethod == RenderMode.Standard)
            {
                try
                {
                    // Create graphics to allow drawing on surface form

                    SurfaceGraphics = this.CreateGraphics();
                    SurfaceGraphics.Clear(TransparentColor);
                    //SurfaceGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                }
                catch { }
            }
            else
            {
                // Create graphics for each display using compatibility mode
                CompatibilitySurfaces = Screen.AllScreens.Select(s => new CompatibilitySurface()
                    {
                        SurfaceGraphics = Graphics.FromHdc(CreateDC(null, s.DeviceName, null, IntPtr.Zero)),
                        Offset = new Size(s.Bounds.Location)
                    }).ToArray();
            }

            // Create pens using settings from configuration
            InitializePen();

        }

        private void InitializePen()
        {
            DrawingPen = new Pen(GestureSign.Common.Configuration.AppConfig.VisualFeedbackColor, GestureSign.Common.Configuration.AppConfig.VisualFeedbackWidth);
            DrawingPen.StartCap = DrawingPen.EndCap = LineCap.Round;
            DrawingPen.LineJoin = LineJoin.Round;
        }


        private PointF TranslatePoint(PointF Point)
        {
            // Add point offset
            return PointF.Subtract(Point, ScreenOffset);
        }

        private void ClearSurfaces()
        {
            if (RenderMethod == RenderMode.Standard)
            {
                try
                {
                    // Nothing to end if the graphics haven't been initialized
                    if (SurfaceGraphics != null)
                        SurfaceGraphics.Clear(TransparentColor);
                    //this.CreateGraphics().DrawImage(bmp, 0, 0);
                }
                catch
                {
                    InitializeGraphics();
                }
                LastStroke = null;
            }
            else
                InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
        }

        #endregion


        #region P/Invoke Methods

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        class Win32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Size
            {
                public Int32 cx;
                public Int32 cy;

                public Size(Int32 x, Int32 y)
                {
                    cx = x;
                    cy = y;
                }
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct BLENDFUNCTION
            {
                public byte BlendOp;
                public byte BlendFlags;
                public byte SourceConstantAlpha;
                public byte AlphaFormat;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct Point
            {
                public Int32 x;
                public Int32 y;

                public Point(Int32 x, Int32 y)
                {
                    this.x = x;
                    this.y = y;
                }
            }

            public const byte AC_SRC_OVER = 0;
            public const Int32 ULW_ALPHA = 2;
            public const byte AC_SRC_ALPHA = 1;

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

            [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr GetDC(IntPtr hWnd);

            [DllImport("gdi32.dll", ExactSpelling = true)]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObj);

            [DllImport("user32.dll", ExactSpelling = true)]
            public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            public static extern int DeleteDC(IntPtr hDC);

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            public static extern int DeleteObject(IntPtr hObj);

            [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
            public static extern int UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pptSrc, Int32 crKey, ref BLENDFUNCTION pblend, Int32 dwFlags);

            [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr ExtCreateRegion(IntPtr lpXform, uint nCount, IntPtr rgnData);
        }

        #endregion

        #region Base Method Overrides

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                CreateParams myParams = base.CreateParams;
                myParams.ExStyle = myParams.ExStyle | WS_EX_NOACTIVATE;
                return myParams;
            }
        }
        //protected override CreateParams CreateParams
        //{//重载窗体的CreateParams方法
        //    get
        //    {
        //        const int WS_MINIMIZEBOX = 0x00020000;  // Winuser.h中定义   
        //        CreateParams cp = base.CreateParams;
        //        cp.Style = cp.Style | WS_MINIMIZEBOX;   // 允许最小化操作
        //        cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
        //        return cp;
        //    }
        //}
        #endregion
    }
}