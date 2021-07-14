using System;
using APSIM.Services.Documentation;
using APSIM.Services.Graphing;
using OxyPlot;
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
                result = new DateTimeAxis();
            else
                result = new LinearAxis();
            // tbi: CategoryAxis (ie for bar graphs)

            result.Position = axis.Position.ToOxyAxisPosition();
            return result;
        }

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