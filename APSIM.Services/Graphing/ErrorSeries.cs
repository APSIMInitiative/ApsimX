using System.Collections.Generic;
using System.Drawing;

namespace APSIM.Services.Graphing
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
                           IEnumerable<object> yError) : base(title, colour, showLegend, x, y, line, marker)
        {
            BarThickness = barThickness;
            StopperThickness = stopperThickness;
            XError = xError;
            YError = yError;
        }
    }    
}
