using System;
using System.Diagnostics;
using OxyPlot.Axes;
using Axis = OxyPlot.Axes.Axis;
using AxisPosition = OxyPlot.Axes.AxisPosition;

namespace APSIM.Interop.Graphing
{
    public static class AxisExtensions
    {
        /// <summary>
        /// Convert the given apsim axis to an oxyplot <see cref="Axis"/>.
        /// </summary>
        /// <param name="graph">The graph to be converted.</param>
        public static Axis ToOxyPlotAxis(this APSIM.Services.Graphing.Axis axis)
        {
            Axis result;
            if (axis.DateTimeAxis)
            {
                result = new DateTimeAxis();
                ((DateTimeAxis)result).StringFormat = "dd/MM/yyyy";
            }
            else
                result = new LinearAxis();
            // tbi: CategoryAxis (ie for bar graphs)

            result.Position = axis.Position.ToOxyAxisPosition();
            result.Title = axis.Title;
            result.PositionAtZeroCrossing = axis.CrossesAtZero;

            if (axis.Minimum is double min)
            {
                if (double.IsNaN(min))
                    Debug.WriteLine("Axis minimum is NaN");
                else
                    result.Minimum = min;
            }
            if (axis.Maximum is double max)
            {
                if (double.IsNaN(max))
                    Debug.WriteLine("Axis maximum is NaN");
                else
                    result.Maximum = max;
            }

            if (axis.Inverted)
            {
                result.StartPosition = 1;
                result.EndPosition = 0;
            }
            else
            {
                result.StartPosition = 0;
                result.EndPosition = 1;
            }

            if (axis.Interval is double interval)
            {
                if (double.IsNaN(interval))
                    Debug.WriteLine("Axis interval is NaN");
                else
                {
                    if (axis.DateTimeAxis)
                    {
                        DateTimeIntervalType intervalType = (DateTimeIntervalType)interval;
                        ((DateTimeAxis)result).IntervalType = intervalType;
                        ((DateTimeAxis)result).MinorIntervalType = intervalType - 1;
                    }
                    else
                        result.MajorStep = interval;
                }
            }
            result.MinorTickSize = 0;
            // result.AxislineStyle = OxyPlot.LineStyle.Solid;
            // result.AxisTitleDistance = 10;
            result.AxisTickToLabelDistance = result.MajorTickSize;
            return result;
        }

        /// <summary>
        /// Convert an apsim AxisPosition to an OxyPlot AxisPosition.
        /// </summary>
        /// <param name="position">The axis position to be converted.</param>
        public static AxisPosition ToOxyAxisPosition(this APSIM.Services.Graphing.AxisPosition position)
        {
            switch (position)
            {
                case Services.Graphing.AxisPosition.Bottom:
                    return AxisPosition.Bottom;
                case Services.Graphing.AxisPosition.Left:
                    return AxisPosition.Left;
                case Services.Graphing.AxisPosition.Top:
                    return AxisPosition.Top;
                case Services.Graphing.AxisPosition.Right:
                    return AxisPosition.Right;
                default:
                    throw new NotImplementedException($"Unknown axis type: '{position}'");
            }
        }
    }
}