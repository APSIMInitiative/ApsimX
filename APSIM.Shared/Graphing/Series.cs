using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// Contains options common to all graph series.
    /// </summary>
    public abstract class Series : ISeries
    {
        /// <summary>
        /// Name of the series.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Colour of the series.
        /// </summary>
        public Color Colour { get; private set; }

        /// <summary>
        /// Should this series appear in the legend?
        /// </summary>
        public bool ShowOnLegend { get; private set; }

        /// <summary>
        /// X-axis data.
        /// </summary>
        /// <value></value>
        public IEnumerable<object> X { get; private set; }

        /// <summary>
        /// Y-axis data.
        /// </summary>
        public IEnumerable<object> Y { get; private set; }

        /// <summary>
        /// Name of the x-axis field displayed by this series.
        /// </summary>
        public string XFieldName { get; private set; }

        /// <summary>
        /// Name of the y-axis field displayed by this series.
        /// </summary>
        public string YFieldName { get; private set; }

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
        public Series(string title, Color colour, bool showLegend, IEnumerable<double> x, IEnumerable<double> y, string xName, string yName) : this(title, colour, showLegend, x.Cast<object>(), y.Cast<object>(), xName, yName)
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
        /// <param name="xName">Name of the x-axis field displayed by this series.</param>
        /// <param name="yName">Name of the y-axis field displayed by this series.</param>
        public Series(string title, Color colour, bool showLegend, IEnumerable<object> x, IEnumerable<object> y, string xName, string yName)
        {
            if (x == null)
                throw new ArgumentNullException(nameof(x));
            if (y == null)
                throw new ArgumentNullException(nameof(y));
            int nx = x.Count();
            int ny = y.Count();
            if (nx != ny)
                throw new ArgumentException($"X data is of different length ({nx}) to y data ({ny})");
            Title = title;
            Colour = colour;
            ShowOnLegend = showLegend;
            X = x;
            Y = y;
            XFieldName = xName;
            YFieldName = yName;
        }
    }
}
