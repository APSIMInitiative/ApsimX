using System;
using OxyPlot;
using OxyPlot.Series;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// A line series with a better tracker.
    /// </summary>
    public class LineSeriesWithTracker : LineSeries
    {
        /// <summary>
        /// Invoked when the user hovers over a series point.
        /// </summary>
        public event EventHandler<HoverPointArgs> OnHoverOverPoint;

        /// <summary>
        /// Name of the variable behind the X data.
        /// </summary>
        public string XFieldName { get; set; }

        /// <summary>
        /// Name of the variable behind the Y data.
        /// </summary>
        public string YFieldName { get; set; }

        /// <summary>
        /// Tracker is calling to determine the nearest point.
        /// </summary>
        /// <param name="point">The point clicked</param>
        /// <param name="interpolate">A value indicating whether interpolation should be used.</param>
        /// <returns>The return hit result</returns>
        public override TrackerHitResult GetNearestPoint(OxyPlot.ScreenPoint point, bool interpolate)
        {
            TrackerHitResult hitResult = base.GetNearestPoint(point, interpolate);

            if (hitResult != null && OnHoverOverPoint != null)
            {
                HoverPointArgs e = new HoverPointArgs();
                if (Title == null)
                    e.SeriesName = ToolTip;
                else
                    e.SeriesName = Title;
                
                e.X = hitResult.DataPoint.X;
                e.Y = hitResult.DataPoint.Y;
                OnHoverOverPoint.Invoke(this, e);
                if (e.HoverText != null)
                    hitResult.Series.TrackerFormatString = e.HoverText + "\n" + XFieldName + ": {2}\n" + YFieldName + ": {4}";
            }

            return hitResult;
        }
    }
}
