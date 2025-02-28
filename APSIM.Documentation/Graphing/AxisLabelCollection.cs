using System.Collections.Generic;
using System.Linq;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// A collection of axis labels.
    /// </summary>
    public struct AxisLabelCollection
    {
        /// <summary>
        /// X-axis labels.
        /// </summary>
        /// <value></value>
        public IEnumerable<string> XLabels { get; private set; }

        /// <summary>
        /// Y-axis labels.
        /// </summary>
        /// <value></value>
        public IEnumerable<string> YLabels { get; private set; }

        /// <summary>
        /// Create a new <see cref="AxisLabelCollection"/> instance.
        /// </summary>
        /// <param name="xLabels">X-axis labels.</param>
        /// <param name="yLabels">Y-axis labels.</param>
        public AxisLabelCollection(IEnumerable<string> xLabels, IEnumerable<string> yLabels)
        {
            XLabels = xLabels;
            YLabels = yLabels;
        }

        /// <summary>
        /// Get an empty axis label collection.
        /// </summary>
        public static AxisLabelCollection Empty()
        {
            return new AxisLabelCollection(Enumerable.Empty<string>(), Enumerable.Empty<string>());
        }
    }
}
