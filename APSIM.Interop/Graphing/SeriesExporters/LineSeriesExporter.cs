using System;
using APSIM.Services.Graphing;
using OxyPlot;
using Series = OxyPlot.Series.Series;

namespace APSIM.Interop.Graphing
{
    /// <summary>
    /// This class can export an apsim line series to an oxyplot series.
    /// </summary>
    internal class LineSeriesExporter : SeriesExporterBase<LineSeries>
    {
        /// <summary>
        /// Export the line series to an oxyplot series.
        /// </summary>
        /// <param name="series">The line series to be exported.</param>
        protected override Series Export(LineSeries series)
        {
            LineSeriesWithTracker result = new LineSeriesWithTracker();
            result.ItemsSource = GetDataPoints(series.X, series.Y);
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
            return result;
        }
    }
}