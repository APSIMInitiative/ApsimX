using APSIM.Shared.Utilities;
using System.Collections.Generic;
using APSIM.Shared.Graphing;
using Series = OxyPlot.Series.Series;
using OxyPlot;
using System;
using System.Linq;
using OxyPlot.Axes;
using ApsimSeries = APSIM.Shared.Graphing.Series;

namespace APSIM.Documentation.Graphing
{
    /// <summary>
    /// Base class for exporting apsim series to oxyplot series.
    /// </summary>
    public abstract class SeriesExporterBase<T> : ISeriesExporter where T : ISeries
    {
        /// <summary>
        /// This struct is really just a wrapper around a value tuple.
        /// </summary>
        protected struct DataPointCollection
        {
            /// <summary>
            /// The actual data points.
            /// </summary>
            public IEnumerable<DataPoint> Points { get; private set; }

            /// <summary>
            /// The x-axis labels.
            /// </summary>
            public AxisLabelCollection Labels { get; private set; }

            /// <summary>
            /// Create a new <see cref="DataPointCollection"/> instance.
            /// </summary>
            /// <param name="dataPoints">The data points.</param>
            /// <param name="xLabels">The x-axis labels.</param>
            /// <param name="yLabels">The y-axis labels.</param>
            public DataPointCollection(IEnumerable<DataPoint> dataPoints, IEnumerable<string> xLabels, IEnumerable<string> yLabels)
            {
                Points = dataPoints;
                Labels = new AxisLabelCollection(xLabels, yLabels);
            }
        }

        /// <summary>
        /// Can this class export the given series?
        /// </summary>
        /// <param name="series">The series to be exported.</param>
        public bool CanExport(ISeries series)
        {
            return series is T;
        }

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
        public ExportedSeries Export(ISeries series, AxisLabelCollection existingAxisLabels)
        {
            (Series oxyPlotSeries, AxisLabelCollection labels) = Export((T)series, existingAxisLabels);

            AxisType? xAxisType = GetRequiredAxisType(series.X.FirstOrDefault());
            AxisType? yAxisType = GetRequiredAxisType(series.Y.FirstOrDefault());

            AxisRequirements xAxisRequirements = new AxisRequirements(xAxisType, series.XFieldName);
            AxisRequirements yAxisRequirements = new AxisRequirements(yAxisType, series.YFieldName);

            return new ExportedSeries(oxyPlotSeries, xAxisRequirements, yAxisRequirements, labels);
        }

        /// <summary>
        /// Export the series to an oxyplot series.
        /// </summary>
        protected abstract (Series, AxisLabelCollection) Export(T series, AxisLabelCollection existingAxisLabels);

        /// <summary>
        /// fixme: use classes for different data types
        /// </summary>
        /// <remarks>
        /// When dealing with string data, the returned data points are ints
        /// which are indices into the axis labels list. Therefore we
        /// need to know about any existing axis labels.
        /// </remarks>
        protected DataPointCollection GetDataPoints(IEnumerable<object> x, IEnumerable<object> y, AxisLabelCollection existingAxisLabels)
        {
            if (x == null)
                throw new ArgumentNullException("x data is null");
            if (y == null)
                throw new ArgumentNullException("y data is null");

            List<DataPoint> data = new List<DataPoint>();
            List<string> xAxisLabels = existingAxisLabels.XLabels.ToList();
            List<string> yAxisLabels = existingAxisLabels.YLabels.ToList();


            foreach ((object xi, object yi) in x.Zip(y))

                data.Add(new DataPoint(GetDataPointValue(xi, xAxisLabels), GetDataPointValue(yi, yAxisLabels)));

            return new DataPointCollection(data, xAxisLabels, yAxisLabels);
        }

        /// <summary>
        /// fixme, use classes for different types of datapoint
        /// </summary>
        protected double GetDataPointValue(object value, List<string> labels)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Type valueType = value.GetType();
            if (valueType == typeof(DateTime))
                return DateTimeAxis.ToDouble((DateTime)value);
            else if (valueType == typeof(double))
                return (double)value;
            else if (double.TryParse(value.ToString(), out double x))
                return x;
            else if (valueType == typeof(string))
            {
                int index = labels.IndexOf((string)value);
                if (index < 0)
                {
                    labels.Add((string)value);
                    index = labels.Count - 1;
                }
                return index;
            }
            else
                // category axis - tbi
                throw new NotImplementedException();
        }

        /// <summary>
        /// Get the axis requirements to render an object on a graph.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected AxisType? GetRequiredAxisType(object value)
        {
            if (value == null)
                return null;

            Type type = value.GetType();
            if (type == typeof(DateTime))
                return AxisType.DateTime;
            if (ReflectionUtilities.IsNumericType(type))
                return AxisType.Numeric;
            return AxisType.Category;
        }
    }
}
