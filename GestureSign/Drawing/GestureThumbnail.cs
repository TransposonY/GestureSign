using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace HighSign.Drawing
{
	public class GestureThumbnail
	{
		#region Private Variables

		private Image _Image;
		private Graphics _Graphics;
		private PointF[] _Points;
		private Pen _DrawingPen;
		private Pen _ArrowPen;
		private Size _Size;
		private Size _ScaledSize;
		private bool _WithArrow;

		#endregion

		#region Public Properties

		public Size Size
		{
			get { return _Size; }
			set { _Size = value; }
		}

		public PointF[] Points
		{
			get { return _Points; }
			set { _Points = value; }
		}

		public Image Image
		{
			get
			{
				if (_Image == null)
					_Image = GenerateThumbnail();

				return _Image;
			}
		}

		#endregion

		#region Constructor

		public GestureThumbnail(PointF[] Points, Size Size, bool WithArrow)
		{
			_Points = Points;
			_Size = Size;
			_DrawingPen = new Pen(Properties.Settings.Default.MiniViewColor, 3);
			_DrawingPen.StartCap = _DrawingPen.EndCap = LineCap.Round;
			_ArrowPen = _DrawingPen.Clone() as Pen;

			// Create new large arrow cap
			AdjustableArrowCap arrowCap = new AdjustableArrowCap(3, 3, true);
			arrowCap.BaseCap = LineCap.Square;

			_ArrowPen.CustomEndCap = arrowCap;

			_WithArrow = WithArrow;
		}

		#endregion

		#region Private Methods

		private Image GenerateThumbnail()
		{
			if (_Points == null)
				throw new Exception("You must provide a gesture before trying to generate a thumbnail");

			if (_DrawingPen == null)
				throw new Exception("You must provide a drawing pen before trying to generate a thumbnail");

			// Create a bitmap object so we can draw gesture
			Bitmap bmpOutput = new Bitmap(_Size.Width, _Size.Height);

			if (_Points.Count() < 2)
				return bmpOutput;

			// Create new size object accounting for pen width
			Size szeAdjusted = new Size((int)Math.Floor(_Size.Width - _DrawingPen.Width - 1), (int)Math.Floor(_Size.Height - _DrawingPen.Width - 1));

			// Create graphics object from bitmap so we can manipulate it
			_Graphics = Graphics.FromImage((Image)bmpOutput);

			// Antialias line
			_Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			PointF[] _ScaledPoints = ScaleGesture(_Points, szeAdjusted.Width - 10, szeAdjusted.Height - 10);

			// Create variable to hold last point drawn
			PointF lastPoint = _ScaledPoints.First();

			// Define size that will mark the offset to center the gesture
			int iLeftOffset = (int)Math.Floor((double)((_Size.Width / 2) - (_ScaledSize.Width / 2)));
			int iTopOffset = (int)Math.Floor((double)((_Size.Height / 2) - (_ScaledSize.Height / 2)));
			Size sizOffset = new Size(iLeftOffset, iTopOffset);

			for (int i = 0; i < _ScaledPoints.Count(); i++)
			{
				if (_WithArrow)
				{
					if (i < _ScaledPoints.Count() - 5)
					{
						_Graphics.DrawLine(_DrawingPen, PointF.Add(lastPoint, sizOffset), PointF.Add(_ScaledPoints[i], sizOffset));
					}
					else
					{
						// Draw arrow on the end of the gesture
						_Graphics.DrawLine(_ArrowPen, PointF.Add(lastPoint, sizOffset), PointF.Add(_ScaledPoints.Last(), sizOffset));

						break;
					}
				}
				else
				{
					_Graphics.DrawLine(_DrawingPen, PointF.Add(lastPoint, sizOffset), PointF.Add(_ScaledPoints[i], sizOffset));
				}

				lastPoint = _ScaledPoints[i];
			}

			return (Image)bmpOutput;
		}

		private PointF[] ScaleGesture(PointF[] Input, int Width, int Height)
		{
			// Create generic list of points to hold scaled stroke
			List<PointF> ScaledStroke = new List<PointF>();

			// Get total width and height of gesture
			float fGestureOffsetLeft = Input.Min(i => i.X);
			float fGestureOffsetTop = Input.Min(i => i.Y);
			float fGestureWidth = Input.Max(i => i.X) - fGestureOffsetLeft;
			float fGestureHeight = Input.Max(i => i.Y) - fGestureOffsetTop;

			// Get each scale ratio
			double dScaleX = (double)Width / fGestureWidth;
			double dScaleY = (double)Height / fGestureHeight;

			// Scale on the longest axis
			if (fGestureWidth >= fGestureHeight)
			{
				// Scale on X axis
				// Clear current scaled stroke
				ScaledStroke.Clear();

				foreach (PointF currentPoint in Input)
					ScaledStroke.Add(new PointF((float)((currentPoint.X - fGestureOffsetLeft) * dScaleX), (float)((currentPoint.Y - fGestureOffsetTop) * dScaleX)));

				// Calculate new gesture width and height
				_ScaledSize = new Size((int)Math.Floor(fGestureWidth * dScaleX), (int)Math.Floor(fGestureHeight * dScaleX));
			}
			else
			{
				// Scale on X axis
				// Clear current scaled stroke
				ScaledStroke.Clear();

				foreach (PointF currentPoint in Input)
					ScaledStroke.Add(new PointF((float)((currentPoint.X - fGestureOffsetLeft) * dScaleY), (float)((currentPoint.Y - fGestureOffsetTop) * dScaleY)));

				// Calculate new gesture width and height
				_ScaledSize = new Size((int)Math.Floor(fGestureWidth * dScaleY), (int)Math.Floor(fGestureHeight * dScaleY));
			}

			return ScaledStroke.ToArray();
		}

		#endregion

		#region Static Methods

		public static Image Create(PointF[] Points, int Width, int Height, bool WithArrow)
		{
			return Create(Points, new Size(Width, Height), WithArrow);
		}

		public static Image Create(PointF[] Points, Size Size, bool WithArrow)
		{
			// Create instance of gesture thumbnail
			GestureThumbnail gThumb = new GestureThumbnail(Points, Size, WithArrow);
			return gThumb.Image;
		}

		#endregion
	}
}
