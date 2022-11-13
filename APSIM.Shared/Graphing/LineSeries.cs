using System;
using System.Collections.Generic;
using System.Drawing;

namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// A line/marker series.
    /// </summary>
    public class LineSeries : Series
    {
        /// <summary>
        /// Line settings.
        /// </summary>
        public Line LineConfig { get; private set; }

        /// <summary>
        /// Marker settings.
        /// </summary>
        public Marker MarkerConfig { get; private set; }

        /// <summary>
        /// Initialise a series instance.
        /// </summary>
        /// <param name="title">Name of the series.</param>
        /// <param name="colour">Colour of the series.</param>
        /// <param name="showLegend">Should this series appear in the legend?</param>
        /// <param name="x">X-axis data.</param>
        /// <param name="y">Y-axis data.</param>
        /// <param name="line">The line settings for the graph (thickness, type, ...).</param>
        /// <param name="marker">The marker settings for the graph (size, shape, ...).</param>
        /// <param name="xName">Name of the x-axis field displayed by this series.</param>
        /// <param name="yName">Name of the y-axis field displayed by this series.</param>
        public LineSeries(string title,
                          Color colour,
                          bool showLegend,
                          IEnumerable<object> x,
                          IEnumerable<object> y,
                          Line line,
                          Marker marker,
                          string xName,
                          string yName) : base(title, colour, showLegend, x, y, xName, yName)
        {
            LineConfig = line;
            MarkerConfig = marker;
        }

        /// <summary>
        /// Initialise a series instance.
        /// </summary>
        /// <param name="title">Name of the series.</param>
        /// <param name="colour">Colour of the series.</param>
        /// <param name="showLegend">Should this series appear in the legend?</param>
        /// <param name="x">X-axis data.</param>
        /// <param name="y">Y-axis data.</param>
        /// <param name="line">The line settings for the graph (thickness, type, ...).</param>
        /// <param name="marker">The marker settings for the graph (size, shape, ...).</param>
        /// <param name="xName">Name of the x-axis field displayed by this series.</param>
        /// <param name="yName">Name of the y-axis field displayed by this series.</param>
        public LineSeries(string title,
                          Color colour,
                          bool showLegend,
                          IEnumerable<double> x,
                          IEnumerable<double> y,
                          Line line,
                          Marker marker,
                          string xName,
                          string yName) : base(title, colour, showLegend, x, y, xName, yName)
        {
            LineConfig = line;
            MarkerConfig = marker;
        }
    }
}
