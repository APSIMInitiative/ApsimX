using System;
using System.Collections.Generic;
using APSIM.Services.Graphing;

namespace APSIM.Services.Documentation
{
    /// <summary>
    /// A graph tag.
    /// </summary>
    /// <remarks>
    /// todo:
    /// - caption?
    /// </remarks>
    public class Graph : Tag
    {
        /// <summary>
        /// The series to be shown on the graph.
        /// </summary>
        /// <value></value>
        public IEnumerable<Series> Series { get; private set; }

        /// <summary>
        /// The axes on the graph.
        /// </summary>
        public IEnumerable<Axis> Axes { get; private set; }

        /// <summary>
        /// Legend configuration.
        /// </summary>
        public LegendConfiguration Legend { get; private set; }

        /// <summary>
        /// Constructs a graph tag instance.
        /// </summary>
        /// <param name="series">The series to be shown on the graph.</param>
        /// <param name="axes">The axes on the graph.</param>
        /// /// <param name="legend">Legend configuration.</param>
        public Graph(IEnumerable<Series> series, IEnumerable<Axis> axes, LegendConfiguration legend, uint indent = 0) : base(indent)
        {
            Series = series;
            Legend = legend;
            Axes = axes;
        }
    }
}
