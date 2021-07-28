using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Services.Graphing;

namespace APSIM.Services.Documentation
{
    /// <summary>
    /// Interface for a graph object.
    public interface IGraph : ITag
    {
        /// <summary>
        /// The series to be shown on the graph.
        /// </summary>
        /// <value></value>
        IEnumerable<Series> Series { get; }

        /// <summary>
        /// The axes on the graph.
        /// </summary>
        IEnumerable<Axis> Axes { get; }

        /// <summary>
        /// Legend configuration.
        /// </summary>
        ILegendConfiguration Legend { get; }

        /// <summary>
        /// Graph Title.
        /// </summary>
        string Title { get; }
    }
}
