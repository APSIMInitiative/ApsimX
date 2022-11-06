using System.Collections.Generic;
using System.Drawing;

namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// Represents a series on a bar graph.
    /// </summary>
    public class BarSeries : Series
    {
        /// <summary>
        /// Colour used to fill in the rectangular area.
        /// If null, the outline colour will be used.
        /// </summary>
        private Color? fillColour = null;

        /// <summary>
        /// Colour used to fill in the rectangular area.
        /// </summary>
        public Color FillColour => fillColour ?? Colour;

        /// <summary>
        /// Initialise a series instance.
        /// </summary>
        /// <param name="title">Name of the series.</param>
        /// <param name="colour">Colour of the series.</param>
        /// <param name="showLegend">Should this series appear in the legend?</param>
        /// <param name="x">X-axis data.</param>
        /// <param name="y">Y-axis data.</param>
        /// <param name="xName">Name of the x-axis field displayed by this series.</param>
        /// <param name="yName">Name of the y-axis field displayed by this series.</param>
        public BarSeries(string title,
                          Color colour,
                          bool showLegend,
                          IEnumerable<object> x,
                          IEnumerable<object> y,
                          string xName,
                          string yName) : base(title, colour, showLegend, x, y, xName, yName)
        {
        }

        /// <summary>
        /// Initialise a series instance.
        /// </summary>
        /// <param name="title">Name of the series.</param>
        /// <param name="colour">Colour of the series.</param>
        /// <param name="showLegend">Should this series appear in the legend?</param>
        /// <param name="x">X-axis data.</param>
        /// <param name="y">Y-axis data.</param>
        /// <param name="fillColour">Colour used to fill in the rectangular area.</param>
        /// <param name="xName">Name of the x-axis field displayed by this series.</param>
        /// <param name="yName">Name of the y-axis field displayed by this series.</param>
        public BarSeries(string title,
                          Color colour,
                          bool showLegend,
                          IEnumerable<object> x,
                          IEnumerable<object> y,
                          Color fillColour,
                          string xName,
                          string yName) : base(title, colour, showLegend, x, y, xName, yName)
        {
            this.fillColour = fillColour;
        }
    }
}