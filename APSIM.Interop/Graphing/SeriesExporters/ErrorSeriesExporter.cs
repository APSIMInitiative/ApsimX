using System;
using APSIM.Services.Graphing;
using Series = OxyPlot.Series.Series;

namespace APSIM.Interop.Graphing
{
    /// <summary>
    /// This class can export an apsim error series to an oxyplot series.
    /// </summary>
    internal class ErrorSeriesExporter : SeriesExporterBase<ErrorSeries>
    {
        /// <summary>
        /// Export the error series to an oxyplot series.
        /// </summary>
        /// <param name="series">The error series to be exported.</param>
        protected override Series Export(ErrorSeries series)
        {
            throw new NotImplementedException();
        }
    }
}