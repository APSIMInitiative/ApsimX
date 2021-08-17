using System.Collections.Generic;
using APSIM.Services.Graphing;
using Series = OxyPlot.Series.Series;
using OxyPlot;
using System;
using System.Linq;
using OxyPlot.Axes;

namespace APSIM.Interop.Graphing
{
    /// <summary>
    /// Base class for exporting apsim series to oxyplot series.
    /// </summary>
    internal abstract class SeriesExporterBase<T> : ISeriesExporter where T : ISeries
    {
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
        /// <param name="series">The series to be exported.</param>
        public Series Export(ISeries series)
        {
            return Export((T)series);
        }

        /// <summary>
        /// Export the series to an oxyplot series.
        /// </summary>
        /// <param name="series">The series to be exported.</param>
        protected abstract Series Export(T series);

        /// <summary>
        /// fixme: use classes for different data types
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected IEnumerable<DataPoint> GetDataPoints(IEnumerable<object> x, IEnumerable<object> y)
        {
            if (x == null)
                throw new ArgumentNullException("x data is null");
            if (y == null)
                throw new ArgumentNullException("y data is null");
            List<DataPoint> data = new List<DataPoint>();
            foreach ((object xi, object yi) in x.Zip(y))
                data.Add(new DataPoint(GetDataPointValue(xi), GetDataPointValue(yi)));
            return data;
        }

        /// <summary>
        /// fixme, use classes for different types of datapoint
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected double GetDataPointValue(object value)
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
            else
                // category axis - tbi
                throw new NotImplementedException();
        }
    }
}
