using APSIM.Shared.Graphing;
using Series = OxyPlot.Series.Series;

namespace APSIM.Documentation.Graphing
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
        /// <param name="labels">Existing axis labels.</param>
        protected override (Series, AxisLabelCollection) Export(BarSeries series, AxisLabelCollection labels)
        {
            ColumnXYSeries result = new ColumnXYSeries();

            if (series.ShowOnLegend)
                result.Title = series.Title;

            result.FillColor = series.FillColour.ToOxyColour();
            result.StrokeColor = series.Colour.ToOxyColour();

            DataPointCollection data = GetDataPoints(series.X, series.Y, labels);
            result.ItemsSource = data.Points;

            return (result, data.Labels);
        }
    }
}
