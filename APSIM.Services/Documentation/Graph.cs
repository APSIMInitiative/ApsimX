using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Graph : ITag
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
        /// Graph Title.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Constructs a graph tag instance.
        /// </summary>
        /// <param name="title">Title of the graph.</param>
        /// <param name="series">The series to be shown on the graph.</param>
        /// <param name="axes">The axes on the graph.</param>
        /// <param name="legend">Legend configuration.</param>
        public Graph(string title, IEnumerable<Series> series, IEnumerable<Axis> axes, LegendConfiguration legend)
        {
            Title = title;
            Series = series;
            Legend = legend;
            Axes = axes;

            foreach (Axis axis in Axes)
            {
                IEnumerable<IEnumerable<object>> axisData = GetSeries(axis.Position);
                if (axisData?.FirstOrDefault()?.FirstOrDefault()?.GetType() == typeof(DateTime))
                    axis.DateTimeAxis = true;
            }
        }

        /// <summary>
        /// Get all data which would be rendered on the given axis (ie
        /// all x data for a top- or bottom-positioned axis, or all y
        /// data for a left- or right-positioned axis).
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private IEnumerable<IEnumerable<object>> GetSeries(AxisPosition position)
        {
            switch (position)
            {
                case AxisPosition.Bottom:
                case AxisPosition.Top:
                    return Series.Select(s => s.X);
                case AxisPosition.Left:
                case AxisPosition.Right:
                    return Series.Select(s => s.Y);
                default:
                    throw new NotImplementedException($"Unknown axis position {position}");
            }
        }
    }
}
