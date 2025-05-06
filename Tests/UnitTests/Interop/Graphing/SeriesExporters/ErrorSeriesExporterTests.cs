using NUnit.Framework;
using System;
using System.Collections.Generic;
using APSIM.Shared.Graphing;
using APSIM.Shared.Documentation;
using APSIM.Shared.Documentation.Extensions;
using System.Drawing;
using APSIM.Documentation.Graphing;
using Moq;
using OxyPlot;
using LegendOrientation = APSIM.Shared.Graphing.LegendOrientation;
using LegendPosition = APSIM.Shared.Graphing.LegendPosition;
using MarkerType = APSIM.Shared.Graphing.MarkerType;
using System.Linq;
using OxyPlot.Series;
using APSIM.Shared.Utilities;

namespace UnitTests.Graphing.SeriesExporters
{
    /// <summary>
    /// Tests for <see cref="ErrorSeriesExporter"/>.
    /// </summary>
    [TestFixture]
    public class ErrorSeriesExporterTests
    {
        private ErrorSeriesExporter exporter = new ErrorSeriesExporter();

        [Test]
        public void TestSimpleCase()
        {
            IEnumerable<object> x = new object[] { 0d, 1d, 2d };
            IEnumerable<object> y = new object[] { 1d, 2d, 4d };
            IEnumerable<object> xerr = new object[] { 0.25, 0.5, 0.75 };
            IEnumerable<object> yerr = new object[] { 0.5, 0.25, 0.125 };
            Line line = new Line(LineType.Solid, LineThickness.Thin);
            LineThickness bar = LineThickness.Thin;
            LineThickness stopper = LineThickness.Normal;
            Marker marker = new Marker(MarkerType.Cross, MarkerSize.Normal, 1);

            ErrorSeries input = new ErrorSeries("asdf", Color.Blue, true, x, y, line, marker, bar, stopper, xerr, yerr, "", "");
            var output = exporter.Export(input, AxisLabelCollection.Empty()).Result;

            Assert.That(output, Is.Not.Null);
            Assert.That(output is ScatterErrorSeries, Is.True);
            ScatterErrorSeries errorSeries = (ScatterErrorSeries)output;
            Assert.That(errorSeries.Title, Is.EqualTo("asdf"));
            Assert.That(errorSeries.ItemsSource.Count(), Is.EqualTo(3));

            // Marker style
            Assert.That(errorSeries.MarkerType, Is.EqualTo(OxyPlot.MarkerType.Cross));
            Assert.That(errorSeries.MarkerSize, Is.EqualTo(7));

            // Line style TBI

            // Bar style
            Assert.That(errorSeries.ErrorBarStrokeThickness, Is.EqualTo(0.25));

            // TBI: stopper thickness

            // Colours
            Assert.That(errorSeries.ErrorBarColor, Is.EqualTo(OxyColors.Blue));
        }

        /// <summary>
        /// Test an error series with no x error data AND no y error.
        /// This should work, and all error values should be set to 0.
        /// </summary>
        [Test]
        public void NoXOrYError()
        {
            IEnumerable<double> x = new double[3] { 0, 1, 2 };
            IEnumerable<double> y = new double[3] { 0, 1, 1 };
            TestSeriesWithThisData(x, y, null, null);
        }

        /// <summary>
        /// Test a series with X error data, but no y error data.
        /// This should work, and all y error values should be set to 0.
        /// </summary>
        [Test]
        public void NoYError()
        {
            IEnumerable<double> x = new double[5] { 5, 4, 3, 2, 1 };
            IEnumerable<double> y = new double[5] { 1, 3, 6, 10, 15 };
            IEnumerable<double> xerr = new double[5] { 0.5, 0.25, 0.125, 0.0625, 0.03125 };
            TestSeriesWithThisData(x, y, xerr, null);
        }

        /// <summary>
        /// Test a series with Y error data, but no x error data.
        /// This should work, with all x error values set to 0.
        /// </summary>
        [Test]
        public void NoXError()
        {
            IEnumerable<double> x = new double[6] { 1, 3, 2, 4, 3, 5 };
            IEnumerable<double> y = new double[6] { 1, 2, 6, 24, 120, 720 };
            IEnumerable<double> yerr = new double[6] { 1, 2, 4, 8, 16, 32 };
            TestSeriesWithThisData(x, y, null, yerr);
        }

        /// <summary>
        /// Test a series with both X and Y error data.
        /// This should work with x and y error values populated naturally.
        /// </summary>
        [Test]
        public void XAndYError()
        {
            IEnumerable<double> x = new double[7] { 0, 1, 2, 3, 4, 5, 6 };
            IEnumerable<double> y = new double[7] { 1, 2, 6, 24, 120, 720, 5040 };
            IEnumerable<double> xerr = new double[7] { 1, 0.5, 0.25, 0.125, 0.0625, 0.03125, 0.015625 };
            IEnumerable<double> yerr = new double[7] { 1, 2, 4, 8, 16, 32, 64 };
            TestSeriesWithThisData(x, y, xerr, yerr);
        }

        /// <summary>
        /// Test the case where x and y data are of different lengths.
        /// </summary>
        [Test]
        public void TestXYLengthMismatch()
        {
            // Y is longer than x.
            IEnumerable<double> x = new double[1];
            IEnumerable<double> y = new double[2];
            ThrowsForAllXErrAndYErr<ArgumentException>(x, y);

            // X is longer than y.
            x = new double[2];
            y = new double[1];
            ThrowsForAllXErrAndYErr<ArgumentException>(x, y);
        }

        /// <summary>
        /// Test the case where x or why are null.
        /// </summary>
        [Test]
        public void TestXOrYNull()
        {
            // Test when X or Y is null.
            IEnumerable<double> input = new double[1];

            ThrowsForAllXErrAndYErr<ArgumentNullException>(input, null);
            ThrowsForAllXErrAndYErr<ArgumentNullException>(null, input);
            ThrowsForAllXErrAndYErr<ArgumentNullException>(null, null);
        }

        /// <summary>
        /// Test the case where the x error series is the wrong length.
        /// </summary>
        [Test]
        public void TestXErrLengthMismatch()
        {
            IEnumerable<double> x = new double[2];
            IEnumerable<double> y = new double[2];

            // xerr is not provided - should not throw.
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, null, null));
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, new double[0], null));

            // xerr is correct length - should not throw with any valid yerr.
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, new double[2], null));
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, new double[2], new double[0]));
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, new double[2], new double[2]));

            // xerr is too short - should throw for any value of yerr.
            ThrowsForAllYErr<ArgumentException>(x, y, new double[1]);

            // xerr is too long - should throw for any value of yerr.
            ThrowsForAllYErr<ArgumentException>(x, y, new double[3]);
        }

        /// <summary>
        /// Test the case where the y error series is the wrong length.
        /// </summary>
        [Test]
        public void TestYErrLengthMismatch()
        {
            IEnumerable<double> x = new double[2];
            IEnumerable<double> y = new double[2];

            // yerr is not provided - should not throw with any valid xerr.
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, null, null));
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, null, new double[0]));

            // yerr is correct length - should not throw with any valid xerr.
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, null, new double[2]));
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, new double[0], new double[2]));
            Assert.DoesNotThrow(() => TestSeriesWithThisData(x, y, new double[2], new double[2]));

            // yerr is too short - should throw for any value of xerr.
            ThrowsForAllXErr<ArgumentException>(x, y, new double[1]);

            // yerr is too long - should throw for any value of xerr.
            ThrowsForAllXErr<ArgumentException>(x, y, new double[3]);
        }

        [Test]
        public void TestXAndYErrLengthMismatch()
        {
            IEnumerable<double> x = new double[1];
            IEnumerable<double> y = new double[1];
            Assert.Throws<ArgumentException>(() => TestSeriesWithThisData(x, y, new double[2], new double[3]));
        }

        public void TestSeriesWithThisData(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> xerr, IEnumerable<double> yerr)
        {
            Line line = new Line(LineType.Solid, LineThickness.Thin);
            LineThickness bar = LineThickness.Thin;
            LineThickness stopper = LineThickness.Normal;
            Marker marker = new Marker(MarkerType.Cross, MarkerSize.Normal, 1);

            ErrorSeries input = new ErrorSeries("asdf", Color.Blue, true, x, y, line, marker, bar, stopper, xerr, yerr, "", "");
            var output = exporter.Export(input, AxisLabelCollection.Empty()).Result;

            Assert.That(output, Is.Not.Null);
            Assert.That(output is ScatterErrorSeries, Is.True);
            ScatterErrorSeries series = (ScatterErrorSeries)output;

            int n = x.Count();
            Assert.That(series.ItemsSource.Count(), Is.EqualTo(n));
            IEnumerable<ScatterErrorPoint> points = series.ItemsSource.Cast<ScatterErrorPoint>();
            bool havexError = xerr != null && xerr.Any();
            bool haveyError = yerr != null && yerr.Any();
            
            IEnumerator<double> enumeratorX = x.GetEnumerator();
            IEnumerator<double> enumeratorY = y.GetEnumerator();
            IEnumerator<ScatterErrorPoint> seriesEnumerator = points.GetEnumerator();
            IEnumerator<double> enumeratorXErr = xerr?.GetEnumerator();
            IEnumerator<double> enumeratorYErr = yerr?.GetEnumerator();
            while (enumeratorX.MoveNext() && enumeratorY.MoveNext() && seriesEnumerator.MoveNext() &&
                (!havexError || enumeratorXErr.MoveNext()) &&
                (!haveyError || enumeratorYErr.MoveNext()))
            {
                ScatterErrorPoint point = seriesEnumerator.Current;
                Assert.That(point.X, Is.EqualTo(enumeratorX.Current));
                Assert.That(point.Y, Is.EqualTo(enumeratorY.Current));
                double expectedXerr = havexError ? enumeratorXErr.Current : 0;
                double expectedYerr = haveyError ? enumeratorYErr.Current : 0;
                Assert.That(point.ErrorX, Is.EqualTo(expectedXerr));
                Assert.That(point.ErrorY, Is.EqualTo(expectedYerr));
            }
            Assert.That(enumeratorX.MoveNext(), Is.False, "X input has more data");
            Assert.That(enumeratorY.MoveNext(), Is.False, "Y input has more data");
            Assert.That(seriesEnumerator.MoveNext(), Is.False, "Series has more data");
        }

        /// <summary>
        /// Ensure that attempts to export the given x/y data always results
        /// in the given exception type for all different values of xerr/yerr.
        /// </summary>
        /// <param name="x">X data.</param>
        /// <param name="y">Y data.</param>
        /// <typeparam name="T">Expected exception type.</typeparam>
        private void ThrowsForAllXErrAndYErr<T>(IEnumerable<double> x, IEnumerable<double> y) where T : Exception
        {
            for (int xerrLen = -1; xerrLen < 4; xerrLen++)
            {
                for (int yerrLen = -1; yerrLen < 4; yerrLen++)
                {
                    IEnumerable<double> xerr = xerrLen < 0 ? null : new double[xerrLen];
                    IEnumerable<double> yerr = yerrLen < 0 ? null : new double[yerrLen];
                    Assert.Throws<T>(() => TestSeriesWithThisData(x, y, xerr, yerr));
                }
            }
        }

        /// <summary>
        /// Ensure that attempts to export the given x/y/yerr data always results
        /// in the given exception type for all different values of xerr.
        /// </summary>
        /// <param name="x">X data.</param>
        /// <param name="y">Y data.</param>
        /// <param name="yerr">Y error data.</param>
        /// <typeparam name="T">Expected exception type.</typeparam>
        private void ThrowsForAllXErr<T>(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> yerr) where T : Exception
        {
            Assert.Throws<T>(() => TestSeriesWithThisData(x, y, null, yerr));
            for (int xerrLen = 0; xerrLen < 4; xerrLen++)
                Assert.Throws<T>(() => TestSeriesWithThisData(x, y, new double[xerrLen], yerr));
        }

        /// <summary>
        /// Ensure that attempts to export the given x/y/xerr data always results
        /// in the given exception type for all different values of yerr.
        /// </summary>
        /// <param name="x">X data.</param>
        /// <param name="y">Y data.</param>
        /// <param name="yerr">Y error data.</param>
        /// <typeparam name="T">Expected exception type.</typeparam>
        private void ThrowsForAllYErr<T>(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> xerr) where T : Exception
        {
            Assert.Throws<T>(() => TestSeriesWithThisData(x, y, xerr, null));
            for (int yerrLen = 0; yerrLen < 4; yerrLen++)
                Assert.Throws<T>(() => TestSeriesWithThisData(x, y, xerr, new double[yerrLen]));
        }
    }
}
