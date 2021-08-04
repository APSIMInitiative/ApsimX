using NUnit.Framework;
using System;
using System.Collections.Generic;
using APSIM.Services.Graphing;
using APSIM.Services.Documentation;
using APSIM.Services.Documentation.Extensions;
using System.Drawing;
using APSIM.Interop.Graphing;
using Moq;
using OxyPlot;
using LegendOrientation = APSIM.Services.Graphing.LegendOrientation;
using LegendPosition = APSIM.Services.Graphing.LegendPosition;
using MarkerType = APSIM.Services.Graphing.MarkerType;
using System.Linq;
using OxyPlot.Series;
using APSIM.Shared.Utilities;
using LineSeries = APSIM.Services.Graphing.LineSeries;
using OxyLineSeries = OxyPlot.Series.LineSeries;

namespace APSIM.Tests.Graphing.SeriesExporters
{
    /// <summary>
    /// Tests for <see cref="LineSeriesExporter"/>.
    /// </summary>
    [TestFixture]
    public class LineSeriesExporterTests
    {
        private LineSeriesExporter exporter = new LineSeriesExporter();

        [Test]
        public void TestSimpleCase()
        {
            IEnumerable<object> x = new object[] { 0d, 1d, 2d, 4d };
            IEnumerable<object> y = new object[] { 1d, 2d, 4d, 8d };
            Line line = new Line(LineType.Solid, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.Square, MarkerSize.Normal, 1);

            string title = "asdf";
            LineSeries input = new LineSeries(title, Color.Blue, true, x, y, line, marker);
            var output = exporter.Export(input);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;

            Assert.AreEqual(title, series.Title);
            Assert.AreEqual(4, series.ItemsSource.Count());

            // Marker style
            Assert.AreEqual(OxyPlot.MarkerType.Square, series.MarkerType);
            Assert.AreEqual(7, series.MarkerSize);

            // Line style
            Assert.AreEqual(OxyPlot.LineStyle.Solid, series.LineStyle);
            Assert.AreEqual(0.25, series.StrokeThickness);

            // Colours
            Assert.AreEqual(OxyColors.Blue, series.Color);
        }

        /// <summary>
        /// Test the case with no x/y data.
        /// </summary>
        [Test]
        public void TestNoData()
        {
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.Solid, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.Square, MarkerSize.Normal, 1);

            LineSeries input = new LineSeries("", Color.Blue, true, x, y, line, marker);
            var output = exporter.Export(input);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;
            Assert.AreEqual(0, series.ItemsSource.Count());
        }

        /// <summary>
        /// Ensure that the <see cref="LineSeriesExporter"/> generates oxyplot
        /// series with the correct line type for all allowed line types.
        /// </summary>
        [Test]
        public void TestLineTypes()
        {
            TestLineType(LineType.Dash, LineStyle.Dash);
            TestLineType(LineType.DashDot, LineStyle.DashDot);
            TestLineType(LineType.Dot, LineStyle.Dot);
            TestLineType(LineType.None, LineStyle.None);
            TestLineType(LineType.Solid, LineStyle.Solid);
        }

        /// <summary>
        /// Create an apsim series with the given input line type, then
        /// convert the apsim series to an oxyplot series and ensure that
        /// the output series' line type matches the expected output.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="expectedOutput"></param>
        private void TestLineType(LineType input, LineStyle expectedOutput)
        {
            // Create an apsim series with the given line type.
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(input, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.None, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", Color.Black, true, x, y, line, marker);

            // Convert the series to an oxyplot series.
            var output = exporter.Export(inputSeries);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;

            // Ensure that the line type matches the expected line type.
            Assert.AreEqual(expectedOutput, series.LineStyle);
        }

        /// <summary>
        /// Test the marker types and ensure that the "FilledX" marker types
        /// do indeed cause the markers to be filled with colour.
        /// </summary>
        [Test]
        public void TestMarkerTypes()
        {
            // Series with un-filled markers should have marker colour set to undefined.
            TestMarker(MarkerType.Circle, Color.Red, OxyPlot.MarkerType.Circle, OxyColors.Undefined);
            TestMarker(MarkerType.Cross, Color.Red, OxyPlot.MarkerType.Cross, OxyColors.Undefined);
            TestMarker(MarkerType.Diamond, Color.Red, OxyPlot.MarkerType.Diamond, OxyColors.Undefined);
            TestMarker(MarkerType.None, Color.Red, OxyPlot.MarkerType.None, OxyColors.Undefined);
            TestMarker(MarkerType.Plus, Color.Red, OxyPlot.MarkerType.Plus, OxyColors.Undefined);
            TestMarker(MarkerType.Square, Color.Red, OxyPlot.MarkerType.Square, OxyColors.Undefined);
            TestMarker(MarkerType.Star, Color.Red, OxyPlot.MarkerType.Star, OxyColors.Undefined);
            TestMarker(MarkerType.Triangle, Color.Red, OxyPlot.MarkerType.Triangle, OxyColors.Undefined);

            // The series with "FilledX" marker type should have the correct colour.
            Color colourIn = Color.Red;
            OxyColor colourOut = OxyColors.Red;
            TestMarker(MarkerType.FilledCircle, colourIn, OxyPlot.MarkerType.Circle, colourOut);
            TestMarker(MarkerType.FilledDiamond, colourIn, OxyPlot.MarkerType.Diamond, colourOut);
            TestMarker(MarkerType.FilledSquare, colourIn, OxyPlot.MarkerType.Square, colourOut);
            TestMarker(MarkerType.FilledTriangle, colourIn, OxyPlot.MarkerType.Triangle, colourOut);
        }

        /// <summary>
        /// Creates a series with the given colour and marker type, and ensures that the
        /// output series' marker fill and type match the given expected outputs.
        /// </summary>
        /// <param name="input">Marker type which the constructed apsim series should use.</param>
        /// <param name="inputColour">Colour of the apsim series.</param>
        /// <param name="expectedOutput">Expected marker type of the output oxyplot series.</param>
        /// <param name="expectedColour">Expected marker colour of the output oxyplot series.</param>
        private void TestMarker(MarkerType input, Color inputColour, OxyPlot.MarkerType expectedOutput, OxyColor expectedColour)
        {
            // Create an apsim series with the given inputs.
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(input, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", inputColour, true, x, y, line, marker);

            // Convert the series to an oxyplot series.
            var output = exporter.Export(inputSeries);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;

            // Ensure that the oxyplot series' marker type and fill match the expected values.
            Assert.AreEqual(expectedOutput, series.MarkerType);
            Assert.AreEqual(expectedColour, series.MarkerFill);
        }

        /// <summary>
        /// Test conversion of marker sizes. I've opted to not test the absolute
        /// generated sizes, but rather to test relative sizes - ie to ensure
        /// that the "small" size generates smaller markers than the "large" size.
        /// </summary>
        [Test]
        public void TestMarkerSizes()
        {
            double verySmall = GetExportedMarkerSize(MarkerSize.VerySmall);
            double small = GetExportedMarkerSize(MarkerSize.Small);
            double normal = GetExportedMarkerSize(MarkerSize.Normal);
            double large = GetExportedMarkerSize(MarkerSize.Large);
            Assert.Greater(large, normal);
            Assert.Greater(normal, small);
            Assert.Greater(small, verySmall);
        }

        private double GetExportedMarkerSize(MarkerSize markerSize)
        {
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.Solid, LineThickness.Normal);
            Marker marker = new Marker(MarkerType.FilledCircle, markerSize, 1);
            LineSeries inputSeries = new LineSeries("", Color.Black, true, x, y, line, marker);
            // Convert the series to an oxyplot series.
            var output = exporter.Export(inputSeries);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;
            return series.MarkerSize;
        }
    }
}
