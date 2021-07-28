using System;
using System.Collections.Generic;
using System.Drawing;

namespace APSIM.Services.Graphing
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
        public LineSeries(string title,
                          Color colour,
                          bool showLegend,
                          IEnumerable<object> x,
                          IEnumerable<object> y,
                          Line line,
                          Marker marker) : base(title, colour, showLegend, x, y)
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
        public LineSeries(string title,
                          Color colour,
                          bool showLegend,
                          IEnumerable<double> x,
                          IEnumerable<double> y,
                          Line line,
                          Marker marker) : base(title, colour, showLegend, x, y)
        {
            LineConfig = line;
            MarkerConfig = marker;
        }
    }
}
