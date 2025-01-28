using System;
using Series = OxyPlot.Series.Series;
using System.Collections.Generic;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// Extensions for the <see cref="Series"/> class.
    /// </summary>
    public static class SeriesExtensions
    {
        /// <summary>
        /// The default set of series exporters.
        /// </summary>
        /// <remarks>
        /// todo: Expose functionality for adding custom exporters to this list.
        /// </remarks>
        private static readonly IEnumerable<ISeriesExporter> seriesExporters = GetSeriesExporters();

        /// <summary>
        /// This is the default list of series exporters.
        /// </summary>
        private static IEnumerable<ISeriesExporter> GetSeriesExporters()
        {
            return new ISeriesExporter[5]
            {
                new BarSeriesExporter(),
                new ErrorSeriesExporter(),
                new LineSeriesExporter(),
                new RegionSeriesExporter(),
                new BoxWhiskerSeriesExporter()
            };
        }

        /// <summary>
        /// Cache to speed up searches for series exporters.
        /// </summary>
        private static Dictionary<Type, ISeriesExporter> exportersLookup = new Dictionary<Type, ISeriesExporter>();

        /// <summary>
        /// Convert an apsim series to an oxyplot series.
        /// </summary>
        /// <remarks>
        /// When dealing with string data, the returned data points are ints
        /// which are indices into the axis labels list. Therefore we
        /// need to know about any existing axis labels.
        /// </remarks>
        /// <param name="series">The series to be converted.</param>
        /// <param name="labels">Existing axis labels on the graph.</param>
        public static ExportedSeries ToOxyPlotSeries(this APSIM.Shared.Graphing.Series series, AxisLabelCollection labels)
        {
            ISeriesExporter exporter = FindSeriesExporter(series);
            return exporter.Export(series, labels);
        }

        /// <summary>
        /// Find an <see cref="ISeriesExporter"/> instance capable of exporting the given series.
        /// </summary>
        /// <param name="series">The series to be exported.</param>
        private static ISeriesExporter FindSeriesExporter(APSIM.Shared.Graphing.Series series)
        {
            Type seriesType = series.GetType();

            // Try cache.
            if (exportersLookup.TryGetValue(seriesType, out ISeriesExporter exporter))
                return exporter;

            // Search through available exporters.
            foreach (ISeriesExporter seriesExporter in seriesExporters)
            {
                if (seriesExporter.CanExport(series))
                {
                    // Found a match - add it to the cache for next time and return it.
                    exportersLookup[seriesType] = seriesExporter;
                    return seriesExporter;
                }
            }

            // Unable to find a series exporter for this series type.
            throw new NotImplementedException($"Unknown series type {seriesType.Name}");
        }
    }
}
