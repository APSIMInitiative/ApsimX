using NUnit.Framework;
using System;
using System.Collections.Generic;
using APSIM.Services.Graphing;
using APSIM.Services.Documentation;
using System.Drawing;
using APSIM.Interop.Graphing;
using Moq;
using OxyPlot;
using LegendOrientation = APSIM.Services.Graphing.LegendOrientation;
using LegendPosition = APSIM.Services.Graphing.LegendPosition;
using MarkerType = APSIM.Services.Graphing.MarkerType;
using System.Linq;

namespace APSIM.Tests.Interop.Graphing
{
    /// <summary>
    /// This class contains tests for the conversion of an apsim graph
    /// (<see cref="APSIM.Services.Documentation.Graph"/>) into an
    /// oxyplot graph.
    /// </summary>
    [TestFixture]
    public class GraphToOxyPlotTests
    {
        private IGraph graph;
        private ILegendConfiguration legend;

        // Modifying these properties will modify the graph.
        // Do NOT assign to these properties. They should really be readonly,
        // but we need to assign to them in the setup method.
        private List<Series> series = new List<Series>();
        private List<Axis> axes = new List<Axis>();
        private LegendOrientation legendOrientation = LegendOrientation.Vertical;
        private LegendPosition legendPos = LegendPosition.TopLeft;
        private bool legendInsideGraphArea = true;

        [SetUp]
        public void Setup()
        {
            Mock<ILegendConfiguration> mockLegend = new Mock<ILegendConfiguration>();
            mockLegend.Setup(l => l.InsideGraphArea).Returns(() => legendInsideGraphArea);
            mockLegend.Setup(l => l.Orientation).Returns(() => legendOrientation);
            mockLegend.Setup(l => l.Position).Returns(() => legendPos);
            legend = mockLegend.Object;

            Mock<IGraph> mockGraph = new Mock<IGraph>();
            mockGraph.Setup(g => g.Series).Returns(series);
            mockGraph.Setup(g => g.Axes).Returns(axes);
            mockGraph.Setup(g => g.Legend).Returns(legend);
            graph = mockGraph.Object;
        }

        /// <summary>
        /// Test converting a graph with no series and no axes.
        /// </summary>
        [Test]
        public void TestEmptyGraph()
        {
            PlotModel plot = (PlotModel)graph.ToPlotModel();
            Assert.AreEqual(0, plot.Series.Count);
            Assert.AreEqual(0, plot.Axes.Count);
        }

        /// <summary>
        /// Ensure that using a series with no data causes an exception.
        /// </summary>
        [Test]
        public void TestSeriesWithNoData()
        {
            series.Add(CreateSimpleLineSeries(Enumerable.Empty<object>(), Enumerable.Empty<object>()));
            PlotModel plot = (PlotModel)graph.ToPlotModel();
            // Plot model should have 0 axes, and 1 series with no data.
            Assert.AreEqual(1, plot.Series.Count);
            Assert.AreEqual(0, plot.Axes.Count);

            var graphSeries = plot.Series.First();
            Assert.AreEqual(typeof(LineSeriesWithTracker), graphSeries.GetType());
            LineSeriesWithTracker lineSeries = (LineSeriesWithTracker)graphSeries;
            Assert.False(lineSeries.ItemsSource.GetEnumerator().MoveNext(), "Series contains data, but it should not");
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
                new Marker(MarkerType.Circle, MarkerSize.Normal, 1));
        }
    }
}