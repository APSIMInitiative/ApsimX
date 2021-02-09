using APSIM.Shared.Utilities;
using Gdk;
using Gtk;
using Models;
using Models.Core;
using Models.Core.Run;
using Models.Storage;
using NUnit.Framework;
using OxyPlot.GtkSharp;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTests.ApsimNG.Utilities;
using UserInterface.Commands;
using UserInterface.Presenters;
using UserInterface.Views;
using Utility;

namespace UnitTests.ApsimNG.Views
{
    /// <summary>
    /// Tests for the GraphView UI component.
    /// </summary>
    [TestFixture]
    public class GraphViewTests
    {
        private Simulations CreateTemplate()
        {
            return new Simulations()
            {
                Name = "Simulation",
                Children = new List<IModel>()
                {
                    new DataStore()
                    {
                        Name = "DataStore"
                    },
                    new Simulation()
                    {
                        Name = "Sim",
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                StartDate = new DateTime(2019, 1, 1),
                                EndDate = new DateTime(2019, 3, 1),
                                Name = "Clock"
                            },
                            new Summary()
                            {
                                Name = "Summary"
                            },
                            new Zone()
                            {
                                Name = "Field",
                                Area = 1,
                                Children = new List<IModel>()
                                {
                                    new Models.Report()
                                    {
                                        Name = "Report",
                                        VariableNames = new string[]
                                        {
                                            "[Clock].Today",
                                            "[Clock].Today.DayOfYear as n",
                                            "[Clock].Today.DayOfYear * [Clock].Today.DayOfYear as n2"
                                        },
                                        EventNames = new string[]
                                        {
                                            "[Clock].DoReport"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// This test creates a simple graph line graph with one series and ensures that
        /// 1. Changing the series type text box has an effect and behaves correctly.
        /// 2. The default series colour (in light theme) is not white (as this would be invisible against the white background).
        /// 3. Changing the legend position has an effect.
        /// </summary>
        [Test]
        public void CreateGraphs()
        {
            Simulations sims = CreateTemplate();
            sims.FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");

            DataStore storage = sims.FindInScope<DataStore>();
            storage.FileName = Path.ChangeExtension(sims.FileName, ".db");

            // Run the file to populate the datastore.
            Runner runner = new Runner(sims);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];

            // Open the .apsimx file in the GUI.
            sims.Write(sims.FileName);
            ExplorerPresenter explorer = UITestsMain.MasterPresenter.OpenApsimXFileInTab(sims.FileName, true);
            GtkUtilities.WaitForGtkEvents();
            sims = explorer.ApsimXFile;

            // Create a graphs folder under the zone.
            IModel paddock = sims.FindInScope<Zone>();
            Folder graphs = new Folder();
            graphs.Name = "Graphs";

            var command = new AddModelCommand(paddock, graphs);
            explorer.CommandHistory.Add(command, true);

            // Add an empty graph to the folder.
            Models.Graph graph = new Models.Graph();
            graph.Name = "Graph";
            command = new AddModelCommand(graphs, graph);
            explorer.CommandHistory.Add(command, true);

            // Add an empty series to the graph.
            Models.Series series = new Models.Series();
            series.Name = "Series";
            command = new AddModelCommand(graph, series);
            explorer.CommandHistory.Add(command, true);
            explorer.Refresh();

            // click on the series node.
            explorer.SelectNode(series.FullPath);
            GtkUtilities.WaitForGtkEvents();

            // Get a reference to the OxyPlot PlotView via reflection.
            SeriesView seriesView = explorer.CurrentRightHandView as SeriesView;
            GraphView view = seriesView?.GraphView as GraphView;
            Assert.NotNull(view);

            PlotView plot = ReflectionUtilities.GetValueOfFieldOrProperty("plot1", view) as PlotView;
            Assert.NotNull(plot);

            // Series has no table name or x/y series names yet, so there should
            // be nothing shown on the graph.
            Assert.AreEqual(0, plot.Model.Series.Count);

            // Now draw some data on the graph.
            seriesView.DataSource.SelectedValue = "Report";
            seriesView.X.SelectedValue = "n";
            seriesView.Y.SelectedValue = "n2";
            seriesView.SeriesType.SelectedValue = "Scatter";

            GtkUtilities.WaitForGtkEvents();

            // There should now be one series showing.
            Assert.AreEqual(1, plot.Model.Series.Count);

            // It should be a line series.
            Assert.True(plot.Model.Series[0] is LineSeries, "Graph series type is set to scatter, but the series object is not a LineSeries.");

            // Series colour should not be white, and should not be the same as the background colour.
            LineSeries line = plot.Model.Series[0] as LineSeries;
            OxyPlot.OxyColor empty = OxyPlot.OxyColor.FromArgb(0, 0, 0, 0);
            OxyPlot.OxyColor white = OxyPlot.OxyColor.FromArgb(0, 255, 255, 255);
            Assert.AreNotEqual(empty, line.Color, "Graph line series default colour is white on white.");
            Assert.AreNotEqual(white, line.Color, "Graph line series default colour is white on white.");

            // Legend should be visible but empty by default.
            Assert.True(plot.Model.IsLegendVisible);
            // todo - ensure legend is empty

            // Next, we want to change the legend position and ensure that the legend actually moves.

            // Click on the 'show in legend' checkbox.
            seriesView.ShowInLegend.Checked = true;
            GtkUtilities.WaitForGtkEvents();

            // Double click on the middle of the legend.
            Cairo.Rectangle legendRect = plot.Model.LegendArea.ToRect(true);
            double x = (legendRect.X + (legendRect.X + legendRect.Width)) / 2;
            double y = (legendRect.Y + (legendRect.Y + legendRect.Height)) / 2;
            GtkUtilities.DoubleClick(plot, x, y, wait: true);

            // Default legend position should be top-left.
            Assert.AreEqual(plot.Model.LegendPosition, OxyPlot.LegendPosition.TopLeft);

            // Now we want to change the legend position. First, get a reference to the legend view
            // via the legend presenter, via the graph presenter, via the series presenter, via the explorer presenter.
            Assert.True(explorer.CurrentPresenter is SeriesPresenter);
            SeriesPresenter seriesPresenter = explorer.CurrentPresenter as SeriesPresenter;
            LegendPresenter legendPresenter = seriesPresenter.GraphPresenter.CurrentPresenter as LegendPresenter;

            // todo: should we add something like a GetView() method to the IPresenter interface?
            // It might be a bit of work to set up but would save us having to use reflection
            LegendView legendView = ReflectionUtilities.GetValueOfFieldOrProperty("view", legendPresenter) as LegendView;

            // The legend options are inside a Gtk expander.
            Assert.IsTrue(legendView.MainWidget.Parent is Expander);
            Expander expander = legendView.MainWidget.Parent as Expander;

            // The expander should be expanded and the options visible.
            Assert.IsTrue(expander.Expanded);
            Assert.IsTrue(legendView.MainWidget.Visible);

            // The legend view contains a combo box with the legend position options (top-right, bottom-left, etc).
            // This should really be refactored to use a public IDropDownView, which is much more convenient to use.
            // First, get a reference to the combo box via reflection.
            ComboBox combo = ReflectionUtilities.GetValueOfFieldOrProperty("combobox1", legendView.PositionDropDown) as ComboBox;

            // fixme - we should support all valid OxyPlot legend position types.
            foreach (Models.Graph.LegendPositionType legendPosition in Enum.GetValues(typeof(Models.Graph.LegendPositionType)))
            {
                string name = legendPosition.ToString();
                GtkUtilities.SelectComboBoxItem(combo, name, wait: true);

                OxyPlot.LegendPosition oxyPlotEquivalent = (OxyPlot.LegendPosition)Enum.Parse(typeof(OxyPlot.LegendPosition), name);
                Assert.AreEqual(plot.Model.LegendPosition, oxyPlotEquivalent);
            }

            // If we change the graph to a box plot then the several unused properties should be disabled.
            // These are x variable dropdown, x cumulative, x on top, marker size/type checkboxes.

            // First, make sure that these options are sensitive to input and can be changed.
            Assert.IsTrue(seriesView.X.IsSensitive);
            Assert.IsTrue(seriesView.XCumulative.IsSensitive);
            Assert.IsTrue(seriesView.XOnTop.IsSensitive);
            Assert.IsTrue(seriesView.MarkerSize.IsSensitive);
            Assert.IsTrue(seriesView.MarkerType.IsSensitive);

            // Now change series type to box plot.
            GtkUtilities.SelectComboBoxItem(seriesView.SeriesType, "Box", wait: true);
            Assert.AreEqual(SeriesType.Box, series.Type);

            // Ensure the box plot is not white in light theme.
            plot = ReflectionUtilities.GetValueOfFieldOrProperty("plot1", view) as PlotView;
            Assert.NotNull(plot);
            BoxPlotSeries boxPlot = plot.Model.Series.OfType<BoxPlotSeries>().FirstOrDefault();
            Assert.NotNull(boxPlot);

            Assert.AreNotEqual(empty, boxPlot.Fill);
            Assert.AreNotEqual(white, boxPlot.Fill);
            Assert.AreNotEqual(empty, boxPlot.Stroke);
            Assert.AreNotEqual(white, boxPlot.Stroke);

            // The controls should no longer be sensitive.
            Assert.IsFalse(seriesView.XCumulative.IsSensitive);
            Assert.IsFalse(seriesView.XOnTop.IsSensitive);
            Assert.IsFalse(seriesView.MarkerSize.IsSensitive);
            Assert.IsFalse(seriesView.MarkerType.IsSensitive);

            // Change the series type back to scatter.
            GtkUtilities.SelectComboBoxItem(seriesView.SeriesType, "Scatter", wait: true);

            // The controls should be sensitive once more.
            Assert.IsTrue(seriesView.X.IsSensitive);
            Assert.IsTrue(seriesView.XCumulative.IsSensitive);
            Assert.IsTrue(seriesView.XOnTop.IsSensitive);
            Assert.IsTrue(seriesView.MarkerSize.IsSensitive);
            Assert.IsTrue(seriesView.MarkerType.IsSensitive);
        }
    }
}
