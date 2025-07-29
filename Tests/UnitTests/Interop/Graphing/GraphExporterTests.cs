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

namespace UnitTests.Interop.Graphing
{
    /// <summary>
    /// This class contains tests for the conversion of an apsim graph
    /// into an oxyplot plot model. See:
    /// <see cref="GraphExporter"/>
    /// </summary>
    [TestFixture]
    public class GraphExporterTests
    {
        private IGraph graph;
        private ILegendConfiguration legend;
        private GraphExporter exporter;

        // Modifying these properties will modify the graph.
        // Do NOT assign to these properties. They should really be readonly,
        // but we need to assign to them in the setup method.
        private List<Series> series = new List<Series>();
        private Axis xAxis = null;
        private Axis yAxis = null;
        private LegendOrientation legendOrientation = LegendOrientation.Vertical;
        private LegendPosition legendPos = LegendPosition.TopLeft;
        private bool legendInsideGraphArea = true;

        [SetUp]
        public void Setup()
        {
            Mock<Axis> mockAxis = new Mock<Axis>("", AxisPosition.Bottom);
            xAxis = mockAxis.Object;
            yAxis = mockAxis.Object;

            Mock<ILegendConfiguration> mockLegend = new Mock<ILegendConfiguration>();
            mockLegend.Setup(l => l.InsideGraphArea).Returns(() => legendInsideGraphArea);
            mockLegend.Setup(l => l.Orientation).Returns(() => legendOrientation);
            mockLegend.Setup(l => l.Position).Returns(() => legendPos);
            legend = mockLegend.Object;

            Mock<IGraph> mockGraph = new Mock<IGraph>();
            mockGraph.Setup(g => g.Series).Returns(series);
            mockGraph.Setup(g => g.XAxis).Returns(() => xAxis);
            mockGraph.Setup(g => g.YAxis).Returns(() => yAxis);
            mockGraph.Setup(g => g.Legend).Returns(legend);
            graph = mockGraph.Object;

            exporter = new GraphExporter();
        }

        /// <summary>
        /// Test a graph with no x-axis. Should throw if we attempt to export it.
        /// </summary>
        [Test]
        public void TestNoXAxis()
        {
            xAxis = null;
            Assert.Throws<Exception>(() => exporter.ToPlotModel(graph));
        }

        /// <summary>
        /// Test a graph with no x-axis. Should throw if we attempt to export it.
        /// </summary>
        [Test]
        public void TestNoYAxis()
        {
            yAxis = null;
            Assert.Throws<Exception>(() => exporter.ToPlotModel(graph));
        }

        /// <summary>
        /// Test converting a graph with no series and no axes.
        /// </summary>
        [Test]
        public void TestEmptyGraph()
        {
            PlotModel plot = (PlotModel)exporter.ToPlotModel(graph);
            Assert.That(plot.Series.Count, Is.EqualTo(0));
            Assert.That(plot.Axes.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Ensure that using a series with no data causes an exception.
        /// </summary>
        [Test]
        public void TestSeriesWithNoData()
        {
            series.Add(CreateSimpleLineSeries(Enumerable.Empty<object>(), Enumerable.Empty<object>()));
            PlotModel plot = (PlotModel)exporter.ToPlotModel(graph);
            // Plot model should have 0 axes, and 1 series with no data.
            Assert.That(plot.Series.Count, Is.EqualTo(1));
            Assert.That(plot.Axes.Count, Is.EqualTo(0));

            var graphSeries = plot.Series.First();
            Assert.That(graphSeries.GetType(), Is.EqualTo(typeof(LineSeriesWithTracker)));
            LineSeriesWithTracker lineSeries = (LineSeriesWithTracker)graphSeries;
            Assert.That(lineSeries.ItemsSource.GetEnumerator().MoveNext(), Is.False, "Series contains data, but it should not");
        }

        // fixme: the exception gets thrown in the series constructor,
        // ergo this test should be with the series class tests.
        // /// <summary>
        // /// Test a series with a different number of x vs y items.
        // /// (This should throw when we attempt to creat a plot model).
        // /// </summary>
        // [Test]
        // public void TestSeriesWithItemCountMismatch()
        // {
        //     object[] x = new object[1] { 0d };
        //     object[] y = new object[2] { 0d, 1d };
        //     series.Add(CreateSimpleLineSeries(x, y));
        //     Assert.Throws<ArgumentException>(() => graph.ToPlotModel());
        // }

        private Series CreateSimpleLineSeries(IEnumerable<object> x, IEnumerable<object> y)
        {
            return new LineSeries(
                "series1",
                Color.Red,
                false,
                x,
                y,
                new Line(LineType.Solid, LineThickness.Normal),
                new Marker(MarkerType.Circle, MarkerSize.Normal, 1),
                "",
                "");
        }
    }
}