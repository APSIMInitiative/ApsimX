using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Timers;
using APSIM.Shared.Utilities;
using Gtk;
using UserInterface.Interfaces;
using APSIMNG.Utility;
using TreeModel = Gtk.ITreeModel;

namespace UserInterface.Views
{

    /// <summary>
    /// This class encapsulates a hierachical tree view that the user interacts with.
    /// </summary>
    /// <remarks>
    /// The basics are all here, but there are still a few things to be implemented:
    /// Drag and drop is pinning an object so we can pass its address around as data. Is there a better way?
    /// (Probably not really, as we go through a native layer, unless we can get by with the serialized XML).
    /// Shortcuts (accelerators in Gtk terminology) haven't yet been implemented.
    /// Link doesn't work, but it appears that move and link aren't working in the Windows.Forms implementation either.
    /// Actually, Move "works" here but doesn't undo correctly
    /// </remarks>
    public class TreeView : ViewBase, ITreeView
    {
        private static object _lockObject = new();

        private string previouslySelectedNodePath;
        private string sourcePathOfItemBeingDragged;
        private string nodePathBeforeRename;
        private Gtk.TreeView _treeview1 = null;
        private TreeViewNode rootNode;
        private ISerializable dragDropData = null;
        private GCHandle dragSourceHandle;
        private CellRendererText _textRender;
        private const string modelMime = "application/x-model-component";
        private Timer timer = new Timer();
        private bool isEdittingNodeLabel = false;

        /// <summary>
        /// Keep track of whether the accelerator group is attached to the toplevel window.
        /// </summary>
        /// <remarks>
        /// Normally we just need to remove the accelerators when the treeview loses focus,
        /// and re-add them when it regains focus. However, it's possible for the treeview
        /// to gain focus multiple times without losing it in-between, which leads to
        /// gtk warnings. Typically this occurs after using the search functionality.
        /// The solution is to use this variable to keep track of whether the accelerators
        /// are already attached to the window, so that we only add them when necessary.
        /// </remarks>
        private bool acceleratorsAreAttached;

        // If you add a new item to the tree model that is not at the end, a lot of things will break.
        private TreeStore _treemodel {
            get {
                return (TreeStore)_treeview1.Model; 
            }
        }

        /// <summary>Constructor</summary>
        public TreeView() {}

        /// <summary>Constructor</summary>
        public TreeView(ViewBase owner) : base(owner)
        {
            Initialise(owner, new Gtk.TreeView());
        }

        /// <summary>Constructor</summary>
        public TreeView(ViewBase owner, Gtk.TreeView treeView) : base(owner)
        {
            Initialise(owner, treeView);
        }

        /// <summary>Gets or sets whether tree nodes can be changed.</summary>
        public bool ReadOnly { get; set; }

        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf();
            iconRender.SetPadding(2, 1);

            _textRender = new Gtk.CellRendererText();
            _textRender.Editable = false;
            _textRender.EditingStarted += OnBeforeLabelEdit;
            _textRender.Edited += OnAfterLabelEdit;

            CellRendererText tickCell = new CellRendererText();
            tickCell.Editable = false;

            TreeViewColumn column = new TreeViewColumn();
            column.PackStart(iconRender, false);
            column.PackStart(_textRender, true);
            column.SetCellDataFunc(_textRender, OnSetCellData);
            column.PackEnd(tickCell, false);
            column.SetAttributes(iconRender, "pixbuf", 1);
            column.SetAttributes(_textRender, "text", 0);

            TargetEntry[] target_table = new TargetEntry[] {
               new TargetEntry(modelMime, TargetFlags.App, 0)
            };

            _treeview1 = gtkControl as Gtk.TreeView;
            _treeview1.Model = new TreeStore(typeof(string), typeof(Gdk.Pixbuf), typeof(string), typeof(Color), typeof(bool));
            _treeview1.AppendColumn(column);
            _treeview1.TooltipColumn = 2;
            
            _treeview1.CursorChanged += OnAfterSelect;
            _treeview1.ButtonReleaseEvent += OnButtonUp;
            _treeview1.ButtonPressEvent += OnButtonPress;
            _treeview1.RowActivated += OnRowActivated;
            _treeview1.FocusInEvent += OnTreeGainFocus;
            _treeview1.FocusOutEvent += OnTreeLoseFocus;
            _treeview1.RowExpanded += OnRowExpanded;
            _treeview1.DragMotion += OnDragOver;
            _treeview1.DragDrop += OnDragDrop;
            _treeview1.DragBegin += OnDragBegin;
            _treeview1.DragDataGet += OnDragDataGet;
            _treeview1.DragDataReceived += OnDragDataReceived;
            _treeview1.DragEnd += OnDragEnd;
            
            Gdk.DragAction actions = Gdk.DragAction.Copy | Gdk.DragAction.Link | Gdk.DragAction.Move;
            Drag.SourceSet(_treeview1, Gdk.ModifierType.Button1Mask, target_table, actions);
            Drag.DestSet(_treeview1, 0, target_table, actions);
            
            mainWidget = _treeview1;

            timer.Elapsed += OnTimerElapsed;
            mainWidget.Destroyed += OnDestroyed;
        }

        /// <summary>Invoked when a node is selected not by the user but by an Undo command.</summary>
        public event EventHandler<NodeSelectedArgs> SelectedNodeChanged;

        /// <summary>Invoked when a drag operation has commenced. Need to create a DragObject.</summary>
        public event EventHandler<DragStartArgs> DragStarted;

        /// <summary>Invoked to determine if a drop is allowed on the specified Node.</summary>
        public event EventHandler<AllowDropArgs> AllowDrop;

        /// <summary>Invoked when a drop has occurred.</summary>
        public event EventHandler<DropArgs> Droped;

        /// <summary>Invoked then a node is renamed.</summary>
        public event EventHandler<NodeRenameArgs> Renamed;

        /// <summary>Invoked then a node is double clicked.</summary>
        public event EventHandler<EventArgs> DoubleClicked;

        /// <summary>Gets or sets the currently selected node.</summary>
        public string SelectedNode
        {
            get
            {
                lock (_lockObject)
                {
                    TreePath selPath;
                    TreeViewColumn selCol;
                    _treeview1.GetCursor(out selPath, out selCol);
                    if (selPath != null)
                        return this.GetFullPath(selPath);
                    else
                        return string.Empty;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    if (SelectedNode != value && value != string.Empty)
                    {
                        if (FindNode(value, out TreeIter iter))
                        {
                            TreePath pathToSelect = _treemodel.GetPath(iter);
                            if (pathToSelect != null)
                            {
                                _treeview1.ExpandToPath(pathToSelect);
                                _treeview1.SetCursor(pathToSelect, _treeview1.GetColumn(0), false);
                            }
                            // Scroll to the newly-selected cell (if necessary; in theory, setting
                            // use_align to false should cause the tree to perform the minimum amount
                            // of scrolling necessary to bring the cell onscreen).
                            _treeview1.ScrollToCell(pathToSelect, null, false, 0, 0);
                            _treeview1.GrabFocus();
                        }
                    }
                }
            }
        }

        /// <summary>Gets or sets the width of the tree view.</summary>
        public Int32 TreeWidth
        {
            get 
            { 
                lock (_lockObject)
                {
                    return _treeview1.Allocation.Width;
                }
            }
            set 
            {
                lock (_lockObject)
                {
                    _treeview1.WidthRequest = value;
                }
            }
        }

        /// <summary>Gets or sets the popup menu of the tree view.</summary>
        public MenuView ContextMenu { get; set; }

        /// <summary>
        /// Record which rows are currently expanded.
        /// We can't directly use a TreePath to an outdated TreeModel/TreeView, so we store the path as a string, and 
        /// then parse that into a new TreePath after the model is reassembled. This assumes that the structure of the 
        /// tree view/model does not change when RefreshNode() is called (e.g. it still has the same number of rows/columns).
        /// </summary>
        /// <returns></returns>
        public List<string> GetExpandedRows()
        {
            List<string> expandedNodes = new List<string>();
            _treeview1.MapExpandedRows(new TreeViewMappingFunc((tree, path) =>
            {
                expandedNodes.Add(path.ToString());
            }));
            return expandedNodes;
        }

        /// <summary>Populate the treeview.</summary>
        /// <param name="topLevelNode">A description of the top level root node</param>
        public void Populate(TreeViewNode topLevelNode)
        {
            lock (_lockObject)
            {
                rootNode = topLevelNode;
                Refresh(rootNode, GetExpandedRows());
            }
        }

        /// <summary>Populate the treeview.</summary>
        /// <param name="topLevelNode">A description of the top level root node</param>
        /// <param name="expandedNodes">A list of nodes to expand again after refreshing.</param>
        public void Populate(TreeViewNode topLevelNode, List<string> expandedNodes)
        {
            lock (_lockObject)
            {
                rootNode = topLevelNode;
                Refresh(rootNode, expandedNodes);
            }
        }

        /// <summary>Moves the specified node up 1 position.</summary>
        /// <param name="nodePath">The path of the node to move.</param>
        public void MoveUp(string nodePath)
        {
            lock (_lockObject)
            {
                if (FindNode(nodePath, out TreeIter node))
                {
                    TreePath path = _treemodel.GetPath(node);
                    TreeIter prevnode;
                    if (path.Prev() && _treemodel.GetIter(out prevnode, path))
                        _treemodel.MoveBefore(node, prevnode);

                    _treeview1.ScrollToCell(path, null, false, 0, 0);
                }
            }
        }

        /// <summary>Moves the specified node down 1 position.</summary>
        /// <param name="nodePath">The path of the node to move.</param>
        public void MoveDown(string nodePath)
        {
            lock (_lockObject)
            {
                if (FindNode(nodePath, out TreeIter node))
                {
                    TreePath path = _treemodel.GetPath(node);
                    TreeIter nextnode;
                    path.Next();
                    if (_treemodel.GetIter(out nextnode, path))
                        _treemodel.MoveAfter(node, nextnode);

                    _treeview1.ScrollToCell(path, null, false, 0, 0);
                }
            }
        }

        /// <summary>Renames the specified node path.</summary>
        /// <param name="nodePath">The node path.</param>
        /// <param name="newName">The new name for the node.</param>
        public void Rename(string nodePath, string newName)
        {
            lock (_lockObject)
            {
                if (FindNode(nodePath, out TreeIter node))
                {
                    _treemodel.SetValue(node, 0, newName);
                    previouslySelectedNodePath = GetFullPath(_treemodel.GetPath(node));
                }
            }
        }

        /// <summary>Puts the current node into edit mode so user can rename it.</summary>
        public void BeginRenamingCurrentNode()
        {
            lock (_lockObject)
            {
                if (!ReadOnly)
                {
                    _textRender.Editable = true;
                    TreePath selPath;
                    TreeViewColumn selCol;
                    _treeview1.GetCursor(out selPath, out selCol);
                    _treeview1.GrabFocus();
                    _treeview1.SetCursor(selPath, _treeview1.GetColumn(0), true);
                }
            }
        }

        /// <summary>Edit the text of the current node</summary>
        private void EndRenamingCurrentNode(string newText)
        {
            if (isEdittingNodeLabel == true)
            {
                _textRender.Editable = false;
                // TreeView.ContextMenuStrip = this.PopupMenu;
                if (Renamed != null && !string.IsNullOrEmpty(newText))
                {
                    NodeRenameArgs args = new NodeRenameArgs()
                    {
                        NodePath = this.nodePathBeforeRename,
                        NewName = newText
                    };
                    Renamed(this, args);
                    if (!args.CancelEdit)
                        previouslySelectedNodePath = args.NodePath;
                }
                isEdittingNodeLabel = false;
            }
        }

        private TreePath CreatePath(APSIMNG.Utility.TreeNode node)
        {
            lock (_lockObject)
            {
                return new TreePath(node.Indices);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expandedNodes"></param>
        public void ExpandNodes(APSIMNG.Utility.TreeNode[] expandedNodes)
        {
            lock (_lockObject)
            {
                foreach (var node in expandedNodes)
                    _treeview1.ExpandRow(CreatePath(node), false);
            }
        }

        public APSIMNG.Utility.TreeNode[] GetExpandedNodes()
        {
            lock (_lockObject)
            {
                List<APSIMNG.Utility.TreeNode> expandedRows = new List<APSIMNG.Utility.TreeNode>();
                _treeview1.MapExpandedRows((view, path) => expandedRows.Add(new APSIMNG.Utility.TreeNode(path.Indices)));
                return expandedRows.ToArray();
            }
        }

        /// <summary>Deletes the specified node.</summary>
        /// <param name="nodePath">The node path.</param>
        public void Delete(string nodePath)
        {
            lock (_lockObject)
            {
                if (!FindNode(nodePath, out TreeIter node))
                    return;

                // We will typically be deleting the currently selected node. If this is the case,
                // Gtk will not automatically move the cursor for us.
                // We need to work out where we want selection to be after this node is deleted
                TreePath cursorPath;
                TreeViewColumn cursorCol;
                _treeview1.GetCursor(out cursorPath, out cursorCol);
                TreeIter nextSel = node;
                TreePath pathToSelect = _treemodel.GetPath(node);
                if (pathToSelect.Compare(cursorPath) != 0)
                    pathToSelect = null;
                else if (_treemodel.IterNext(ref nextSel)) // If there's a "next" sibling, the current TreePath will do
                    pathToSelect = _treemodel.GetPath(nextSel);
                else
                {                                     // Otherwise
                    if (!pathToSelect.Prev())         // If there's a "previous" sibling, use that
                        pathToSelect.Up();            // and if that didn't work, use the parent
                }

                // Note: gtk_tree_store_remove() seems quite slow if the node being
                // deleted is selected. Therefore, we select the next node *before*
                // deleting the specified node.
                if (pathToSelect != null)
                    _treeview1.SetCursor(pathToSelect, _treeview1.GetColumn(0), false);

                _treemodel.Remove(ref node);
            }
        }

        /// <summary>Adds a child node.</summary>
        /// <param name="parentNodePath">The node path.</param>
        /// <param name="nodeDescription">The node description.</param>
        /// <param name="position">The position.</param>
        public void AddChild(string parentNodePath, TreeViewNode nodeDescription, int position = -1)
        {
            lock (_lockObject)
            {
                if (FindNode(parentNodePath, out TreeIter node))
                {
                    TreeIter iter;
                    if (position == -1)
                        iter = _treemodel.AppendNode(node);
                    else
                        iter = _treemodel.InsertNode(node, position);
                    RefreshNode(iter, nodeDescription);
                    _treeview1.ExpandToPath(_treemodel.GetPath(iter));
                }
            }
        }

        /// <summary>Return the position of the node under its parent</summary>
        /// <param name="path">The full node path.</param>
        public int GetNodePosition(string path)
        {
            lock (_lockObject)
            {
                int count = 0;
                if (FindNode(path, out TreeIter node))
                    while (_treemodel.IterPrevious(ref node))
                        count = count + 1;

                return count;
            }
        }

        /// <summary>
        /// Treeview is being destroyed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDestroyed(object sender, EventArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    _textRender.EditingStarted -= OnBeforeLabelEdit;
                    _textRender.Edited -= OnAfterLabelEdit;
                    _textRender.Dispose();

                    _treeview1.CursorChanged -= OnAfterSelect;
                    _treeview1.ButtonReleaseEvent -= OnButtonUp;
                    _treeview1.ButtonPressEvent -= OnButtonPress;
                    _treeview1.RowActivated -= OnRowActivated;
                    _treeview1.FocusInEvent -= OnTreeGainFocus;
                    _treeview1.FocusOutEvent -= OnTreeLoseFocus;
                    _treeview1.RowExpanded -= OnRowExpanded;
                    _treeview1.DragMotion -= OnDragOver;
                    _treeview1.DragDrop -= OnDragDrop;
                    _treeview1.DragBegin -= OnDragBegin;
                    _treeview1.DragDataGet -= OnDragDataGet;
                    _treeview1.DragDataReceived -= OnDragDataReceived;
                    _treeview1.DragEnd -= OnDragEnd;
                    timer.Elapsed -= OnTimerElapsed;
                    mainWidget.Destroyed -= OnDestroyed;

                    ContextMenu = null;
                    owner = null;
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>Refreshes the entire tree from the specified descriptions.</summary>
        /// <param name="nodeDescriptions">The nodes descriptions.</param>
        /// <param name="expandedNodes">A list of nodes to expand again after refreshing.</param>
        private void Refresh(TreeViewNode nodeDescriptions, List<string> expandedNodes)
        {
            //_treeview1.Model = new TreeStore(typeof(string), typeof(Gdk.Pixbuf), typeof(string), typeof(Color), typeof(bool));
            _treemodel.Clear();

            TreeIter iter = _treemodel.AppendNode();
            RefreshNode(iter, nodeDescriptions, false);
            
            _treeview1.ShowAll();
            _treeview1.ExpandRow(new TreePath("0"), false);

            // Expand all rows which were previously expanded by the user.
            expandedNodes.ForEach(row => _treeview1.ExpandRow(new TreePath(row), false));

            if (ContextMenu != null)
                ContextMenu.AttachToWidget(_treeview1);
        }

        /// <summary>
        /// Refresh the node at the given data.
        /// </summary>
        /// <param name="path">The node to refresh.</param>
        /// <param name="description">Data to use to refresh the node.</param>
        /// <remarks>
        /// This will not remove any existing children - but it will append new ones.
        /// If any children already exist, they must be removed before calling this function.
        /// </remarks>
        public void RefreshNode(string path, TreeViewNode description)
        {
            lock (_lockObject)
            {
                if (FindNode(path, out TreeIter iter))
                    RefreshNode(iter, description);
                else
                    throw new Exception($"Unable to refresh node - invalid path '{path}'");
            }
        }

        /// <summary>
        /// Configure the specified tree node using the fields in 'Description'.
        /// Recursively descends through all child nodes as well.
        /// </summary>
        /// <remarks>
        /// If any models have been deleted, calls to this function will not
        /// cause those models to be removed from the tree. When child models are
        /// updated, this function will attempt to update any existing tree nodes
        /// representing the children - if none exist, they will be added.
        /// </remarks>
        /// <param name="node">The node.</param>
        /// <param name="description">The description.</param>
        /// <param name="checkForExisting">
        /// If set to true, will attempt to update existing nodes instead of creating
        /// new ones, where possible. This should only be set to false when populating
        /// the tree control for the first time, and when set to false it will improve
        /// performance considerably, especially for large tree structures.
        /// </param>
        private void RefreshNode(TreeIter node, TreeViewNode description, bool checkForExisting = true)
        {
            Gdk.Pixbuf pixbuf = null;
            if (MasterView != null && MasterView.HasResource(description.ResourceNameForImage))
                pixbuf = new Gdk.Pixbuf(null, description.ResourceNameForImage);
            _treemodel.SetValues(node, description.Name, pixbuf, description.ToolTip, description.Colour, description.Strikethrough);

            foreach (TreeViewNode child in description.Children)
            {
                string path = GetFullPath(_treemodel.GetPath(node));
                TreeIter iter;
                if (checkForExisting)
                {
                    if (FindChild(node, child.Name, out TreeIter matchingChild))
                        iter = matchingChild;
                    else
                        iter = _treemodel.AppendNode(node);
                }
                else
                {
                    iter = _treemodel.AppendNode(node);
                }
                RefreshNode(iter, child);
            }
        }

        /// <summary>
        /// Find a child of a TreeIter with the specified name.
        /// Returns true iff a matching child was found.
        /// </summary>
        /// <param name="node">Node under which to search for a child.</param>
        /// <param name="name">Name of the child.</param>
        /// <param name="child">The matching child, if any is found.</param>
        private bool FindChild(TreeIter node, string name, out TreeIter child)
        {
            child = TreeIter.Zero;
            foreach (TreeIter c in GetChildren(node))
            {
                if (GetName(c) == name)
                {
                    child = c;
                    return true;
                }
            }
            return false;
        }

        private IEnumerable<TreeIter> GetChildren(TreeIter node)
        {
            if (_treemodel.IterChildren(out TreeIter child, node))
            {
                yield return child;
                while (_treemodel.IterNext(ref child))
                    yield return child;
            }
        }

        /// <summary>
        /// Return the name of the given node.
        /// </summary>
        /// <param name="node">The node.</param>
        private string GetName(TreeIter node)
        {
            return (string)_treemodel.GetValue(node, 0);
        }

        /// <summary>Return a string representation of the specified path.</summary>
        /// <param name="path">The path.</param>
        private string GetFullPath(TreePath path)
        {
            string result = "";
            if (path != null)
            {
                int[] ilist = path.Indices;
                TreeIter iter;
                _treemodel.IterNthChild(out iter, ilist[0]);
                result = "." + (string)_treemodel.GetValue(iter, 0);
                for (int i = 1; i < ilist.Length; i++)
                {
                    _treemodel.IterNthChild(out iter, iter, ilist[i]);
                    result += "." + (string)_treemodel.GetValue(iter, 0);
                }
            }
            return result;
        }

        /// <summary>
        /// Find a specific node with the node path.
        /// NodePath format: .Parent.Child.SubChild
        /// </summary>
        /// <param name="namePath">The name path.</param>
        /// <param name="result">The matching node.</param>
        /// <exception cref="System.Exception">Invalid name path ' + namePath + '</exception>
        private bool FindNode(string namePath, out TreeIter result)
        {
            if (!namePath.StartsWith(".", StringComparison.CurrentCulture))
                throw new Exception("Invalid name path '" + namePath + "'");

            namePath = namePath.Remove(0, 1); // Remove the leading '.'

            string[] namePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            result = TreeIter.Zero;
            TreeIter iter;
            if (!_treemodel.GetIterFirst(out iter))
                // The tree is empty.
                return false;
            for (int i = 0; i < namePathBits.Length; i++)
            {
                string pathBit = namePathBits[i];
                string nodeName = (string)_treemodel.GetValue(iter, 0);
                while (nodeName != pathBit && _treemodel.IterNext(ref iter))
                {
                    nodeName = (string)_treemodel.GetValue(iter, 0);
                }
                if (nodeName == pathBit)
                {
                    result = iter;
                    if (!_treemodel.IterChildren(out iter, iter) && i != namePathBits.Length - 1)
                        // We've found an ancestor but it has no children.
                        return false;
                }
                else
                    // Unable to locate an ancestor at this level.
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Allows for fine control over how each individual cell is rendered.
        /// </summary>
        /// <param name="col">Column in which the cell appears.</param>
        /// <param name="cell">
        /// The individual cells. 
        /// Any changes to this cell only affect this cell.
        /// The other cells in the column are unaffected.
        /// </param>
        /// <param name="model">
        /// The tree model which holds the data being displayed in the tree.
        /// </param>
        /// <param name="iter">
        /// TreeIter object associated with this cell in the tree. This object
        /// can be used for many things, such as retrieving this cell's data.
        /// </param>
        private void OnSetCellData(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            lock (_lockObject)
            {
                try
                {
                    // This makes a lot of assumptions about how the tree model is structured.
                    if (cell is CellRendererText)
                    {
                        Color colour = (Color)model.GetValue(iter, 3);
                        if (colour == Color.Empty && _treeview1.StyleContext != null)
                        {
                            Gdk.Color foreground = _treeview1.StyleContext.GetColor(StateFlags.Normal).ToColour().ToGdk();
                            colour = Colour.FromGtk(foreground);
                        }
                        (cell as CellRendererText).Strikethrough = (bool)model.GetValue(iter, 4);

                        // This is a bit of a hack which we use to convert a System.Drawing.Color
                        // to its hex string equivalent (e.g. #FF0000).
                        string hex = Colour.ToHex(colour);

                        string text = (string)model.GetValue(iter, 0);
                        (cell as CellRendererText).Markup = "<span foreground=\"" + hex + "\">" + text + "</span>";
                    }
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>User has selected a node. Raise event for presenter.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The EventArgs instance containing the event data.</param>
        private void OnAfterSelect(object sender, EventArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    if (SelectedNodeChanged != null && _treeview1 != null)
                    {
                        _treeview1.CursorChanged -= OnAfterSelect;
                        NodeSelectedArgs selectionChangedData = new NodeSelectedArgs();
                        selectionChangedData.OldNodePath = previouslySelectedNodePath;
                        TreePath selPath;
                        TreeViewColumn selCol;
                        _treeview1.GetCursor(out selPath, out selCol);
                        if (selPath != null) {
                            selectionChangedData.NewNodePath = GetFullPath(selPath);
                            if (selectionChangedData.NewNodePath != selectionChangedData.OldNodePath)
                                SelectedNodeChanged.Invoke(this, selectionChangedData);
                            previouslySelectedNodePath = selectionChangedData.NewNodePath;
                        }
                    }
                    else
                    {
                        // Presenter is ignoring the SelectedNodeChanged event.
                        // We should scroll to the newly selected node so the user
                        // can actually see what they've selected.
                        _treeview1.GetCursor(out TreePath path, out _);
                        _treeview1.ScrollToCell(path, null, false, 0, 1);
                    }
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
                finally
                {
                    if (SelectedNodeChanged != null && _treeview1 != null)
                        _treeview1.CursorChanged += OnAfterSelect;
                }
            }
        }

        /// <summary>
        /// Handle button press events to possibly begin editing an item name.
        /// This is in an attempt to rather slavishly follow Windows conventions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore]
        private void OnButtonPress(object sender, ButtonPressEventArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    timer.Stop();
                    if (e.Event.Button == 1 && e.Event.Type == Gdk.EventType.ButtonPress)
                    {
                        if(_treeview1.IsFocus) 
                        {
                            TreePath path;
                            TreeViewColumn col;
                            // Get the clicked location
                            if (_treeview1.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path, out col))
                            {
                                // See if the click was on the current selection
                                TreePath selPath;
                                TreeViewColumn selCol;
                                _treeview1.GetCursor(out selPath, out selCol);
                                if (selPath != null && path.Compare(selPath) == 0)
                                {
                                    // Check where on the row we are located, allowing 16 pixels for the image, and 2 for its border
                                    Gdk.Rectangle rect = _treeview1.GetCellArea(path, col);
                                    if (e.Event.X > rect.X + 18)
                                    {
                                        // We want this to be a bit longer than the double-click interval, which is normally 250 milliseconds
                                        timer.Interval = Settings.Default.DoubleClickTime + 10;
                                        timer.AutoReset = false;
                                        timer.Start();
                                    }
                                }
                            }
                        }
                        else
                        {
                            _treeview1.GrabFocus();
                        }
                    }
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>
        /// Timer has elapsed. Begin renaming node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    // Note - this will not be called on the main thread, so we
                    // need to wrap any Gtk calls inside an appropriate delegate
                    Gtk.Application.Invoke(delegate
                    {
                        try
                        {
                            BeginRenamingCurrentNode();
                        }
                        catch (Exception err)
                        {
                            ShowError(err);
                        }
                    });
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>
        /// A row in the tree view has been activated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRowActivated(object sender, RowActivatedArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    timer.Stop();
                    if (_treeview1.GetRowExpanded(e.Path))
                        _treeview1.CollapseRow(e.Path);
                    else
                        _treeview1.ExpandRow(e.Path, false);
                    e.RetVal = true;

                    DoubleClicked?.Invoke(this, new EventArgs());
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>
        /// A row in the tree view has been expanded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRowExpanded(object sender, RowExpandedArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    TreePath path = e.Path.Copy();
                    path.Down();
                    _treeview1.ScrollToCell(path, null, false, 0, 0);
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>
        /// Displays the popup menu when the right mouse button is released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonUp(object sender, ButtonReleaseEventArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    if (e.Event.Button == 3 && ContextMenu != null)
                        ContextMenu.Show();
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>Node has begun to be dragged.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragBegin(object sender, DragBeginArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    DragStartArgs args = new DragStartArgs();
                    args.NodePath = SelectedNode; // FullPath(e.Item as TreeNode);
                    if (DragStarted != null)
                    {
                        DragStarted(this, args);
                        if (args.DragObject != null)
                        {
                            sourcePathOfItemBeingDragged = args.NodePath;
                            dragSourceHandle = GCHandle.Alloc(args.DragObject);
                        }
                    }
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>
        /// A drag has completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragEnd(object sender, DragEndArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    EndRenaming();
                    if (dragSourceHandle.IsAllocated)
                    {
                        dragSourceHandle.Free();
                    }
                    sourcePathOfItemBeingDragged = null;
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>Node has been dragged over another node. Allow a drop here?</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragOver(object sender, DragMotionArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    // e.Effect = DragDropEffects.None;
                    e.RetVal = false;
                    Gdk.Drag.Status(e.Context, 0, e.Time); // Default to no drop

                    Gdk.Atom target = Drag.DestFindTarget(_treeview1, e.Context, null);
                    // Get the drop location
                    TreePath path;
                    TreeIter dest;
                    if (_treeview1.GetPathAtPos(e.X, e.Y, out path) && _treemodel.GetIter(out dest, path) &&
                        target != null && target != Gdk.Atom.Intern("GDK_NONE", false))
                    {
                        AllowDropArgs args = new AllowDropArgs();
                        args.NodePath = GetFullPath(path);
                        Drag.GetData(_treeview1, e.Context, target, e.Time);
                        if (dragDropData != null)
                        {
                            args.DragObject = dragDropData;
                            if (AllowDrop != null)
                            {
                                AllowDrop(this, args);
                                if (args.Allow)
                                {
                                    e.RetVal = true;
                                    string sourceParent = null;
                                    if (sourcePathOfItemBeingDragged != null)
                                        sourceParent = StringUtilities.ParentName(sourcePathOfItemBeingDragged);

                                    // Now determine the effect. If the drag originated from a different view 
                                    // (e.g. a toolbox or another file) then only copy is supported.
                                    if (sourcePathOfItemBeingDragged == null)
                                        Gdk.Drag.Status(e.Context, Gdk.DragAction.Copy, e.Time);
                                    else if (sourceParent == args.NodePath)
                                        Gdk.Drag.Status(e.Context, Gdk.DragAction.Copy, e.Time);
                                    else
                                        // The "SuggestedAction" will normally be Copy, but will be Move 
                                        // if shift is pressed, and Link if Ctrl-Shift is pressed
                                        Gdk.Drag.Status(e.Context, e.Context.SuggestedAction, e.Time);
                                }
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>Node has been dropped. Send to presenter.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragDataGet(object sender, DragDataGetArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    IntPtr data = (IntPtr)dragSourceHandle;
                    Int64 ptrInt = data.ToInt64();
                    Gdk.Atom target = Drag.DestFindTarget(sender as Widget, e.Context, null);
                    if (target != Gdk.Atom.Intern("GDK_NONE", false))
                        e.SelectionData.Set(target, 8, BitConverter.GetBytes(ptrInt));
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>
        /// Drag data has been received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragDataReceived(object sender, DragDataReceivedArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    var data = e.SelectionData.Data;
                    Int64 value = BitConverter.ToInt64(data, 0);
                    if (value != 0)
                    {
                        GCHandle handle = (GCHandle)new IntPtr(value);
                        dragDropData = (ISerializable)handle.Target;
                    }
                }
                catch (Exception err)
                {
                    if (err.Message.Contains("Arithmetic operation resulted in an overflow."))
                        ShowError(new Exception("Unable to add new model. Try adding the model again."));
                    else ShowError(err);
                }
            }
        }

        /// <summary>Node has been dropped. Send to presenter.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragDrop(object sender, DragDropArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    Gdk.Atom target = Drag.DestFindTarget(_treeview1, e.Context, null);
                    // Get the drop location
                    TreePath path;
                    TreeIter dest;
                    bool success = false;
                    if (_treeview1.GetPathAtPos(e.X, e.Y, out path) && _treemodel.GetIter(out dest, path) &&
                        target != Gdk.Atom.Intern("GDK_NONE", false))
                    {
                        AllowDropArgs args = new AllowDropArgs();
                        args.NodePath = GetFullPath(path);

                        Drag.GetData(_treeview1, e.Context, target, e.Time);
                        if (dragDropData != null)
                        {
                            DropArgs dropArgs = new DropArgs();
                            dropArgs.NodePath = GetFullPath(path);

                            dropArgs.DragObject = dragDropData;
                            Gdk.DragAction action = e.Context.SelectedAction;
                            if ((action & Gdk.DragAction.Move) == Gdk.DragAction.Move)
                                dropArgs.Moved = true;
                            else if ((action & Gdk.DragAction.Copy) == Gdk.DragAction.Copy)
                                dropArgs.Copied = true;
                            else
                                dropArgs.Linked = true;
                            Droped(this, dropArgs);
                            success = true;
                        }
                    }
                    Gtk.Drag.Finish(e.Context, success, e.Context.SelectedAction == Gdk.DragAction.Move, e.Time);
                    e.RetVal = success;
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>User is about to start renaming a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The EventArgs> instance containing the event data.</param>
        private void OnBeforeLabelEdit(object sender, EditingStartedArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    isEdittingNodeLabel = true;
                    nodePathBeforeRename = SelectedNode;
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>User has finished renaming a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The EventArgs instance containing the event data.</param>
        private void OnAfterLabelEdit(object sender, EditedArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    EndRenamingCurrentNode(e.NewText);
                }
                catch (Exception err)
                {
                    isEdittingNodeLabel = false;
                    ShowError(err);
                }
            }
        }

        /// <summary>
        /// Handle loss of focus by removing the accelerators from the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnTreeLoseFocus(object o, FocusOutEventArgs args)
        {
            lock (_lockObject)
            {
                try
                {
                    if (ContextMenu != null && acceleratorsAreAttached)
                    {
                        (_treeview1.Toplevel as Gtk.Window).RemoveAccelGroup(ContextMenu.Accelerators);
                        acceleratorsAreAttached = false;
                    }
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        /// <summary>
        /// Handle receiving focus by adding accelerators for the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnTreeGainFocus(object o, FocusInEventArgs args)
        {
            lock (_lockObject)
            {
                try
                {
                    // window is already in the list of acceleratables. Need to remove accelerators before we add them!
                    if (ContextMenu != null && !acceleratorsAreAttached)
                    {
                        (_treeview1.Toplevel as Gtk.Window).AddAccelGroup(ContextMenu.Accelerators);
                        acceleratorsAreAttached = true;
                    }
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }

        private void EndRenaming()
        {
            lock (_lockObject)
            {
                if (isEdittingNodeLabel)
                {
                    _textRender.StopEditing(true);
                    isEdittingNodeLabel = false;
                    nodePathBeforeRename = "";
                }
            }
        }

        /// <summary>
        /// Expands all child nodes recursively.
        /// </summary>
        /// <param name="path">Path to the node. e.g. ".Simulations.DataStore"</param>
        /// <param name="recursive">Recursively expand children too?</param>
        public void ExpandChildren(string path, bool recursive = true)
        {
            lock (_lockObject)
            {
                if (FindNode(path, out TreeIter node))
                    _treeview1.ExpandRow(_treemodel.GetPath(node), recursive);
            }
        }

        /// <summary>
        /// Collapses all child nodes recursively.
        /// </summary>
        /// <param name="path">Path to the node. e.g. ".Simulations.DataStore"</param>
        public void CollapseChildren(string path)
        {
            lock (_lockObject)
            {
                if (FindNode(path, out TreeIter node))
                    _treeview1.CollapseRow(_treemodel.GetPath(node));
            }
        }
    }
}