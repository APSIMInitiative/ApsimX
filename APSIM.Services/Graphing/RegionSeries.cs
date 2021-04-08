using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace APSIM.Services.Graphing
{
    /// <summary>
    /// Represents a shaded area on a graph.
    /// </summary>
    public class RegionSeries : Series
    {
        /// <summary>
        /// Upper bound for shaded area, relative to the x-axis.
        /// </summary>
        /// <value></value>
        public IEnumerable<object> X2 { get; private set; }

        /// <summary>
        /// Upper bound for shaded area, relative to the y-axis.
        /// </summary>
        public IEnumerable<object> Y2 { get; private set; }

        /// <summary>
        /// Initialise a series instance.
        /// </summary>
        /// <param name="title">Name of the series.</param>
        /// <param name="colour">Colour of the series.</param>
        /// <param name="showLegend">Should this series appear in the legend?</param>
        /// <param name="x">X-axis data.</param>
        /// <param name="y">Y-axis data.</param>
        public RegionSeries(string title, Color colour, bool showLegend, IEnumerable<object> x, IEnumerable<object> y, IEnumerable<object> x2, IEnumerable<object> y2) : base(title, colour, showLegend, x, y)
        {if (x == null)
                throw new ArgumentNullException(nameof(x));
            if (y == null)
                throw new ArgumentNullException(nameof(y));
            int nx = X.Count();
            int nx2 = x.Count();
            int ny2 = y.Count();
            if (nx2 != ny2)
                throw new ArgumentException($"X2 data is of different length ({nx2}) to Y2 data ({ny2})");
            if (nx2 != nx)
                throw new ArgumentException($"X2 data is of different length ({nx2}) to X data ({nx})");
            X2 = x2;
            Y2 = y2;
        }
    }
}