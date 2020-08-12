using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Models.Core;
using UserInterface.Extensions;
using UserInterface.Presenters;

namespace Utility
{
    /// <summary>
    /// Class for displaying results from a "Find All References" operation.
    /// Displays results a Gtk.TreeView in a popup window.
    /// </summary>
    public sealed class FindAllReferencesDialog : IDisposable
    {
        /// <summary>
        /// Window in which the data will be displayed.
        /// </summary>
        private Window window;

        /// <summary>
        /// TreeView responsible for displaying the data.
        /// </summary>
        private TreeView tree;

        /// <summary>
        /// Data to be displayed.
        /// </summary>
        private ListStore data;

        /// <summary>
        /// The target model to which all references are pointing.
        /// </summary>
        private IModel target;

        /// <summary>
        /// The ExplorerPresenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Default constructor.
        /// Displays an empty Find All References dialog.
        /// </summary>
        public FindAllReferencesDialog()
        {
            Initialise();
        }

        /// <summary>
        /// Initialises and populates the view.
        /// </summary>
        /// <param name="model">The reference model.</param>
        /// <param name="references">List of references to be displayed.</param>
        /// <param name="presenter">The ExplorerPresenter for this tab.</param>
        public FindAllReferencesDialog(IModel model, List<Reference> references, ExplorerPresenter presenter)
        {
            this.target = model;
            this.explorerPresenter = presenter;
            Initialise();
            Update(references);
        }

        /// <summary>
        /// Empties the view and repopulates it with data.
        /// </summary>
        /// <param name="references">List of references to be displayed.</param>
        public void Update(List<Reference> references)
        {
            data.Clear();
            if (references == null || references.Count < 1)
            {
                if (target != null)
                    data.AppendValues(string.Format("Found no references to '{0}'.", target.Name), "");
                return;
            }

            window.Title = string.Format("'{0}' references", references[0].Target.Name);

            string commonWordsString = GetCommonPathElements(references.Select(r => r.Model.FullPath).ToArray());
            data.AppendValues(target.FullPath, "", target.FullPath);
            foreach (Reference reference in references)
            {
                string path = reference.Model.FullPath;
                string cutDownPath = path.Replace(commonWordsString, "");
                data.AppendValues(cutDownPath, reference.Member.DeclaringType.Name, path);
            }
        }

        /// <summary>
        /// Get the prefix common to all paths in all references.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns></returns>
        private string GetCommonPathElements(string[] paths)
        {
            try
            {
                string common = new string(
                    paths.First().Substring(0, paths.Min(s => s.Length))
                    .TakeWhile((c, i) => paths.All(s => s[i] == c)).ToArray());

                // We actually want to display the last common word in the path.
                return common.Substring(0, common.TrimEnd('.').LastIndexOf('.') + 1);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs a one-time initialisation of the Gtk components.
        /// </summary>
        private void Initialise()
        {
            tree = new TreeView();
            data = new ListStore(typeof(string), typeof(string), typeof(string));
            tree.Model = data;
            tree.CanFocus = true;
#if NETFRAMEWORK
            tree.RulesHint = true; // Allows for alternate-row colouring.
#endif
            tree.CursorChanged += OnSelectionChanged;
            tree.KeyPressEvent += OnKeyPress;

            // First column displays the path to the model.
            TreeViewColumn col = new TreeViewColumn();
            col.Title = "Model";
            CellRendererText cell = new CellRendererText();
            col.PackStart(cell, false);
            col.AddAttribute(cell, "text", 0);
            tree.AppendColumn(col);

            // Second column displays the name of the property or field which
            // references the model.
            col = new TreeViewColumn();
            col.Title = "Property Name";
            cell = new CellRendererText();
            col.PackStart(cell, false);
            col.AddAttribute(cell, "text", 1);
            tree.AppendColumn(col);

            window = new Window("Find All References")
            {
                SkipPagerHint = true,
                SkipTaskbarHint = true,
                Parent = explorerPresenter.GetView().MainWidget,
                WindowPosition = WindowPosition.CenterAlways,
                TransientFor = explorerPresenter.GetView().MainWidget.Toplevel as Window
            };
            window.DeleteEvent += OnClose;
            window.Destroyed += OnClose;
            window.Add(tree);
            window.ShowAll();
        }

        /// <summary>
        /// Invoked when the selected row is changed.
        /// Navigates to the selected node in the simulations tree.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnSelectionChanged(object sender, EventArgs args)
        {
            try
            {
                // Get the the first selected row.
                TreePath path = tree.Selection?.GetSelectedRows()?.FirstOrDefault();
                if (path == null)
                    return;

                TreeIter iter;
                data.GetIter(out iter, path);
                string nodePath = data.GetValue(iter, 2) as string;
                if (nodePath == null)
                    throw new Exception("Unable to navigate to selected item in 'Find All References' window.");

                // Select this node in the tree.
                explorerPresenter.SelectNode(nodePath);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user presses a key.
        /// Closes the dialog if the key was escape.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnKeyPress(object sender, KeyPressEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Escape)
                window.Cleanup();
        }

        /// <summary>
        /// Invoked when the user closes the window.
        /// Detaches event handlers and disposes resources.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnClose(object sender, EventArgs args)
        {
            try
            {
                if (tree != null)
                {
                    tree.CursorChanged -= OnSelectionChanged;
                    tree.Dispose();
                    tree = null;
                }
                if (data != null)
                {
                    data.Clear();
                    data.Dispose();
                    data = null;
                }
                if (window != null)
                {
                    window.DeleteEvent -= OnClose;
                    window.Destroyed -= OnClose;
                    window.Dispose();
                    window = null;
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        public void Dispose()
        {
            data?.Dispose();
            tree?.Dispose();
        }
    }
}
