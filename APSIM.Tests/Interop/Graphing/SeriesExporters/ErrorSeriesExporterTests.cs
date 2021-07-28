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

namespace APSIM.Tests.Graphing.SeriesExporters
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

            ErrorSeries input = new ErrorSeries("asdf", Color.Blue, true, x, y, line, marker, bar, stopper, xerr, yerr);
            var output = exporter.Export(input);

            Assert.NotNull(output);
            Assert.True(output is ScatterErrorSeries);
            ScatterErrorSeries errorSeries = (ScatterErrorSeries)output;
            Assert.AreEqual("asdf", errorSeries.Title);
            Assert.AreEqual(3, errorSeries.ItemsSource.Count());

            // Marker style
            Assert.AreEqual(OxyPlot.MarkerType.Cross, errorSeries.MarkerType);
            Assert.AreEqual(7, errorSeries.MarkerSize);

            // Line style TBI

            // Bar style
            Assert.AreEqual(0.25, errorSeries.ErrorBarStrokeThickness);

            // TBI: stopper thickness

            // Colours
            Assert.AreEqual(OxyColors.Blue, errorSeries.ErrorBarColor);
        }
    }
}
