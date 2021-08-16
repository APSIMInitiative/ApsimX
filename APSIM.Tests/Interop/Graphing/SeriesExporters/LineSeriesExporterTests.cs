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
using Series = OxyPlot.Series.Series;

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
            Series output = exporter.Export(input);
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
            Series output = exporter.Export(input);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;
            Assert.AreEqual(0, series.ItemsSource.Count());
        }

        /// <summary>
        /// Ensure that an exception is thrown if x or y is null.
        /// </summary>
        [Test]
        public void TestNullData()
        {
            Line line = new Line(LineType.Solid, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.Square, MarkerSize.Normal, 1);

            Assert.Throws<ArgumentNullException>(() => new LineSeries("", Color.Blue, true, null, new double[0], line, marker));
            Assert.Throws<ArgumentNullException>(() => new LineSeries("", Color.Blue, true, new double[0], null, line, marker));
            Assert.Throws<ArgumentNullException>(() => new LineSeries("", Color.Blue, true, (double[])null, null, line, marker));
        }

        /// <summary>
        /// Ensure that attempting to create an oxyplot series in which the x/y
        /// fields are of different lengths.
        /// </summary>
        [Test]
        public void TestDataLengthMismatch()
        {
            Line line = new Line(LineType.Solid, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.Square, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", Color.Blue, true, new double[1], new double[2], line, marker);
            Assert.Throws<ArgumentException>(() => exporter.Export(inputSeries));
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
            Series output = exporter.Export(inputSeries);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;

            // Ensure that the line type matches the expected line type.
            Assert.AreEqual(expectedOutput, series.LineStyle);
        }

        /// <summary>
        /// Test all line thicknesses. This test doesn't test the absolute
        /// line thickness, but rather it ensures that "large" is thicker
        /// than "normal", which is thicker than "small".
        /// </summary>
        [Test]
        public void TestLineThicknesses()
        {
            double thick = GetExportedLineThickness(LineThickness.Normal);
            double thin = GetExportedLineThickness(LineThickness.Thin);
            Assert.Greater(thick, thin);
        }

        /// <summary>
        /// Crate a series with the specified line thickness, convert it to
        /// an oxyplot series and return the generated series' line thickness.
        /// </summary>
        /// <param name="lineThickness">Desired line thickness.</param>
        private double GetExportedLineThickness(LineThickness lineThickness)
        {
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.Solid, lineThickness);
            Marker marker = new Marker(MarkerType.FilledCircle, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", Color.Black, true, x, y, line, marker);

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;
            return series.StrokeThickness;
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
            Series output = exporter.Export(inputSeries);
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

        /// <summary>
        /// Create a series with the given marker size, conver it to an oxyplot
        /// series, and return the generated series' marker size.
        /// </summary>
        /// <param name="markerSize">Desired marker size.</param>
        private double GetExportedMarkerSize(MarkerSize markerSize)
        {
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.Solid, LineThickness.Normal);
            Marker marker = new Marker(MarkerType.FilledCircle, markerSize, 1);
            LineSeries inputSeries = new LineSeries("", Color.Black, true, x, y, line, marker);

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;

            return series.MarkerSize;
        }

        /// <summary>
        /// Ensure that the output series' title matches the input series' title.
        /// </summary>
        [Test]
        public void TestTitle()
        {
            // Create an apsim series with the given inputs.
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.FilledCircle, MarkerSize.Normal, 1);
            string[] titles = new[]
            {
                null,
                "",
                "A somewhat long title containing spaces"
            };
            foreach (string title in titles)
            {
                LineSeries inputSeries = new LineSeries(title, Color.Black, true, x, y, line, marker);
                Assert.AreEqual(title, exporter.Export(inputSeries).Title);
            }
        }

        /// <summary>
        /// Ensure that the output series' colour matches the input series' colour.
        /// </summary>
        [Test]
        public void TestColours()
        {
            foreach ((Color inColour, OxyColor outColour) in GetColourMap())
                TestColour(inColour, outColour);
        }

        /// <summary>
        /// Return a collection of tuples; with the item of the tuple
        /// being a System.Drawing.Color and the second item being an
        /// equivalent OxyColor instance.
        /// </summary>
        private IEnumerable<(Color, OxyColor)> GetColourMap()
        {
            return new List<(Color, OxyColor)>()
            {
                (Color.Red, OxyColors.Red),
                (Color.Blue, OxyColors.Blue),
                (Color.Green, OxyColors.Green),
                (Color.Black, OxyColors.Black),
                (Color.White, OxyColors.White)
            };
        }

        /// <summary>
        /// Create a series with the given colour, convert it to an oxyplot
        /// series, and ensure that the generated series' colour matches
        /// the expected output colour.
        /// </summary>
        /// <remarks>
        /// Note: this is testing the series' colour, not the marker colour.
        /// </remarks>
        /// <param name="inputColour">Input colour.</param>
        /// <param name="expectedOutput">Output colour.</param>
        [Test]
        private void TestColour(Color inputColour, OxyColor expectedOutput)
        {
            // Create an apsim series with the given inputs.
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.FilledCircle, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", inputColour, true, x, y, line, marker);

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;
            Assert.AreEqual(expectedOutput, series.Color);
        }

        /// <summary>
        /// Ensure that "filled" marker types result in the marker colour
        /// being set.
        /// </summary>
        [Test]
        public void TestFilledMarkers()
        {
            foreach ((Color inColour, OxyColor outColour) in GetColourMap())
            {
                TestMarkerColour(inColour, MarkerType.FilledCircle, outColour);
                TestMarkerColour(inColour, MarkerType.FilledDiamond, outColour);
                TestMarkerColour(inColour, MarkerType.FilledSquare, outColour);
                TestMarkerColour(inColour, MarkerType.FilledTriangle, outColour);
            }
        }

        /// <summary>
        /// Ensure that "unfilled" marker types result in the marker colour
        /// being set to "undefined".
        /// </summary>
        [Test]
        public void TestUnfilledMarkers()
        {
            foreach ((Color inColour, OxyColor _) in GetColourMap())
            {
                TestMarkerColour(inColour, MarkerType.Circle, OxyColors.Undefined);
                TestMarkerColour(inColour, MarkerType.Cross, OxyColors.Undefined);
                TestMarkerColour(inColour, MarkerType.Diamond, OxyColors.Undefined);
                TestMarkerColour(inColour, MarkerType.None, OxyColors.Undefined);
                TestMarkerColour(inColour, MarkerType.Plus, OxyColors.Undefined);
                TestMarkerColour(inColour, MarkerType.Square, OxyColors.Undefined);
                TestMarkerColour(inColour, MarkerType.Star, OxyColors.Undefined);
                TestMarkerColour(inColour, MarkerType.Triangle, OxyColors.Undefined);
            }
        }

        private void TestMarkerColour(Color inputColour, MarkerType markerType, OxyColor expectedOutput)
        {
            // Create an apsim series with the given inputs.
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(markerType, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", inputColour, true, x, y, line, marker);

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries);
            Assert.NotNull(output);
            Assert.True(output is OxyLineSeries);
            OxyLineSeries series = (OxyLineSeries)output;
            Assert.AreEqual(expectedOutput, series.MarkerFill);
        }
    }
}
