using System;
using APSIM.Services.Graphing;
using Series = OxyPlot.Series.Series;

namespace APSIM.Interop.Graphing
{
    /// <summary>
    /// This class can export an apsim bar series to an oxyplot series.
    /// </summary>
    internal class BarSeriesExporter : SeriesExporterBase<BarSeries>
    {
        /// <summary>
        /// Export the bar series to an oxyplot series.
        /// </summary>
        /// <param name="series">The bar series to be exported.</param>
        protected override Series Export(BarSeries series)
        {
            throw new NotImplementedException();
        }
    }
}