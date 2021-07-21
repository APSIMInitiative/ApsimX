using System;
using APSIM.Services.Graphing;
using Series = OxyPlot.Series.Series;

namespace APSIM.Interop.Graphing
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
        protected override Series Export(RegionSeries series)
        {
            throw new NotImplementedException();
        }
    }
}