using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GestureSign.PointPatterns
{
    public static class PointPatternMath
    {
        #region Interpolation

        public static PointF[] GetInterpolatedPointArray(Point[] Points, int Segments)
        {
            // Create an empty return collection to store interpolated points
            List<PointF> interpolatedPoints = new List<PointF>(Segments);

            // Precalculate desired segment length and define helper variables
            double desiredSegmentLength = GetPointArrayLength(Points) / Segments;
            double dCurrSegmentLength, dTestSegmentLength, dIncToCurrentlength, dInterpolationPosition;
            PointF currentPoint;
            dCurrSegmentLength = 0; // Initialize to zero

            // Add first point in point pattern to return array and save it for use in the interpolation process
            PointF lastTestPoint = Points[0]; // Initialize to first point in point pattern
            interpolatedPoints.Add(lastTestPoint);

            // Enumerate points starting with second point (if any)
            for (int currentIndex = 1; currentIndex < Points.Length; currentIndex++)
            {
                // Store current index point in helper variable
                currentPoint = Points[currentIndex];

                // Calculate distance between last added point and current point in point pattern
                // and use calculated length to calculate test segment length for next point to add
                dIncToCurrentlength = GetDistance(lastTestPoint, currentPoint);
                dTestSegmentLength = dCurrSegmentLength + dIncToCurrentlength;

                // Does the test segment length meet our desired length requirement
                if (dTestSegmentLength < desiredSegmentLength)
                {
                    // Desired segment length has not been satisfied so we don't need to add an interpolated point
                    // save this test point and move on to next test point
                    dCurrSegmentLength = dTestSegmentLength;
                    lastTestPoint = currentPoint;
                    continue;
                }

                // Test segment length has met or exceeded our desired segment length
                // so lets calculate how far we overshot our desired segment length and calculate
                // an interpolation position to use to derive our new interpolation point
                dInterpolationPosition = (desiredSegmentLength - dCurrSegmentLength) * (1 / dIncToCurrentlength);

                // Use interpolation position to derive our new interpolation point
                PointF interpolatedPoint = GetInterpolatedPoint(lastTestPoint, currentPoint, dInterpolationPosition);
                interpolatedPoints.Add(interpolatedPoint);

                // Sometimes rounding errors cause us to attempt to add more points than the user has requested.
                // If we've reached our segment count limit, exit loop
                if (interpolatedPoints.Count == Segments)
                    break;

                // Store new interpolated point as last test point for use in next segment calculations
                // reset current segment length and jump back to the last index because we aren't done with original line segment
                lastTestPoint = interpolatedPoint;
                dCurrSegmentLength = 0;
                currentIndex--;
            }

            // Return interpolated point array
            return interpolatedPoints.ToArray();
        }

        public static PointF GetInterpolatedPoint(PointF LineStartPoint, PointF LineEndPoint, double InterpolatePosition)
        {
            // Create return point
            PointF pReturn = new PointF();

            // Calculate x and y of increment point
            pReturn.X = (float)((1 - InterpolatePosition) * LineStartPoint.X + InterpolatePosition * LineEndPoint.X);
            pReturn.Y = (float)((1 - InterpolatePosition) * LineStartPoint.Y + InterpolatePosition * LineEndPoint.Y);

            // Return new point
            return pReturn;
        }

        #endregion

        #region Angles

        public static double[] GetPointArrayAngularMargins(PointF[] PointArray)
        {
            // Create an empty collection of angles
            List<double> angularMargins = new List<double>(PointArray.Length);

            // Enumerate input point array starting with second point and calculate angular margin
            for (int currentIndex = 1; currentIndex < PointArray.Length; currentIndex++)
                angularMargins.Add(GetAngularGradient(PointArray[currentIndex - 1], PointArray[currentIndex]));

            // Return angular margins array
            return angularMargins.ToArray();
        }

        public static double GetAngularGradient(PointF LineStartPoint, PointF LineEndPoint)
        {
            return Math.Atan2((LineEndPoint.Y - LineStartPoint.Y), (LineEndPoint.X - LineStartPoint.X));
        }

        public static double GetAngularDelta(double Angle1, double Angle2)
        {
            double retValue = Math.Abs(Angle1 - Angle2);

            if (retValue > Math.PI)
                retValue = Math.PI - (retValue - Math.PI);

            return retValue;
        }

        public static double GetProbabilityFromAngularDelta(double AngularDelta)
        {
            const double dScale = 31.830988618379067D;
            return Math.Abs(AngularDelta * dScale - 100);
        }

        public static double GetDegreeFromRadian(double Angle)
        {
            return Angle * (180.0 / Math.PI);
        }

        #endregion

        #region Distances

        public static double GetDistance(PointF LineStartPoint, PointF LineEndPoint)
        {
            return GetDistance(LineStartPoint.X, LineStartPoint.Y, LineEndPoint.X, LineEndPoint.Y);
        }

        public static double GetDistance(double X1, double Y1, double X2, double Y2)
        {
            double XD = X2 - X1;
            double YD = Y2 - Y1;
            return Math.Sqrt(XD * XD + YD * YD);
        }

        public static double GetPointArrayLength(Point[] Points)
        {
            // Create return variable to hold final calculated length
            double returnLength = 0;

            // Enumerate points in point pattern and get a sum of each line segments distances
            for (int currentIndex = 1; currentIndex < Points.Length; currentIndex++)
                returnLength += GetDistance(Points[currentIndex - 1], Points[currentIndex]);

            // Return calculated length
            return returnLength;
        }

        #endregion
    }
}
