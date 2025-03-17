using NUnit.Framework;
using System;
using System.Collections.Generic;
using APSIM.Shared.Graphing;
using APSIM.Shared.Documentation;
using System.Drawing;
using APSIM.Documentation.Graphing;
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

            Assert.That(oxySeries, Is.Not.Null);
            Assert.That(oxySeries is BoxPlotSeries, Is.True);
            BoxPlotSeries boxPlot = (BoxPlotSeries)oxySeries;

            // Line style
            Assert.That(boxPlot.LineStyle, Is.EqualTo(OxyPlot.LineStyle.Solid));
            Assert.That(boxPlot.StrokeThickness, Is.EqualTo(0.5));

            // Marker style
            Assert.That(boxPlot.OutlierType, Is.EqualTo(OxyPlot.MarkerType.Square));
            Assert.That(boxPlot.OutlierSize, Is.EqualTo(7));

            // Colours
            Assert.That(boxPlot.Stroke, Is.EqualTo(OxyColors.Transparent));
            Assert.That(boxPlot.Fill, Is.EqualTo(OxyColors.Red));

            // Title
            Assert.That(boxPlot.Title, Is.EqualTo("Title"));

            // Contents of series.
            Assert.That(boxPlot.Items.Count, Is.EqualTo(1));
            BoxPlotItem item = boxPlot.Items[0];
            Assert.That(item, Is.Not.Null);

            // Test box plot whisker values.
            Assert.That(item.X, Is.EqualTo(0));
            Assert.That(item.LowerWhisker, Is.EqualTo(0));
            Assert.That(item.BoxBottom, Is.EqualTo(1));
            Assert.That(item.Median, Is.EqualTo(2));
            Assert.That(item.BoxTop, Is.EqualTo(3));
            Assert.That(item.UpperWhisker, Is.EqualTo(4));
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
                Assert.That(oxySeries.Fill, Is.EqualTo(OxyColors.Green));
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
