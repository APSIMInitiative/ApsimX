using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Graphing;
using Series = OxyPlot.Series.Series;
using ScatterErrorPoint = OxyPlot.Series.ScatterErrorPoint;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// This class can export an apsim error series to an oxyplot series.
    /// </summary>
    public class ErrorSeriesExporter : SeriesExporterBase<ErrorSeries>
    {
        /// <summary>
        /// Export the error series to an oxyplot series.
        /// </summary>
        /// <param name="series">The error series to be exported.</param>
        /// <param name="labels">Existing axis labels on the graph.</param>
        protected override (Series, AxisLabelCollection) Export(ErrorSeries series, AxisLabelCollection labels)
        {
            var result = new OxyPlot.Series.ScatterErrorSeries();
            (result.ItemsSource, labels) = GetErrorDataPoints(series.X, series.Y, series.XError, series.YError, labels);
            if (series.ShowOnLegend)
                result.Title = series.Title;

            // Line style/thickness
            // tbi: line colour configuration
            // result.Stroke = series.LineConfig.Colour.ToOxyPlotColour();

            // Marker type/thickness
            // fixme - this is all duplicated from LineSeries.
            result.MarkerType = series.MarkerConfig.Type.ToOxyPlotMarkerType();
            result.MarkerSize = series.MarkerConfig.Size.ToOxyPlotMarkerSize() * series.MarkerConfig.SizeModifier;
            if (series.MarkerConfig.IsFilled())
                result.MarkerFill = series.Colour.ToOxyColour();

            result.ErrorBarStrokeThickness = series.BarThickness.ToOxyPlotThickness();
            // TBI: stopper thickness            

            // Colour
            result.ErrorBarColor = series.Colour.ToOxyColour();

            return (result, labels);
        }

        /// <summary>
        /// Get error points.
        /// </summary>
        /// <param name="x">X data.</param>
        /// <param name="y">Y data.</param>
        /// <param name="xError">X error data.</param>
        /// <param name="yError">Y error data.</param>
        /// <param name="labels">Existing axis labels on the graph.</param>
        private (IEnumerable<ScatterErrorPoint>, AxisLabelCollection) GetErrorDataPoints(IEnumerable<object> x, IEnumerable<object> y, IEnumerable<object> xError, IEnumerable<object> yError, AxisLabelCollection labels)
        {
            List<string> xLabels = labels.XLabels.ToList();
            List<string> yLabels = labels.YLabels.ToList();

            List<double> xValues = x.Select(xi => GetDataPointValue(xi, xLabels)).ToList();
            List<double> yValues = y.Select(yi => GetDataPointValue(yi, yLabels)).ToList();
            List<double> xErrorValues = xError.Select(xi => GetDataPointValue(xi, xLabels)).ToList();
            List<double> yErrorValues = yError.Select(yi => GetDataPointValue(yi, yLabels)).ToList();

            labels = new AxisLabelCollection(xLabels, yLabels);

            if (xValues.Count == yValues.Count)
            {
                if (xValues.Count == xErrorValues.Count && xValues.Count == yErrorValues.Count)
                {
                    // We have error data for both x and y series.
                    List<ScatterErrorPoint> points = new List<ScatterErrorPoint>();
                    for (int i = 0; i < xValues.Count; i++)
                        if (!double.IsNaN(xValues[i]) && !double.IsNaN(yValues[i]) && !double.IsNaN(xErrorValues[i]) && !double.IsNaN(yErrorValues[i]))
                            points.Add(new ScatterErrorPoint(xValues[i], yValues[i], xErrorValues[i], yErrorValues[i], 0));
                    return (points, labels);
                }
                else if (xValues.Count == xErrorValues.Count)
                {
                    if (yErrorValues.Count != 0)
                        throw new ArgumentException($"Number of y error values ({yErrorValues.Count}) does not match number of datapoints or x error values ({xValues.Count})");
                    // We have error data for x series.
                    List<ScatterErrorPoint> points = new List<ScatterErrorPoint>();
                    for (int i = 0; i < xValues.Count; i++)
                        if (!double.IsNaN(xValues[i]) && !double.IsNaN(yValues[i]) && !double.IsNaN(xErrorValues[i]))
                            points.Add(new ScatterErrorPoint(xValues[i], yValues[i], xErrorValues[i], 0, 0));
                    return (points, labels);
                }
                else if (yValues.Count == yErrorValues.Count)
                {
                    if (xErrorValues.Count != 0)
                        throw new ArgumentException($"Number of x error values ({xErrorValues.Count}) does not match number of datapoints or y error values ({xValues.Count})");
                    // We have error data for y series.
                    List<ScatterErrorPoint> points = new List<ScatterErrorPoint>();
                    for (int i = 0; i < xValues.Count; i++)
                        if (!double.IsNaN(xValues[i]) && !double.IsNaN(yValues[i]) && !double.IsNaN(yErrorValues[i]))
                            points.Add(new ScatterErrorPoint(xValues[i], yValues[i], 0, yErrorValues[i], 0));
                    return (points, labels);
                }
                else
                {
                    // Throw error if we have nonzero x or y error data,
                    // but the number of items doesn't match.
                    if (xErrorValues.Count != 0)
                    {
                        if (yErrorValues.Count != 0)
                            throw new ArgumentException($"Number of x/y pairs ({xValues.Count}) does not match number of x error values ({xErrorValues.Count}) or number of y error values ({yErrorValues.Count})");
                        throw new ArgumentException($"Number of x/y pairs ({xValues.Count}) does not match number of x error values ({xErrorValues.Count})");
                    }
                    if (yErrorValues.Count != 0)
                        throw new ArgumentException($"Number of x/y pairs ({xValues.Count}) does not match number of y error values ({yErrorValues.Count})");

                    // If we reached here, there is no x or y error values.
                    // This raises the question of why this series is an error series
                    // given that there's no error data. The most likely cause is a
                    // programming error. However, we might as well just treat it as a
                    // normal series and plot the x/y data anyway.
                    IEnumerable<ScatterErrorPoint> points = xValues.Zip(yValues, (xi, yi) => new ScatterErrorPoint(xi, yi, 0, 0));
                    return (points, labels);
                }
            }
            else
                throw new ArgumentException($"X and Y series are of different lengths ({xValues.Count} vs {yValues.Count})");
        }
    }
}
