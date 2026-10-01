using GestureSign.InputRecorder.Recording;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GestureSign.InputRecorder
{
    /// <summary>
    /// Draws the digitizer surface with the decoded finger/pen paths (one color per contact ID) and the
    /// contacts that are down right now.
    /// </summary>
    internal sealed class ContactView : Control
    {
        private static readonly Color[] Palette =
        {
            Color.RoyalBlue, Color.OrangeRed, Color.ForestGreen, Color.DarkViolet, Color.DarkGoldenrod,
            Color.DeepPink, Color.Teal, Color.SaddleBrown, Color.SlateGray, Color.Crimson,
        };

        public ContactView()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public DeviceMonitor Monitor { get; set; }

        /// <summary>Draw the current take's paths instead of the live ones.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ShowTake { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawRectangle(Pens.Silver, 0, 0, Width - 1, Height - 1);

            DeviceMonitor monitor = Monitor;
            if (monitor == null)
            {
                g.DrawString("No digitizer input yet: touch the trackpad, touch screen or pen.", Font, Brushes.DimGray, 6, 6);
                return;
            }

            ContactTracker paths = ShowTake ? monitor.Take : monitor.Live;
            string header = string.Format("{0} {1}   {2}   {3} reports/s   {4} slots/report\r\n{5}: {6} down now, max {7} together, {8} stroke(s){9}",
                monitor.Kind, monitor.VidPid, monitor.SurfaceText, monitor.ReportsPerSecond, monitor.Decoder?.ContactSlots ?? 0,
                ShowTake ? "Take" : "Live", monitor.Live.ActiveCount, paths.MaxActive, paths.Strokes.Count,
                monitor.Live.ButtonDown ? "   CLICK" : "");
            g.DrawString(header, Font, Brushes.Black, 6, 6);
            int top = 12 + 2 * Font.Height;

            if (monitor.Decoder == null)
            {
                g.DrawString("This collection has no decodable X/Y contacts; its raw reports are still recorded.", Font, Brushes.DarkRed, 6, top);
                return;
            }

            RectangleF surface = FitSurface(new RectangleF(8, top, Width - 16, Height - top - 8), monitor.Decoder.AspectRatio);
            if (surface.Width < 10 || surface.Height < 10)
                return;
            using (var fill = new SolidBrush(Color.FromArgb(245, 245, 247)))
                g.FillRectangle(fill, surface);
            g.DrawRectangle(Pens.Gray, surface.X, surface.Y, surface.Width, surface.Height);

            foreach (ContactStroke stroke in paths.Strokes)
            {
                Color color = Palette[(stroke.ContactId & int.MaxValue) % Palette.Length];
                PointF[] points = Map(stroke, surface);
                using (var pen = new Pen(color, 2.5f) { LineJoin = LineJoin.Round })
                {
                    if (points.Length > 1)
                        g.DrawLines(pen, points);
                    if (stroke.Ended)
                        g.DrawRectangle(pen, points[points.Length - 1].X - 3, points[points.Length - 1].Y - 3, 6, 6);
                }
                using (var brush = new SolidBrush(color))
                    g.FillEllipse(brush, points[0].X - 4, points[0].Y - 4, 8, 8);
            }

            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                foreach (ContactStroke stroke in monitor.Live.Active)
                {
                    Color color = Palette[(stroke.ContactId & int.MaxValue) % Palette.Length];
                    PointF p = MapPoint(stroke.Points[stroke.Points.Count - 1], surface);
                    using (var brush = new SolidBrush(Color.FromArgb(110, color)))
                        g.FillEllipse(brush, p.X - 13, p.Y - 13, 26, 26);
                    g.DrawString(stroke.ContactId.ToString(), Font, Brushes.Black, p, format);
                }
            }

            g.DrawString("dot: touch-down   square: lift   circle: finger down now", Font, Brushes.DimGray, surface.X + 4, surface.Bottom - Font.Height - 4);
        }

        private static RectangleF FitSurface(RectangleF available, float aspect)
        {
            if (available.Width <= 0 || available.Height <= 0 || aspect <= 0)
                return RectangleF.Empty;
            float width = available.Width, height = width / aspect;
            if (height > available.Height)
            {
                height = available.Height;
                width = height * aspect;
            }
            return new RectangleF(available.X + (available.Width - width) / 2, available.Y, width, height);
        }

        private static PointF[] Map(ContactStroke stroke, RectangleF surface)
        {
            var points = new PointF[stroke.Points.Count];
            for (int i = 0; i < points.Length; i++)
                points[i] = MapPoint(stroke.Points[i], surface);
            return points;
        }

        private static PointF MapPoint(PointF normalized, RectangleF surface)
        {
            return new PointF(surface.X + normalized.X * surface.Width, surface.Y + normalized.Y * surface.Height);
        }
    }
}
