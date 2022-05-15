using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace APSIM.Shared.Graphing
{
    /// <summary>
    /// A series which shows error bars.
    /// </summary>
    public class ErrorSeries : LineSeries
    {
        /// <summary>
        /// Thickness of the error bar.
        /// </summary>
        public LineThickness BarThickness { get; private set; }

        /// <summary>
        /// Thickness of the stopper at the end of the error bar.
        /// </summary>
        public LineThickness StopperThickness { get; private set; }

        /// <summary>
        /// X Error data.
        /// </summary>
        public IEnumerable<object> XError { get; private set; }

        /// <summary>
        /// Y error data.
        /// </summary>
        public IEnumerable<object> YError { get; private set; }

        /// <summary>
        /// Initialise an ErrorSeries instance.
        /// </summary>
        /// <param name="title">Name of the series.</param>
        /// <param name="colour">Colour of the series.</param>
        /// <param name="showLegend">Should this series appear in the legend?</param>
        /// <param name="x">X-axis data.</param>
        /// <param name="y">Y-axis data.</param>
        /// <param name="line">The line settings for the graph (thickness, type, ...).</param>
        /// <param name="marker">The marker settings for the graph (size, shape, ...).</param>
        /// <param name="barThickness">Thickness of the error bars.</param>
        /// <param name="stopperThickness">Thickness of the stopper on the error bars.</param>
        /// <param name="xError">Error data for the x series.</param>
        /// <param name="yError">Error data for the y series.</param>
        /// <param name="xName">Name of the x-axis field displayed by this series.</param>
        /// <param name="yName">Name of the y-axis field displayed by this series.</param>
        public ErrorSeries(string title,
                           Color colour,
                           bool showLegend,
                           IEnumerable<object> x,
                           IEnumerable<object> y,
                           Line line,
                           Marker marker,
                           LineThickness barThickness,
                           LineThickness stopperThickness,
                           IEnumerable<object> xError,
                           IEnumerable<object> yError,
                           string xName,
                           string yName) : base(title, colour, showLegend, x, y, line, marker, xName, yName)
        {
            BarThickness = barThickness;
            StopperThickness = stopperThickness;
            XError = xError ?? Enumerable.Empty<object>();
            YError = yError ?? Enumerable.Empty<object>();
        }

        /// <summary>
        /// Initialise an ErrorSeries instance.
        /// </summary>
        /// <param name="title">Name of the series.</param>
        /// <param name="colour">Colour of the series.</param>
        /// <param name="showLegend">Should this series appear in the legend?</param>
        /// <param name="x">X-axis data.</param>
        /// <param name="y">Y-axis data.</param>
        /// <param name="line">The line settings for the graph (thickness, type, ...).</param>
        /// <param name="marker">The marker settings for the graph (size, shape, ...).</param>
        /// <param name="barThickness">Thickness of the error bars.</param>
        /// <param name="stopperThickness">Thickness of the stopper on the error bars.</param>
        /// <param name="xError">Error data for the x series.</param>
        /// <param name="yError">Error data for the y series.</param>
        /// <param name="xName">Name of the x-axis field displayed by this series.</param>
        /// <param name="yName">Name of the y-axis field displayed by this series.</param>
        public ErrorSeries(string title,
                           Color colour,
                           bool showLegend,
                           IEnumerable<double> x,
                           IEnumerable<double> y,
                           Line line,
                           Marker marker,
                           LineThickness barThickness,
                           LineThickness stopperThickness,
                           IEnumerable<double> xError,
                           IEnumerable<double> yError,
                           string xName,
                           string yName) : base(title, colour, showLegend, x, y, line, marker, xName, yName)
        {
            BarThickness = barThickness;
            StopperThickness = stopperThickness;
            XError = xError?.Cast<object>() ?? Enumerable.Empty<object>();
            YError = yError?.Cast<object>() ?? Enumerable.Empty<object>();
        }
    }    
}
