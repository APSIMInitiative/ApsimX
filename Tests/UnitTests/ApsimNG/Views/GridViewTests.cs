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

namespace UnitTests.ApsimNG.Views
{
    [TestFixture]
    public class GridViewTests
    {
        private ExplorerPresenter explorerPresenter;

        [OneTimeSetUp]
        public void OpenTestFileInTab()
        {
            explorerPresenter = UITestUtilities.OpenResourceFileInTab(Assembly.GetExecutingAssembly(),
                                                    "UnitTests.ApsimNG.Resources.SampleFiles.BasicSimulation.apsimx");
        }

        [OneTimeTearDown]
        public void CloseTab()
        {
            // Close the tab we opened. This assumes that this is the only open tab.
            UITestsMain.MasterPresenter.CloseTab(0, onLeft: true);
        }

        /// <summary>
        /// This test ensures that a keypress while a cell is selected will cause the cell to enter edit mode.
        /// </summary>
        [Test]
        public void EnsureKeyPressInitiatesEditing()
        {
            // Click on clock node.
            explorerPresenter.SelectNode(".Simulations.Simulation.Clock");
            GtkUtilities.WaitForGtkEvents();

            GridView grid = explorerPresenter.CurrentRightHandView as GridView;
            if (grid == null)
                throw new Exception("Clock view is not a GridView");

            // Click on top-right cell - this will be in the value column, and so will be editable.
            GtkUtilities.GetTreeViewCoordinates(grid.Grid, 0, 1, out int x, out int y);
            GtkUtilities.Click(grid.Grid, Gdk.EventType.ButtonPress, Gdk.ModifierType.None, GtkUtilities.ButtonPressType.LeftClick, x, y);

            // Grid should not be in edit mode at this point.
            Assert.IsFalse(grid.IsUserEditingCell);

            // Type the letter 'a' now that the cell is selected.
            GtkUtilities.SendKeyPress(grid.Grid, 'a');

            // Grid should now be in edit mode.
            Assert.IsTrue(grid.IsUserEditingCell);
        }

        /// <summary>
        /// This test ensures that double clicking a cell will cause the cell to enter edit mode.
        /// </summary>
        [Test]
        public void EnsureDoubleClickInitiatesEditing()
        {
            // Click on clock node.
            explorerPresenter.SelectNode(".Simulations.Simulation.Clock");
            GtkUtilities.WaitForGtkEvents();

            GridView grid = explorerPresenter.CurrentRightHandView as GridView;
            if (grid == null)
                throw new Exception("Clock view is not a GridView");

            // Grid should not be in edit mode at this point.
            Assert.IsFalse(grid.IsUserEditingCell);

            // Double-click on the top-right cell using the coordinates.
            GtkUtilities.ClickOnGridCell(grid, 0, 1, Gdk.EventType.TwoButtonPress, Gdk.ModifierType.None, GtkUtilities.ButtonPressType.LeftClick, wait: true);

            // Grid should now be in edit mode.
            Assert.IsTrue(grid.IsUserEditingCell);
        }

        /// <summary>
        /// This test ensures that arrow keys can move the caret inside the cell when the cell is in edit mode.
        /// </summary>
        /// <remarks>
        /// This test will break if you pause it via a debugger because this causes Apsim to lose focus and
        /// the grid cell will go out of edit mode next time you wait for gtk to process its events.
        /// </remarks>
        [Test]
        public void EnsureArrowKeysMoveCursorInsideCell()
        {
            // Click on clock node.
            explorerPresenter.SelectNode(".Simulations.Simulation.Clock");
            GtkUtilities.WaitForGtkEvents();

            GridView grid = explorerPresenter.CurrentRightHandView as GridView;
            if (grid == null)
                throw new Exception("Clock view is not a GridView");

            GtkUtilities.ClickOnGridCell(grid, 0, 1, Gdk.EventType.TwoButtonPress, Gdk.ModifierType.None, GtkUtilities.ButtonPressType.LeftClick, wait: true);

            // Double clicking on the cell will highlight all text in the cell. If we press the left arrow, the cursor should move to the start of the cell.
            GtkUtilities.TypeKey(grid.Grid, Gdk.Key.Left, Gdk.ModifierType.None, wait: true);

            Entry editControl = ReflectionUtilities.GetValueOfFieldOrProperty("editControl", grid) as Entry;
            Assert.AreEqual(0, editControl.CursorPosition);

            // The cursor is at the start of the cell. Pressing the left key now should have no effect.
            GtkUtilities.TypeKey(grid.Grid, Gdk.Key.Left, Gdk.ModifierType.None, wait: true);
            Assert.AreEqual(0, editControl.CursorPosition);

            // Now keep hitting the right key until we get to the end of the end of the cell.
            for (int i = 1; i < editControl.Text.Length; i++)
            {
                GtkUtilities.TypeKey(grid.Grid, Gdk.Key.Right, Gdk.ModifierType.None, wait: true);
                Assert.AreEqual(i, editControl.CursorPosition);
            }

            // The cursor should now be at the far right of the cell. Pressing the right arrow now should have no effect.
            GtkUtilities.TypeKey(grid.Grid, Gdk.Key.Right, Gdk.ModifierType.None, wait: true);
            Assert.AreEqual(editControl.Text.Length, editControl.CursorPosition);

            // What behaviour should up/down arrows have if the cell is in edit mode?
        }
    }
}
