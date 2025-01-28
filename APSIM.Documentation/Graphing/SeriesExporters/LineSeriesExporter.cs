using APSIM.Shared.Graphing;
using OxyPlot;
using Series = OxyPlot.Series.Series;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// This class can export an apsim line series to an oxyplot series.
    /// </summary>
    public class LineSeriesExporter : SeriesExporterBase<LineSeries>
    {
        /// <summary>
        /// Export the line series to an oxyplot series.
        /// </summary>
        /// <param name="series">The line series to be exported.</param>
        /// <param name="labels">Existing axis labels.</param>
        protected override (Series, AxisLabelCollection) Export(LineSeries series, AxisLabelCollection labels)
        {
            LineSeriesWithTracker result = new LineSeriesWithTracker();
            DataPointCollection data = GetDataPoints(series.X, series.Y, labels);
            result.ItemsSource = data.Points;

            if (series.ShowOnLegend)
                result.Title = series.Title;

            // Line style/thickness
            result.LineStyle = series.LineConfig.Type.ToOxyPlotLineStyle();
            result.StrokeThickness = series.LineConfig.Thickness.ToOxyPlotThickness();
            // tbi: line colour configuration
            // result.Stroke = series.LineConfig.Colour.ToOxyPlotColour();

            // Marker type/thickness
            result.MarkerType = series.MarkerConfig.Type.ToOxyPlotMarkerType();
            result.MarkerSize = series.MarkerConfig.Size.ToOxyPlotMarkerSize() * series.MarkerConfig.SizeModifier;
            if (series.MarkerConfig.IsFilled())
                result.MarkerFill = series.Colour.ToOxyColour();
            else
                result.MarkerFill = OxyColors.Undefined;

            // Colour
            result.Color = series.Colour.ToOxyColour();
            return (result, data.Labels);
        }
    }
}