using NUnit.Framework;
using System;
using System.Collections.Generic;
using APSIM.Shared.Graphing;
using APSIM.Shared.Documentation;
using APSIM.Shared.Documentation.Extensions;
using System.Drawing;
using APSIM.Interop.Graphing;
using Moq;
using OxyPlot;
using LegendOrientation = APSIM.Shared.Graphing.LegendOrientation;
using LegendPosition = APSIM.Shared.Graphing.LegendPosition;
using MarkerType = APSIM.Shared.Graphing.MarkerType;
using System.Linq;
using OxyPlot.Series;
using APSIM.Shared.Utilities;
using LineSeries = APSIM.Shared.Graphing.LineSeries;
using OxyLineSeries = OxyPlot.Series.LineSeries;
using Series = OxyPlot.Series.Series;

namespace UnitTests.Graphing.SeriesExporters
{
    /// <summary>
    /// Tests for <see cref="LineSeriesExporter"/>.
    /// </summary>
    [TestFixture]
    public class LineSeriesExporterTests
    {
        private LineSeriesExporter exporter = new LineSeriesExporter();

        /// <summary>
        /// This test creates a series and converts it to an oxyplot series,
        /// then checks everything about the generated series.
        /// </summary>
        /// <remarks>
        /// This is probably unnecessary. Each individual component is tested
        /// in isolation in the other tests.
        /// </remarks>
        [Test]
        public void TestSimpleCase()
        {
            IEnumerable<object> x = new object[] { 0d, 1d, 2d, 4d };
            IEnumerable<object> y = new object[] { 1d, 2d, 4d, 8d };
            Line line = new Line(LineType.Solid, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.Square, MarkerSize.Normal, 1);

            string title = "asdf";
            LineSeries input = new LineSeries(title, Color.Blue, true, x, y, line, marker, "", "");
            Series output = exporter.Export(input, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
            OxyLineSeries series = (OxyLineSeries)output;

            Assert.That(series.Title, Is.EqualTo(title));
            Assert.That(series.ItemsSource.Count(), Is.EqualTo(4));

            // Marker style
            Assert.That(series.MarkerType, Is.EqualTo(OxyPlot.MarkerType.Square));
            Assert.That(series.MarkerSize, Is.EqualTo(7));

            // Line style
            Assert.That(series.LineStyle, Is.EqualTo(OxyPlot.LineStyle.Solid));
            Assert.That(series.StrokeThickness, Is.EqualTo(0.25));

            // Colours
            Assert.That(series.Color, Is.EqualTo(OxyColors.Blue));
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

            LineSeries input = new LineSeries("", Color.Blue, true, x, y, line, marker, "", "");
            Series output = exporter.Export(input, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
            OxyLineSeries series = (OxyLineSeries)output;
            Assert.That(series.ItemsSource.Count(), Is.EqualTo(0));
        }

        /// <summary>
        /// Ensure that an exception is thrown if x or y is null.
        /// </summary>
        [Test]
        public void TestNullData()
        {
            Line line = new Line(LineType.Solid, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.Square, MarkerSize.Normal, 1);

            Assert.Throws<ArgumentNullException>(() => new LineSeries("", Color.Blue, true, null, new double[0], line, marker, "", ""));
            Assert.Throws<ArgumentNullException>(() => new LineSeries("", Color.Blue, true, new double[0], null, line, marker, "", ""));
            Assert.Throws<ArgumentNullException>(() => new LineSeries("", Color.Blue, true, (double[])null, null, line, marker, "", ""));
        }

        /// <summary>
        /// Ensure that attempting to create an oxyplot series in which the x/y
        /// fields are of different lengths results in an exception.
        /// </summary>
        [Test]
        public void TestDataLengthMismatch()
        {
            Line line = new Line(LineType.Solid, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.Square, MarkerSize.Normal, 1);
            Assert.Throws<ArgumentException>(() => new LineSeries("", Color.Blue, true, new double[1], new double[2], line, marker, "", ""));
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
            LineSeries inputSeries = new LineSeries("", Color.Black, true, x, y, line, marker, "", "");

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
            OxyLineSeries series = (OxyLineSeries)output;

            // Ensure that the line type matches the expected line type.
            Assert.That(series.LineStyle, Is.EqualTo(expectedOutput));
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
            Assert.That(thick, Is.GreaterThan(thin));
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
            LineSeries inputSeries = new LineSeries("", Color.Black, true, x, y, line, marker, "", "");

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
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
            LineSeries inputSeries = new LineSeries("", inputColour, true, x, y, line, marker, "", "");

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
            OxyLineSeries series = (OxyLineSeries)output;

            // Ensure that the oxyplot series' marker type and fill match the expected values.
            Assert.That(series.MarkerType, Is.EqualTo(expectedOutput));
            Assert.That(series.MarkerFill, Is.EqualTo(expectedColour));
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
            Assert.That(large, Is.GreaterThan(normal));
            Assert.That(normal, Is.GreaterThan(small));
            Assert.That(small, Is.GreaterThan(verySmall));
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
            LineSeries inputSeries = new LineSeries("", Color.Black, true, x, y, line, marker, "", "");

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
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
                LineSeries inputSeries = new LineSeries(title, Color.Black, true, x, y, line, marker, "", "");
                Assert.That(exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result.Title, Is.EqualTo(title));
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
        private void TestColour(Color inputColour, OxyColor expectedOutput)
        {
            // Create an apsim series with the given inputs.
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.FilledCircle, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", inputColour, true, x, y, line, marker, "", "");

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
            OxyLineSeries series = (OxyLineSeries)output;
            Assert.That(series.Color, Is.EqualTo(expectedOutput));
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

        /// <summary>
        /// Create a series with the given System.Drawing.Color and marker type,
        /// then convert to an oxyplot series and ensure that the generated series'
        /// marker colour matches the given colour.
        /// </summary>
        /// <param name="inputColour">Colour to use when creating the series.</param>
        /// <param name="markerType">Marker type for the created series.</param>
        /// <param name="expectedOutput">Expected colour of the output series.</param>
        private void TestMarkerColour(Color inputColour, MarkerType markerType, OxyColor expectedOutput)
        {
            // Create an apsim series with the given inputs.
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(markerType, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", inputColour, true, x, y, line, marker, "", "");

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
            OxyLineSeries series = (OxyLineSeries)output;
            Assert.That(series.MarkerFill, Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Test the 'show on legend' property. This should cause the series'
        /// title to be null.
        /// </summary>
        [Test]
        public void TestShowOnLegend()
        {
            string[] titles = new[]
            {
                null,
                "",
                "Series title"
            };
            foreach (string title in titles)
            {
                // Setting 'show on legend' to false should result in title being set to empty string.
                TestShowOnLegend(title, false, string.Empty);

                // Setting 'show on legend' to true should result in title being set to `title`.
                TestShowOnLegend(title, true, title);
            }
        }

        /// <summary>
        /// Create a series with the given title and 'show on legend' value.
        /// Then convert to an oxyplot series and ensure that the generated
        /// series' title matches the specified expected value.
        /// </summary>
        /// <param name="title">Input title.</param>
        /// <param name="showOnLegend">Input value for 'show on legend'.</param>
        /// <param name="expectedTitle">Expected title of the oxyplot series.</param>
        private void TestShowOnLegend(string title, bool showOnLegend, string expectedTitle)
        {
            // Create an apsim series with the given inputs.
            IEnumerable<object> x = Enumerable.Empty<object>();
            IEnumerable<object> y = Enumerable.Empty<object>();
            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.FilledCircle, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries(title, Color.Black, false, x, y, line, marker, "", "");

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
            OxyLineSeries series = (OxyLineSeries)output;
            Assert.That(series.Title, Is.Null);
        }

        /// <summary>
        /// Test a series containing DateTime data for both x- and y-values.
        /// Ensure that the generated series' values are correct (should be
        /// represented as a double).
        /// </summary>
        [Test]
        public void TestTwoDateSeries()
        {
            int n = 10;
            IEnumerable<DateTime> x = Enumerable.Range(1, n).Select(i => new DateTime(2000, 1, i));
            IEnumerable<DateTime> y = Enumerable.Range(2000, n).Select(i => new DateTime(i, 1, 1));

            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.FilledCircle, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", Color.Black, false, x.Cast<object>(), y.Cast<object>(), line, marker, "", "");

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
            OxyLineSeries series = (OxyLineSeries)output;

            Assert.That(series.ItemsSource.Count(), Is.EqualTo(n));
            double[] expectedX = new double[] { 36526, 36527, 36528, 36529, 36530, 36531, 36532, 36533, 36534, 36535 };
            double[] expectedY = new double[] { 36526, 36892, 37257, 37622, 37987, 38353, 38718, 39083, 39448, 39814 };
            int i = 0;
            foreach (DataPoint point in series.ItemsSource)
            {
                Assert.That(point.X, Is.EqualTo(expectedX[i]));
                Assert.That(point.Y, Is.EqualTo(expectedY[i]));
                i++;
            }
        }

        /// <summary>
        /// Test a series containing DateTime x-data and numeric (double) y-data.
        /// Ensure that the generated series' values are correct.
        /// </summary>
        [Test]
        public void TestOneDateSeries()
        {
            int n = 10;
            IEnumerable<DateTime> x = Enumerable.Range(2, n).Select(i => new DateTime(1900, i, 1));
            double[] y = Enumerable.Range(100, n).Select(i => Convert.ToDouble(i)).ToArray();

            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.FilledCircle, MarkerSize.Normal, 1);
            LineSeries inputSeries = new LineSeries("", Color.Black, false, x.Cast<object>(), y.Cast<object>(), line, marker, "", "");

            // Convert the series to an oxyplot series.
            Series output = exporter.Export(inputSeries, AxisLabelCollection.Empty()).Result;
            Assert.That(output, Is.Not.Null);
            Assert.That(output is OxyLineSeries, Is.True);
            OxyLineSeries series = (OxyLineSeries)output;

            Assert.That(series.ItemsSource.Count(), Is.EqualTo(n));
            double[] expectedX = new double[] { 33, 61, 92, 122, 153, 183, 214, 245, 275, 306 };
            int i = 0;
            foreach (DataPoint point in series.ItemsSource)
            {
                Assert.That(point.X, Is.EqualTo(expectedX[i]));
                Assert.That(point.Y, Is.EqualTo(y[i]));
                i++;
            }
        }

        /// <summary>
        /// Ensure that unsupported data types for series values cause
        /// an exception (rather than silently failing).
        /// </summary>
        [Test]
        public void TestUnsupportedDataTypes()
        {
            // These data types should all be valid.
            TestDataTypeValidity<short>(true);
            TestDataTypeValidity<ushort>(true);
            TestDataTypeValidity<int>(true);
            TestDataTypeValidity<uint>(true);
            TestDataTypeValidity<long>(true);
            TestDataTypeValidity<ulong>(true);
            TestDataTypeValidity<decimal>(true);
            TestDataTypeValidity<float>(true);
            TestDataTypeValidity<double>(true);
            TestDataTypeValidity<DateTime>(true);

            // These types are invalid and should result in exception.
            TestDataTypeValidity<bool>(false);
            TestDataTypeValidity<char>(false);
            TestDataTypeValidity<string>(false);
        }

        /// <summary>
        /// Ensure that the given data type is valid or invalid for series data.
        /// Ensure that the appropriate exception type is thrown for invalid
        /// data types, or that no exception is thrown for valid data types.
        /// </summary>
        /// <param name="valid">Is this data type valid.</param>
        /// <typeparam name="T">Data type.</typeparam>
        private void TestDataTypeValidity<T>(bool valid)
        {
            Line line = new Line(LineType.None, LineThickness.Thin);
            Marker marker = new Marker(MarkerType.FilledCircle, MarkerSize.Normal, 1);

            IEnumerable<T> emptyInvalid = Enumerable.Empty<T>();
            IEnumerable<T> populatedInvalid = new List<T>() { default(T) };
            IEnumerable<double> emptyValid = Enumerable.Empty<double>();
            IEnumerable<double> populatedValid = new double[1];

            // If the data type is an invalid nullable type, we should except an ArgumentNullException.
            // If the datatype is any other invalid type, we expect a NotImplementedException.
            Type exceptionType = default(T) == null ? typeof(ArgumentNullException) : typeof(NotImplementedException);

            IEnumerable<object> x = null;
            IEnumerable<object> y = null;
            TestDelegate createDefaultSeries = () => exporter.Export(new LineSeries("", Color.Black, false, x, y, line, marker, "", ""), AxisLabelCollection.Empty());

            string errorHelper = $"DataType = {typeof(T)}";

            // 1. Empty invalid x, valid y - no error should be thrown, because no data.
            x = emptyInvalid.Cast<object>();
            y = emptyValid.Cast<object>();
            Assert.DoesNotThrow(createDefaultSeries, errorHelper);

            // 2. Populated invalid x, valid y.
            x = populatedInvalid.Cast<object>();
            y = populatedValid.Cast<object>();
            if (valid)
                Assert.DoesNotThrow(createDefaultSeries, errorHelper);
            else
                Assert.Throws(exceptionType, createDefaultSeries, errorHelper);

            // 3. Empty invalid x, invalid y - no error should be thrown, because no data.
            x = emptyInvalid.Cast<object>();
            y = emptyInvalid.Cast<object>();
            Assert.DoesNotThrow(createDefaultSeries, errorHelper);

            // 4. Populated invalid x, invalid y.
            x = populatedInvalid.Cast<object>();
            y = populatedInvalid.Cast<object>();
            if (valid)
                Assert.DoesNotThrow(createDefaultSeries, errorHelper);
            else
                Assert.Throws(exceptionType, createDefaultSeries, errorHelper);

            // 5. Valid x, Empty invalid y - no error should be thrown, because no data.
            x = emptyValid.Cast<object>();
            y = emptyInvalid.Cast<object>();
            Assert.DoesNotThrow(createDefaultSeries, errorHelper);

            // 6. Valid x, Populated invalid y.
            x = populatedValid.Cast<object>();
            y = populatedInvalid.Cast<object>();
            if (valid)
                Assert.DoesNotThrow(createDefaultSeries, errorHelper);
            else
                Assert.Throws(exceptionType, createDefaultSeries, errorHelper);
        }
    }
}
