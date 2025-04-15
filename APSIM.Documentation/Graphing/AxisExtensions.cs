using System;
using System.Collections.Generic;
using System.Diagnostics;
using OxyPlot.Axes;
using Axis = OxyPlot.Axes.Axis;
using AxisType = APSIM.Shared.Graphing.AxisType;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// Extension methods for the <see cref="APSIM.Shared.Graphing.Axis"/> type.
    /// </summary>
    public static class AxisExtensions
    {
        /// <summary>
        /// Convert the given apsim axis to an oxyplot <see cref="Axis"/>.
        /// </summary>
        public static Axis ToOxyPlotAxis(this APSIM.Shared.Graphing.Axis axis, AxisRequirements requirements, IEnumerable<string> labels)
        {
            if (requirements.AxisKind == null)
                throw new InvalidOperationException("Unable to create series - axis requirements unknown, possibly because no series have any data");
            Axis result = CreateAxis((AxisType)requirements.AxisKind);

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
            if (axis.Interval is double interval)
            {
                if (double.IsNaN(interval))
                    Debug.WriteLine("Axis interval is NaN");
                else
                {
                    if (requirements.AxisKind == AxisType.DateTime)
                        Debug.WriteLine("WARNING: Axis interval is set manually on a date axis - need to double check the implementation.");
                    result.MajorStep = interval;
                }
            }

            if (axis.Inverted)
            {
                result.StartPosition = 1;
                result.EndPosition = 0;
            }

            // There are many other options which could be exposed to the user.
            result.MinorTickSize = 0;
            result.AxisTitleDistance = 10;
            result.AxislineStyle = OxyPlot.LineStyle.Solid;

            if (requirements.AxisKind == AxisType.Category && result is CategoryAxis categoryAxis)
            {
                categoryAxis.LabelField = "Label";
                categoryAxis.Labels.AddRange(labels);
            }

            return result;
        }

        /// <summary>
        /// Create an axis for the given axis type.
        /// </summary>
        /// <param name="axisType">The type of axis to create.</param>
        private static Axis CreateAxis(AxisType axisType)
        {
            switch (axisType)
            {
                case AxisType.Category:
                    return new CategoryAxis();
                case AxisType.DateTime:
                    return new SmartDateTimeAxis();
                case AxisType.Numeric:
                    return new LinearAxis();
                default:
                    throw new NotImplementedException($"Unknown axis type {axisType}");
            }
        }
    }
}
