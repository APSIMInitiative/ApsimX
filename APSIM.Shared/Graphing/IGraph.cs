using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Graphing;

namespace APSIM.Shared.Documentation
{
    /// <summary>
    /// Interface for a graph object.
    /// </summary>
    public interface IGraph : ITag
    {
        /// <summary>
        /// The series to be shown on the graph.
        /// </summary>
        /// <value></value>
        IEnumerable<Series> Series { get; }

        /// <summary>
        /// The x axis.
        /// </summary>
        Axis XAxis { get; }

        /// <summary>
        /// The y axis.
        /// </summary>
        Axis YAxis { get; }

        /// <summary>
        /// Legend configuration.
        /// </summary>
        ILegendConfiguration Legend { get; }

        /// <summary>
        /// Graph Title.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Graph Path.
        /// </summary>
        string Path { get; }
    }
}
