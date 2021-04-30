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

        public int Precision { get; set; }
        public IEnumerable<PointsPatternSet> PointPatternSet { get; set; }//

        #endregion

        #region Public Methods

        public PointPatternMatchResult[] GetPointPatternMatchResults(Point[] Points)
        {
            // Create a list of PointPatternMatchResults to hold final results and group results of point pattern set comparison
            List<PointPatternMatchResult> comparisonResults = new List<PointPatternMatchResult>(PointPatternSet.Count());
            var targetPattern = new PointsPatternSet(null, Points);

            // Enumerate each point patterns
            foreach (var pointPatternSet in PointPatternSet)
            {
                // Calculate probability of each point pattern 
                comparisonResults.Add(GetPointPatternMatchResult(pointPatternSet, targetPattern));
            }

            // Return comparison results ordered by highest probability
            return comparisonResults.ToArray();//.OrderByDescending(ppmr => ppmr.Probability).
        }

        public PointPatternMatchResult GetPointPatternMatchResult(PointsPatternSet compareTo, PointsPatternSet points)
        {
            PointPatternMatchResult comparisonResults = new PointPatternMatchResult();
            if (compareTo.Points.Length == 1 || points.Points.Length <= 1)
            {
                if (points.Points.Length == compareTo.Points.Length)
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
                double[] aCompareToAngles = compareTo.GetAngularMargins(Precision);
                double[] aCompareAngles = points.GetAngularMargins(Precision);

                for (int i = 0; i <= aCompareToAngles.Length - 1; i++)
                    aDeltas[i] = PointPatternMath.GetAngularDelta(aCompareToAngles[i], aCompareAngles[i]);

                // Create new PointPatternMatchResult object to hold results from comparison
                comparisonResults.Probability = PointPatternMath.GetProbabilityFromAngularDelta(aDeltas.Average());
            }
            comparisonResults.Name = compareTo.Name;
            // Return results of the comparison
            return comparisonResults;
        }

        #endregion
    }
}
