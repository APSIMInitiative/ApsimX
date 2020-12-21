using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using Models;
using NUnit.Framework;
using UserInterface.Presenters;
using UserInterface.Views;
using APSIM.Shared.Utilities;
using Models.Core;
using System.Reflection;
using UnitTests.ApsimNG.Utilities;
using UserInterface.Classes;
using Models.Core.ApsimFile;
using Models.Interfaces;
using System.Data;

namespace UnitTests.ApsimNG.Views
{
    [TestFixture]
    public class TablePresenterTests
    {
        [ViewName("UserInterface.Views.DualGridView")]
        [PresenterName("UserInterface.Presenters.TablePresenter")]
        private class DualTableModel : Model, IModelAsTable
        {
            public List<DataTable> Tables { get; set; }
        }

        private ExplorerPresenter explorerPresenter;
        private DualTableModel model;

        /// <summary>
        /// Before each test is run, open a file in the GUI, add a table model, and select the table model.
        /// </summary>
        [SetUp]
        public void OpenTestFileInTab()
        {
            // Open a simple .apsimx file in the GUI.
            explorerPresenter = UITestUtilities.OpenResourceFileInTab(Assembly.GetExecutingAssembly(),
                                                    "UnitTests.ApsimNG.Resources.SampleFiles.BasicSimulation.apsimx");
            // Create a table model.
            model = new DualTableModel();
            model.Name = "Table";

            // Create a datatable and assign it to the model.
            DataTable table = new DataTable();
            table.Columns.Add("test_col", typeof(string));
            table.Rows.Add("test");
            model.Tables = new List<DataTable>() { table, table.Copy() };

            // Add the model to the .apsimx file.
            Structure.Add(model, explorerPresenter.ApsimXFile);
            explorerPresenter.Refresh();

            // Select the table model in the GUI.
            explorerPresenter.SelectNode(model);
            GtkUtilities.WaitForGtkEvents();
        }

        [TearDown]
        public void CloseFile()
        {
            UITestsMain.MasterPresenter.CloseTab(0, true);
        }

        /// <summary>
        /// Ensure that edits to the top grid are persistent. Reproduces issue #5850.
        /// https://github.com/APSIMInitiative/ApsimX/issues/5850
        /// </summary>
        [Test]
        public void TestEditingTopGrid()
        {
            DualGridView view = explorerPresenter.CurrentRightHandView as DualGridView;
            Edit(view.Grid1 as GridView, 0, 0, "x");
            Assert.AreEqual("x", model.Tables[0].Rows[0][0]);
        }

        /// <summary>
        /// Ensure that edits to the bottom grid are persistent. Reproduces issue #5850.
        /// https://github.com/APSIMInitiative/ApsimX/issues/5850
        /// </summary>
        [Test]
        public void TestEditingLowerGrid()
        {
            DualGridView view = explorerPresenter.CurrentRightHandView as DualGridView;
            Edit(view.Grid2 as GridView, 0, 0, "y");
            Assert.AreEqual("y", model.Tables[1].Rows[0][0]);
        }

        private void Edit(GridView grid, int row, int col, string contents)
        {
            // Get coordinates of top-left cell.
            GtkUtilities.GetTreeViewCoordinates(grid.Grid, 0, 0, out int x, out int y);

            // Click on the top-left cell. After this, typing anything should initiate an edit operation.
            GtkUtilities.Click(grid.Grid, Gdk.EventType.ButtonPress, Gdk.ModifierType.None, GtkUtilities.ButtonPressType.LeftClick, x, y);

            // Type the letter 'a' now that the cell is selected.
            foreach (char character in contents)
                GtkUtilities.SendKeyPress(grid.Grid, character);

            // Send return keypress, to apply changes.
            GtkUtilities.TypeKey(grid.Grid, Gdk.Key.Return, Gdk.ModifierType.None);
        }
    }
}