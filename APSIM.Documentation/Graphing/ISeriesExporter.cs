using APSIM.Shared.Graphing;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// An interface for a class which can export an apsim series
    /// into an oxyplot series.
    /// </summary>
    internal interface ISeriesExporter
    {
        /// <summary>
        /// Can this class export the given series?
        /// </summary>
        /// <param name="series">The series to be exported.</param>
        bool CanExport(ISeries series);

        /// <summary>
        /// Export the series to an oxyplot series.
        /// </summary>
        /// <remarks>
        /// When dealing with string data, the returned data points are ints
        /// which are indices into the axis labels list. Therefore we
        /// need to know about any existing axis labels.
        /// </remarks>
        /// <param name="series">The series to be exported.</param>
        /// <param name="existingAxisLabels">Existing axis labels on the graph.</param>
        ExportedSeries Export(ISeries series, AxisLabelCollection existingAxisLabels);
    }
}
