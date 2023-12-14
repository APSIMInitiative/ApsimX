using System;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.VariantTypes;
using OxyPlot;
using OxyPlot.Series;
using UserInterface.EventArguments;

namespace Utility
{
    /// <summary>
    /// An area series with a better tracker.
    /// </summary>
    public class AreaSeriesWithTracker : AreaSeries
    {
        /// <summary>
        /// Type of the x variable
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// Type of the x variable
        /// </summary>
        public Type XType { get; set; }

        /// <summary>
        /// Type of the y variable
        /// </summary>
        public Type YType { get; set; }

        /// <summary>
        /// Tracker is calling to determine the nearest point.
        /// </summary>
        /// <param name="point">The point clicked</param>
        /// <param name="interpolate">A value indicating whether interpolation should be used.</param>
        /// <returns>The return hit result</returns>
        public override TrackerHitResult GetNearestPoint(OxyPlot.ScreenPoint point, bool interpolate)
        {
            TrackerHitResult hitResult = base.GetNearestPoint(point, interpolate);

            if (hitResult != null)
            {
                string xInput = "{2}";
                string yInput = "{4}";

                if (XType == typeof(double))
                    xInput = MathUtilities.RoundSignificant(hitResult.DataPoint.X, 2).ToString();
                else if(XType == typeof(DateTime))
                    xInput = DateTime.FromOADate(hitResult.DataPoint.X).ToString();

                if (YType == typeof(double))
                    yInput = MathUtilities.RoundSignificant(hitResult.DataPoint.Y, 2).ToString();
                else if (YType == typeof(DateTime))
                    yInput = DateTime.FromOADate(hitResult.DataPoint.Y).ToString();

                hitResult.Series.TrackerFormatString = this.Title + "\n" + this.XAxis.Title + ": " + xInput + "\n" + this.YAxis.Title + ": " + yInput;
            }
            return hitResult;
        }
    }
}
