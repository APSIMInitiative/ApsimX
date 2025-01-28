using System;
using APSIM.Shared.Graphing;
using Series = OxyPlot.Series.Series;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// This class can export an apsim region series to an oxyplot series.
    /// </summary>
    internal class RegionSeriesExporter : SeriesExporterBase<RegionSeries>
    {
        /// <summary>
        /// Export the region series to an oxyplot series.
        /// </summary>
        /// <param name="series">The region series to be exported.</param>
        /// <param name="labels">Existing axis labels.</param>
        protected override (Series, AxisLabelCollection) Export(RegionSeries series, AxisLabelCollection labels)
        {
            throw new NotImplementedException();
        }
    }
}