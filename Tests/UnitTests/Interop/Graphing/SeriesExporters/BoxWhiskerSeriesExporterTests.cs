using NUnit.Framework;
using System;
using System.Collections.Generic;
using APSIM.Shared.Graphing;
using APSIM.Shared.Documentation;
using System.Drawing;
using APSIM.Interop.Graphing;
using Moq;
using OxyPlot;
using LegendOrientation = APSIM.Shared.Graphing.LegendOrientation;
using LegendPosition = APSIM.Shared.Graphing.LegendPosition;
using MarkerType = APSIM.Shared.Graphing.MarkerType;
using System.Linq;
using OxyPlot.Series;

namespace UnitTests.Graphing.SeriesExporters
{
    [TestFixture]
    public class BoxWhiskerSeriesExporterTests
    {
        private BoxWhiskerSeriesExporter exporter = new BoxWhiskerSeriesExporter();

        [Test]
        public void TestSimpleCase()
        {
            object[] x = new object[7] { 0d, 1d, 2d, 3d, 4d, 5d, 6d };
            object[] y = new object[7] { 0d, 1d, 1d, 2d, 3d, 3d, 4d };
            Line line = new Line(LineType.Solid, LineThickness.Normal);
            Marker marker = new Marker(MarkerType.FilledSquare, MarkerSize.Normal, 1);
            BoxWhiskerSeries series = new BoxWhiskerSeries("Title", Color.Red, true, x, y, line, marker, "", "");
            var oxySeries = exporter.Export(series, AxisLabelCollection.Empty()).Result;

            Assert.NotNull(oxySeries);
            Assert.True(oxySeries is BoxPlotSeries);
            BoxPlotSeries boxPlot = (BoxPlotSeries)oxySeries;

            // Line style
            Assert.AreEqual(OxyPlot.LineStyle.Solid, boxPlot.LineStyle);
            Assert.AreEqual(0.5, boxPlot.StrokeThickness);

            // Marker style
            Assert.AreEqual(OxyPlot.MarkerType.Square, boxPlot.OutlierType);
            Assert.AreEqual(7, boxPlot.OutlierSize);

            // Colours
            Assert.AreEqual(OxyColors.Transparent, boxPlot.Stroke);
            Assert.AreEqual(OxyColors.Red, boxPlot.Fill);

            // Title
            Assert.AreEqual("Title", boxPlot.Title);

            // Contents of series.
            Assert.AreEqual(1, boxPlot.Items.Count);
            BoxPlotItem item = boxPlot.Items[0];
            Assert.NotNull(item);

            // Test box plot whisker values.
            Assert.AreEqual(0, item.X);
            Assert.AreEqual(0, item.LowerWhisker);
            Assert.AreEqual(1, item.BoxBottom);
            Assert.AreEqual(2, item.Median);
            Assert.AreEqual(3, item.BoxTop);
            Assert.AreEqual(4, item.UpperWhisker);
        }

        /// <summary>
        /// Ensure that filled markers result in the box series being filled
        /// with the specified colour, and that non-filled marker type causes
        /// the box to NOT be filled with colour.
        /// </summary>
        [Test]
        public void TestMarkerFill()
        {
            MarkerType[] filledMarkers = new MarkerType[4]
            {
                MarkerType.FilledCircle,
                MarkerType.FilledDiamond,
                MarkerType.FilledSquare,
                MarkerType.FilledTriangle
            };
            MarkerType[] nonFilledMarkers = new MarkerType[]
            {
                MarkerType.Circle,
                MarkerType.Cross,
                MarkerType.Diamond,
                MarkerType.None,
                MarkerType.Plus,
                MarkerType.Square,
                MarkerType.Star,
                MarkerType.Triangle
            };

            object[] x = new object[5] { 0d, 1d, 2d, 3d, 4d };
            object[] y = new object[5] { 0d, 0d, 1d, 2d, 2d };
            Line line = new Line(LineType.Solid, LineThickness.Normal);
            foreach (MarkerType markerType in filledMarkers)
            {
                Marker marker = new Marker(markerType, MarkerSize.Normal, 1);
                BoxWhiskerSeries series = new BoxWhiskerSeries("Title", Color.Green, true, x, y, line, marker, "", "");
                BoxPlotSeries oxySeries = (BoxPlotSeries)exporter.Export(series, AxisLabelCollection.Empty()).Result;

                // Because marker type is "filled", series should be filled with colour.
                Assert.AreEqual(OxyColors.Green, oxySeries.Fill);
            }

            foreach (MarkerType markerType in nonFilledMarkers)
            {
                Marker marker = new Marker(markerType, MarkerSize.Normal, 1);
                BoxWhiskerSeries series = new BoxWhiskerSeries("Title", Color.Green, true, x, y, line, marker, "", "");
                BoxPlotSeries oxySeries = (BoxPlotSeries)exporter.Export(series, AxisLabelCollection.Empty()).Result;

                // Because marker type is "filled", series should be filled with colour.
                // todo
                // Assert.AreEqual(OxyColors.Transparent, oxySeries.Fill);
            }
        }
    }
}
