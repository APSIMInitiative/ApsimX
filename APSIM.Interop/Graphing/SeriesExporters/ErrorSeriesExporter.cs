using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Services.Graphing;
using Series = OxyPlot.Series.Series;
using ScatterErrorPoint = OxyPlot.Series.ScatterErrorPoint;

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
            var result = new OxyPlot.Series.ScatterErrorSeries();
            result.ItemsSource = GetDataPoints(series.X, series.Y);
            if (series.ShowOnLegend)
                result.Title = series.Title;

            // Line style/thickness
            // tbi: line colour configuration
            // result.Stroke = series.LineConfig.Colour.ToOxyPlotColour();

            // Marker type/thickness
            result.MarkerType = series.MarkerConfig.Type.ToOxyPlotMarkerType();
            result.MarkerSize = series.MarkerConfig.Size.ToOxyPlotMarkerSize() * series.MarkerConfig.SizeModifier;
            if (series.MarkerConfig.IsFilled())
                result.MarkerFill = series.Colour.ToOxyColour();

            result.ErrorBarStrokeThickness = series.BarThickness.ToOxyPlotThickness();
            

            // Colour
            result.ErrorBarColor = series.Colour.ToOxyColour();
            return result;
        }

        /// <summary>
        /// Get error points.
        /// </summary>
        /// <param name="x">X data.</param>
        /// <param name="y">Y data.</param>
        /// <param name="xError">X error data.</param>
        /// <param name="yError">Y error data.</param>
        private IEnumerable<ScatterErrorPoint> GetErrorDataPoints(IEnumerable<object> x, IEnumerable<object> y, IEnumerable<object> xError, IEnumerable<object> yError)
        {
            List<double> xValues = x.Select(GetDataPointValue).ToList();
            List<double> yValues = y.Select(GetDataPointValue).ToList();
            List<double> xErrorValues = xError.Select(GetDataPointValue).ToList();
            List<double> yErrorValues = yError.Select(GetDataPointValue).ToList();
            if (xValues.Count == yValues.Count)
            {
                if (xValues.Count == xErrorValues.Count && xValues.Count == yErrorValues.Count)
                {
                    // We have error data for both x and y series.
                    List<ScatterErrorPoint> points = new List<ScatterErrorPoint>();
                    for (int i = 0; i < xValues.Count; i++)
                        if (!double.IsNaN(xValues[i]) && !double.IsNaN(yValues[i]) && !double.IsNaN(xErrorValues[i]) && !double.IsNaN(yErrorValues[i]))
                            points.Add(new ScatterErrorPoint(xValues[i], yValues[i], xErrorValues[i], yErrorValues[i], 0));
                    return points;
                }
                else if (xValues.Count == xErrorValues.Count)
                {
                    // We have error data for x series.
                    List<ScatterErrorPoint> points = new List<ScatterErrorPoint>();
                    for (int i = 0; i < xValues.Count; i++)
                        if (!double.IsNaN(xValues[i]) && !double.IsNaN(yValues[i]) && !double.IsNaN(xErrorValues[i]))
                            points.Add(new ScatterErrorPoint(xValues[i], yValues[i], xErrorValues[i], 0, 0));
                    return points;
                }
                else if (yValues.Count == yErrorValues.Count)
                {
                    // We have error data for y series.
                    List<ScatterErrorPoint> points = new List<ScatterErrorPoint>();
                    for (int i = 0; i < xValues.Count; i++)
                        if (!double.IsNaN(xValues[i]) && !double.IsNaN(yValues[i]) && !double.IsNaN(xErrorValues[i]))
                            points.Add(new ScatterErrorPoint(xValues[i], yValues[i], 0, yErrorValues[i], 0));
                    return points;
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
                    return xValues.Zip(yValues, (xi, yi) => new ScatterErrorPoint(xi, yi, 0, 0));
                }
            }
            else
                throw new ArgumentException($"X and Y series are of different lengths ({xValues.Count} vs {yValues.Count})");
        }
    }
}
