using System.Collections.Generic;
using System.Drawing;

namespace APSIM.Services.Graphing
{
    /// <summary>
    /// A series used to draw a box and whisker plot.
    /// </summary>
    public class BoxWhiskerSeries : Series
    {
        /// <summary>
        /// Line type/thickness configuration.
        /// </summary>
        public Line LineConfig { get; private set; }

        /// <summary>
        /// Marker configuration for outliers.
        /// </summary>
        /// <value></value>
        public Marker MarkerConfig { get; private set; }

        /// <summary>
        /// Initialise a series instance.
        /// </summary>
        /// <param name="title">Name of the series.</param>
        /// <param name="colour">Colour of the series.</param>
        /// <param name="showLegend">Should this series appear in the legend?</param>
        /// <param name="x">X-axis data.</param>
        /// <param name="y">Y-axis data.</param>
        /// <param name="lineConfig">Line type/thickness.</param>
        /// <param name="markerConfig">Marker configuration for outliers.</param>
        public BoxWhiskerSeries(string title,
                                Color colour,
                                bool showLegend,
                                IEnumerable<object> x,
                                IEnumerable<object> y,
                                Line lineConfig,
                                Marker markerConfig) : base(title, colour, showLegend, x, y)
        {
            LineConfig = lineConfig;
            MarkerConfig = markerConfig;
        }
    }
}