using System;
using System.Collections.Generic;
using OxyPlot.Series;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// Encapsulates a series which can be displayed on an oxyplot graph.
    /// </summary>
    public class ExportedSeries
    {
        /// <summary></summary>
        public Series Result { get; private set; }
        /// <summary></summary>
        public AxisRequirements XAxisRequirements { get; private set; }
        /// <summary></summary>
        public AxisRequirements YAxisRequirements { get; private set; }
        /// <summary></summary>
        public AxisLabelCollection AxisLabels { get; private set; }

        /// <summary>
        /// Create a new <see cref="ExportedSeries"/> instance.
        /// </summary>
        /// <param name="series">The actual series.</param>
        /// <param name="xAxisRequirements">X axis requirements of the series.</param>
        /// <param name="yAxisRequirements">Y axis requirements of the series.</param>
        /// <param name="labels">Axis labels required by the series.</param>
        public ExportedSeries(Series series, AxisRequirements xAxisRequirements, AxisRequirements yAxisRequirements, AxisLabelCollection labels)
        {
            Result = series;
            XAxisRequirements = xAxisRequirements;
            YAxisRequirements = yAxisRequirements;
            AxisLabels = labels;
        }

        /// <summary>
        /// Check if this series is compatible with another series. That is,
        /// can the two series be displayed on the same graph. This could fail if,
        /// for instance, one series shows DateTime data on the x-axis but the other
        /// series shows numeric data on the x-axis.
        /// </summary>
        /// <param name="other">The other series to check for compatibility.</param>
        public void ThrowIfIncompatibleWith(ExportedSeries other)
        {
            try
            {
                XAxisRequirements.ThrowIfIncompatibleWith(other.XAxisRequirements);
                YAxisRequirements.ThrowIfIncompatibleWith(other.YAxisRequirements);
            }
            catch (Exception err)
            {
                throw new Exception($"Series {Result.Title} is incompatible with {other.Result.Title}", err);
            }
        }
    }
}
