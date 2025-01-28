using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Graphing;
using OxyPlot.Series;
using Series = OxyPlot.Series.Series;
using MathNet.Numerics.Statistics;
using OxyPlot;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// This class can export an apsim box and whisker series to an oxyplot series.
    /// </summary>
    public class BoxWhiskerSeriesExporter : SeriesExporterBase<BoxWhiskerSeries>
    {
        /// <summary>
        /// Export the box and whisker series to an oxyplot series.
        /// </summary>
        /// <param name="series">The box and whisker series to be exported.</param>
        /// <param name="labels">Existing axis labels.</param>
        protected override (Series, AxisLabelCollection) Export(BoxWhiskerSeries series, AxisLabelCollection labels)
        {
            BoxPlotSeries result = new BoxPlotSeries();
            (result.Items, labels) = GetBoxPlotItems(series, labels);
            if (series.ShowOnLegend)
                result.Title = series.Title;

            // Line style/thickness
            result.LineStyle = series.LineConfig.Type.ToOxyPlotLineStyle();
            result.StrokeThickness = series.LineConfig.Thickness.ToOxyPlotThickness();
            // tbi: line colour configuration
            // result.Stroke = series.LineConfig.Colour.ToOxyPlotColour();

            // Marker type/thickness
            result.OutlierType = series.MarkerConfig.Type.ToOxyPlotMarkerType();
            result.OutlierSize = series.MarkerConfig.Size.ToOxyPlotMarkerSize() * series.MarkerConfig.SizeModifier;

            // Colour
            result.Stroke = OxyColors.Transparent;
            result.Fill = series.Colour.ToOxyColour();

            // todo: need to account for the possibility of string datatypes here.
            return (result, labels);
        }

        private (IList<BoxPlotItem>, AxisLabelCollection) GetBoxPlotItems(BoxWhiskerSeries series, AxisLabelCollection labels)
        {
            List<string> yLabels = labels.YLabels.ToList();
            IEnumerable<double> y = series.Y.Select(yi => GetDataPointValue(yi, yLabels));
            labels = new AxisLabelCollection(labels.XLabels, yLabels);

            double[] fiveNumberSummary = y.FiveNumberSummary();
            double min = fiveNumberSummary[0];
            double lowerQuartile = fiveNumberSummary[1];
            double median = fiveNumberSummary[2];
            double upperQuartile = fiveNumberSummary[3];
            double max = fiveNumberSummary[4];

            // fixme - this won't work with multiple box plot series on the same graph.
            double x = 0;

            IList<BoxPlotItem> items = new List<BoxPlotItem>()
            {
                new BoxPlotItem(x, min, lowerQuartile, median, upperQuartile, max)
            };
            return (items, labels);
        }
    }
}
