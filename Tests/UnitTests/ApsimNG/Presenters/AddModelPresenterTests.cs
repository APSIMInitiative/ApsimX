using APSIM.Shared.Utilities;
using Gdk;
using Models.Core;
using Models.PMF;
using Models.Soils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

            IModel paddock = Apsim.Find(explorerPresenter.ApsimXFile, typeof(Zone));

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
            Assert.AreEqual("Wheat", wheat.CropType);
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
        /// <param name="treeModel">Instance of a Gtk.TreeModel.</param>
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
