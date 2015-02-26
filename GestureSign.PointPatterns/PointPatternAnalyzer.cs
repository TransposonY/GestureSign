using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace GestureSign.PointPatterns
{
    public class PointPatternAnalyzer
    {
        #region Constructors

        public PointPatternAnalyzer()
        {
            // Default precision to 100 (number or interpolation points)
            Precision = 100;
        }

        public PointPatternAnalyzer(IEnumerable<PointsPatternSet> PointPatternSet)
            : this()
        {
            // Instantiate PointPatternAnalyzer class with a PointPatternSet
            this.PointPatternSet = PointPatternSet;
        }

        public PointPatternAnalyzer(IEnumerable<PointsPatternSet> PointPatternSet, int Precision)
        {
            // Instantiate PointPatternAnalyzer class with a PointPatternSet and Precision
            this.PointPatternSet = PointPatternSet;
            this.Precision = Precision;
        }

        #endregion

        #region Public Properties

        public struct PointsPatternSet
        {
            public PointsPatternSet(string name, Point[] points)
            {
                Name = name;
                Points = points;
            }
            public string Name;
            public Point[] Points;
        }
        public int Precision { get; set; }
        public IEnumerable<PointsPatternSet> PointPatternSet { get; set; }//

        #endregion

        #region Public Methods

        public PointPatternMatchResult[] GetPointPatternMatchResults(Point[] Points)
        {
            // Create a list of PointPatternMatchResults to hold final results and group results of point pattern set comparison
            List<PointPatternMatchResult> comparisonResults = new List<PointPatternMatchResult>();
            List<PointPatternMatchResult> groupComparisonResults = new List<PointPatternMatchResult>();

            // Enumerate each point patterns grouped by name
            foreach (IGrouping<string, PointsPatternSet> pointPatternSet in PointPatternSet.GroupBy(pp => pp.Name))
            {
                // Clear out group comparison results from last group comparison
                groupComparisonResults.Clear();

                // Calculate probability of each point pattern in this group
                foreach (PointsPatternSet pointPatternCompareTo in pointPatternSet)
                    groupComparisonResults.Add(GetPointPatternMatchResult(pointPatternCompareTo, Points));

                // Add results of group comparison to final comparison results
                comparisonResults.Add(new PointPatternMatchResult(pointPatternSet.Key, groupComparisonResults.Average(ppmr => ppmr.Probability), groupComparisonResults.Sum(ppmr => ppmr.PointPatternSetCount)));
            }

            // Return comparison results ordered by highest probability
            return comparisonResults.ToArray();//.OrderByDescending(ppmr => ppmr.Probability).
        }

        public PointPatternMatchResult GetPointPatternMatchResult(PointsPatternSet CompareTo, Point[] Points)
        {
            PointPatternMatchResult comparisonResults = new PointPatternMatchResult();
            if (CompareTo.Points.Length == 1 || Points.Length <= 1)
            {
                if (Points.Length == CompareTo.Points.Length)
                {
                    comparisonResults.Probability = 100d;

                }
                else
                {
                    comparisonResults.Probability = 0d;
                }
            }
            else
            {
                double[] aDeltas = new double[Precision];
                double[] aCompareToAngles = PointPatternMath.GetPointArrayAngularMargins(PointPatternMath.GetInterpolatedPointArray(CompareTo.Points, Precision));
                double[] aCompareAngles = PointPatternMath.GetPointArrayAngularMargins(PointPatternMath.GetInterpolatedPointArray(Points, Precision));

                for (int i = 0; i <= aCompareToAngles.Length - 1; i++)
                    aDeltas[i] = PointPatternMath.GetAngularDelta(aCompareToAngles[i], aCompareAngles[i]);

                // Create new PointPatternMatchResult object to hold results from comparison
                comparisonResults.Probability = PointPatternMath.GetProbabilityFromAngularDelta(aDeltas.Average());
            }
            comparisonResults.Name = CompareTo.Name;
            comparisonResults.PointPatternSetCount = 1;
            // Return results of the comparison
            return comparisonResults;
        }

        #endregion
    }
}
