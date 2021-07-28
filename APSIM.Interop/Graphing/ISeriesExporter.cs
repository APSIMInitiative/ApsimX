using APSIM.Services.Graphing;
using Series = OxyPlot.Series.Series;

namespace APSIM.Interop.Graphing
{
    /// <summary>
    /// An interface for a class which can export an apsim series
    /// into an oxyplot series.
    /// </summary>
    public interface ISeriesExporter
    {
        /// <summary>
        /// Can this class export the given series?
        /// </summary>
        /// <param name="series">The series to be exported.</param>
        bool CanExport(ISeries series);

        /// <summary>
        /// Export the series to an oxyplot series.
        /// </summary>
        /// <param name="series">The series to be exported.</param>
        Series Export(ISeries series);
    }
}
