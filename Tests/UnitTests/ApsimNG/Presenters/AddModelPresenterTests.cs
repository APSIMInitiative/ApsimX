using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Models.GrazPlan;
using Models.PMF;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnitTests.ApsimNG.Utilities;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UnitTests.ApsimNG.Presenters
{
    /// <summary>
    /// Tests for the add model presenter.
    /// </summary>
    [TestFixture]
    public class AddModelPresenterTests
    {
        /// <summary>
        /// Add a resource model under replacements, and ensure its children are visible.
        /// </summary>
        [Test]
        public void AddResourceModelToReplacements()
        {
            ExplorerPresenter explorerPresenter = UITestUtilities.OpenBasicFileInGui();
            GtkUtilities.WaitForGtkEvents();

            // Add a replacements node.
            Replacements replacements = new Replacements();
            Structure.Add(replacements, explorerPresenter.ApsimXFile);
            explorerPresenter.Refresh();

            // Select the replacements node, then activate the 'add model' context menu item.
            explorerPresenter.SelectNode(replacements);
            explorerPresenter.ContextMenu.AddModel(explorerPresenter, EventArgs.Empty);
            GtkUtilities.WaitForGtkEvents();

            TreeView addModelsTree = (TreeView)ReflectionUtilities.GetValueOfFieldOrProperty("tree", explorerPresenter.CurrentPresenter);

            // Now, we double click on the fertiliser node. This should add a fertiliser model.
            // For some reason, sending a double click event doesn't trigger the ActivateRow signal.
            // Therefore, we need to manually activate the row.
            //GtkUtilities.ClickOnTreeView(treeView, path, 0, EventType.TwoButtonPress, ModifierType.None, GtkUtilities.ButtonPressType.LeftClick);
            ActivateNode(addModelsTree, ".Models.PMF.Wheat");
            Assert.AreEqual(1, replacements.Children.Count, "Replacements should now have 1 child after adding wheat, but it doesn't");
            Assert.AreEqual(typeof(Models.PMF.Plant), replacements.Children[0].GetType());

            // Wheat should have some children (read in from the resource file).
            IModel wheat = replacements.Children[0];
            Assert.NotZero(wheat.Children.Count);
            // The children should all be visible.
            foreach (IModel child in wheat.Children)
                Assert.False(child.IsHidden);
        }

        /// <summary>
        /// This test ensures that double clicking the folder for the
        /// Graph namespace doesn't add a Graph model, and that double-
        /// clicking on the Wheat node adds the wheat model.
        /// 
        /// https://github.com/APSIMInitiative/ApsimX/issues/3698#issuecomment-552251467
        /// </summary>
        [Test]
        public void DoubleClickGraphFolder()
        {
            ExplorerPresenter explorerPresenter = UITestUtilities.OpenBasicFileInGui();
            GtkUtilities.WaitForGtkEvents();

            IModel paddock = explorerPresenter.ApsimXFile.FindInScope<Zone>();

            explorerPresenter.SelectNode(paddock);
            explorerPresenter.ContextMenu.AddModel(explorerPresenter, EventArgs.Empty);
            GtkUtilities.WaitForGtkEvents();

            TreeView addModelsTree = (TreeView)ReflectionUtilities.GetValueOfFieldOrProperty("tree", explorerPresenter.CurrentPresenter);

            // Now, we double click on the fertiliser node. This should add a fertiliser model.
            // For some reason, sending a double click event doesn't trigger the ActivateRow signal.
            // Therefore, we need to manually activate the row.
            //GtkUtilities.ClickOnTreeView(treeView, path, 0, EventType.TwoButtonPress, ModifierType.None, GtkUtilities.ButtonPressType.LeftClick);
            ActivateNode(addModelsTree, ".Models.Fertiliser");
            Assert.AreEqual(2, paddock.Children.Count);
            Assert.AreEqual(typeof(Models.Fertiliser), paddock.Children[1].GetType());

            // While we're at it, let's make sure we can add resource models - e.g. wheat.
            ActivateNode(addModelsTree, ".Models.PMF.Wheat");
            Assert.AreEqual(3, paddock.Children.Count);
            Assert.AreEqual(typeof(Plant), paddock.Children[2].GetType());
            Plant wheat = paddock.Children[2] as Plant;
            Assert.AreEqual("Wheat", wheat.ResourceName);
            Assert.AreEqual("Wheat", wheat.PlantType);
        }

        /// <summary>
        /// This test ensures that double clicking the folder for the
        /// Graph namespace doesn't add a Graph model, and that double-
        /// clicking on the Wheat node adds the wheat model.
        /// 
        /// https://github.com/APSIMInitiative/ApsimX/issues/3698#issuecomment-552251467
        /// </summary>
        [Test]
        public void AddStockGenotype()
        {
            var simulations = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new Models.GrazPlan.Stock()
                }
            };
            var fileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            simulations.Write(fileName);
            var explorerPresenter = UITestsMain.MasterPresenter.OpenApsimXFileInTab(fileName, onLeftTabControl: true);
            GtkUtilities.WaitForGtkEvents();

            var stock = explorerPresenter.ApsimXFile.FindInScope<Models.GrazPlan.Stock>();

            explorerPresenter.SelectNode(stock);
            explorerPresenter.ContextMenu.AddModel(explorerPresenter, EventArgs.Empty);
            GtkUtilities.WaitForGtkEvents();

            TreeView addModelsTree = (TreeView)ReflectionUtilities.GetValueOfFieldOrProperty("tree", explorerPresenter.CurrentPresenter);

            // Let's make sure we can add stock genotype resource - e.g. Angus.
            ActivateNode(addModelsTree, ".Models.GrazPlan.Genotypes.Cattle.Beef.Angus");
            Assert.AreEqual(1, stock.Children.Count);
            Assert.AreEqual(typeof(Genotype), stock.Children[0].GetType());
            var genotype = stock.Children[0] as Genotype;
            Assert.AreEqual("Angus", genotype.Name);
            Assert.AreEqual(500, genotype.BreedSRW);
        }

        /// <summary>
        /// Activate a row in the TreeView. Would be good to find a better way of doing this.
        /// </summary>
        /// <param name="tree">An instance of UserInterface.Views.TreeView.</param>
        /// <param name="path">Path to the row. e.g. ".Simulations.ContinuousWheat.Paddock".</param>
        private void ActivateNode(TreeView tree, string path)
        {
            tree.SelectedNode = path;

            Gtk.TreeView treeView = (Gtk.TreeView)ReflectionUtilities.GetValueOfFieldOrProperty("treeview1", tree);
            Gtk.TreePath treePath = GetTreePath(tree, path);
            treeView.ActivateRow(treePath, treeView.Columns[0]);
            GtkUtilities.WaitForGtkEvents();
        }

        /// <summary>
        /// Use reflection to get the TreePath of the Graphs node. This is kind of ugly...
        /// </summary>
        /// <param name="tree">Instance of a Gtk.TreeView.</param>
        /// <param name="path">Path string - e.g. ".Simulations.ContinuousWheat.Paddock"</param>
        private Gtk.TreePath GetTreePath(TreeView tree, string path)
        {
            Gtk.TreeModel treeModel = (Gtk.TreeModel)ReflectionUtilities.GetValueOfFieldOrProperty("treemodel", tree);
            MethodInfo findNode = typeof(TreeView).GetMethod("FindNode", BindingFlags.NonPublic | BindingFlags.Instance);
            Gtk.TreeIter iter = (Gtk.TreeIter)findNode.Invoke(tree, new[] { path });
            return treeModel.GetPath(iter);
        }
    }
}
