namespace UserInterface.Views
{
    using APSIM.Shared.Utilities;
    using Gtk;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Timers;

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
        private string previouslySelectedNodePath;
        private string sourcePathOfItemBeingDragged;
        private string nodePathBeforeRename;
        private Gtk.TreeView treeview1 = null;
        private TreeViewNode rootNode;
        private ISerializable dragDropData = null;
        private GCHandle dragSourceHandle;
        private CellRendererText textRender;
        private const string modelMime = "application/x-model-component";
        private Timer timer = new Timer();

        // If you add a new item to the tree model that is not at the end (e.g. add a bool as the third item), a lot of things will break.
        private TreeStore treemodel = new TreeStore(typeof(string), typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(Color), typeof(bool));

        /// <summary>Constructor</summary>
        public TreeView()
        {
        }

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
            treeview1 = (Gtk.TreeView) gtkControl;
            mainWidget = treeview1;
            treeview1.Model = treemodel;
            TreeViewColumn column = new TreeViewColumn();
            CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf();
            column.PackStart(iconRender, false);
            textRender = new Gtk.CellRendererText();
            textRender.Editable = false;
            textRender.EditingStarted += OnBeforeLabelEdit;
            textRender.Edited += OnAfterLabelEdit;
            column.PackStart(textRender, true);
            column.SetCellDataFunc(textRender, OnSetCellData);

            CellRendererText tickCell = new CellRendererText();
            tickCell.Editable = false;
            column.PackEnd(tickCell, false);
            column.SetAttributes(iconRender, "pixbuf", 1);
            column.SetAttributes(textRender, "text", 0);
            column.SetAttributes(tickCell, "text", 3);
            treeview1.AppendColumn(column);
            treeview1.TooltipColumn = 2;

            treeview1.CursorChanged += OnAfterSelect;
            treeview1.ButtonReleaseEvent += OnButtonUp;
            treeview1.ButtonPressEvent += OnButtonPress;
            treeview1.RowActivated += OnRowActivated;
            treeview1.FocusInEvent += OnTreeGainFocus;
            treeview1.FocusOutEvent += OnTreeLoseFocus;

            TargetEntry[] target_table = new TargetEntry[] {
               new TargetEntry(modelMime, TargetFlags.App, 0)
            };

            Gdk.DragAction actions = Gdk.DragAction.Copy | Gdk.DragAction.Link | Gdk.DragAction.Move;
            Drag.SourceSet(treeview1, Gdk.ModifierType.Button1Mask, target_table, actions);
            Drag.DestSet(treeview1, 0, target_table, actions);
            treeview1.DragMotion += OnDragOver;
            treeview1.DragDrop += OnDragDrop;
            treeview1.DragBegin += OnDragBegin;
            treeview1.DragDataGet += OnDragDataGet;
            treeview1.DragDataReceived += OnDragDataReceived;
            treeview1.DragEnd += OnDragEnd;
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
                TreePath selPath;
                TreeViewColumn selCol;
                treeview1.GetCursor(out selPath, out selCol);
                if (selPath != null)
                    return this.GetFullPath(selPath);
                else
                    return string.Empty;
            }

            set
            {
                if (SelectedNode != value && value != string.Empty)
                {
                    TreePath pathToSelect = treemodel.GetPath(FindNode(value));
                    if (pathToSelect != null)
                        treeview1.SetCursor(pathToSelect, treeview1.GetColumn(0), false);
                }
            }
        }

        /// <summary>Gets or sets the width of the tree view.</summary>
        public Int32 TreeWidth
        {
            get { return treeview1.Allocation.Width; }
            set { treeview1.WidthRequest = value; }
        }

        /// <summary>Gets or sets the popup menu of the tree view.</summary>
        public MenuView ContextMenu { get; set; }

        /// <summary>Populate the treeview.</summary>
        /// <param name="rootNode">A description of the top level root node</param>
        public void Populate(TreeViewNode topLevelNode)
        {
            rootNode = topLevelNode;
            Refresh(rootNode);
        }

        /// <summary>Moves the specified node up 1 position.</summary>
        /// <param name="nodePath">The path of the node to move.</param>
        public void MoveUp(string nodePath)
        {
            TreeIter node = FindNode(nodePath);
            TreePath path = treemodel.GetPath(node);
            TreeIter prevnode;
            if (path.Prev() && treemodel.GetIter(out prevnode, path))
                treemodel.MoveBefore(node, prevnode);

            treeview1.ScrollToCell(path, null, false, 0, 0);
        }

        /// <summary>Moves the specified node down 1 position.</summary>
        /// <param name="nodePath">The path of the node to move.</param>
        public void MoveDown(string nodePath)
        {
            TreeIter node = FindNode(nodePath);
            TreePath path = treemodel.GetPath(node);
            TreeIter nextnode;
            path.Next();
            if (treemodel.GetIter(out nextnode, path))
                treemodel.MoveAfter(node, nextnode);

            treeview1.ScrollToCell(path, null, false, 0, 0);
        }

        /// <summary>Renames the specified node path.</summary>
        /// <param name="nodePath">The node path.</param>
        /// <param name="newName">The new name for the node.</param>
        public void Rename(string nodePath, string newName)
        {
            TreeIter node = FindNode(nodePath);
            treemodel.SetValue(node, 0, newName);
            previouslySelectedNodePath = GetFullPath(treemodel.GetPath(node));
        }

        /// <summary>Puts the current node into edit mode so user can rename it.</summary>
        public void BeginRenamingCurrentNode()
        {
            if (!ReadOnly)
            {
                textRender.Editable = true;
                TreePath selPath;
                TreeViewColumn selCol;
                treeview1.GetCursor(out selPath, out selCol);
                treeview1.GrabFocus();
                treeview1.SetCursor(selPath, treeview1.GetColumn(0), true);
            }
        }

        private TreePath CreatePath(Utility.TreeNode node)
        {
            return new TreePath(node.Indices);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="expandedNodes"></param>
        public void ExpandNodes(Utility.TreeNode[] expandedNodes)
        {
            foreach (var node in expandedNodes)
                treeview1.ExpandRow(CreatePath(node), false);
        }

        public Utility.TreeNode[] GetExpandedNodes()
        {
            List<Utility.TreeNode> expandedRows = new List<Utility.TreeNode>();
            treeview1.MapExpandedRows((view, path) => expandedRows.Add(new Utility.TreeNode(path.Indices)));
            return expandedRows.ToArray();
        }

        /// <summary>Deletes the specified node.</summary>
        /// <param name="nodePath">The node path.</param>
        public void Delete(string nodePath)
        {
            TreeIter node = FindNode(nodePath);

            // We will typically be deleting the currently selected node. If this is the case,
            // Gtk will not automatically move the cursor for us.
            // We need to work out where we want selection to be after this node is deleted
            TreePath cursorPath;
            TreeViewColumn cursorCol;
            treeview1.GetCursor(out cursorPath, out cursorCol);
            TreeIter nextSel = node;
            TreePath pathToSelect = treemodel.GetPath(node);
            if (pathToSelect.Compare(cursorPath) != 0)
                pathToSelect = null;
            else if (!treemodel.IterNext(ref nextSel)) // If there's a "next" sibling, the current TreePath will do
            {                                     // Otherwise
                if (!pathToSelect.Prev())         // If there's a "previous" sibling, use that
                    pathToSelect.Up();            // and if that didn't work, use the parent
            }
            treemodel.Remove(ref node);
            if (pathToSelect != null)
                treeview1.SetCursor(pathToSelect, treeview1.GetColumn(0), false);
        }

        /// <summary>Adds a child node.</summary>
        /// <param name="parentNodePath">The node path.</param>
        /// <param name="nodeDescription">The node description.</param>
        /// <param name="position">The position.</param>
        public void AddChild(string parentNodePath, TreeViewNode nodeDescription, int position = -1)
        {
            TreeIter node = FindNode(parentNodePath);

            TreeIter iter;
            if (position == -1)
                iter = treemodel.AppendNode(node);
            else
                iter = treemodel.InsertNode(node, position);
            RefreshNode(iter, nodeDescription);
            treeview1.ExpandToPath(treemodel.GetPath(iter));
        }

        /// <summary>
        /// Treeview is being destroyed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDestroyed(object sender, EventArgs e)
        {
            try
            {
                textRender.EditingStarted -= OnBeforeLabelEdit;
                textRender.Edited -= OnAfterLabelEdit;
                treeview1.FocusInEvent -= OnTreeGainFocus;
                treeview1.FocusOutEvent -= OnTreeLoseFocus;
                treeview1.CursorChanged -= OnAfterSelect;
                treeview1.ButtonReleaseEvent -= OnButtonUp;
                treeview1.ButtonPressEvent -= OnButtonPress;
                treeview1.RowActivated -= OnRowActivated;
                treeview1.DragMotion -= OnDragOver;
                treeview1.DragDrop -= OnDragDrop;
                treeview1.DragBegin -= OnDragBegin;
                treeview1.DragDataGet -= OnDragDataGet;
                treeview1.DragDataReceived -= OnDragDataReceived;
                treeview1.DragEnd -= OnDragEnd;
                timer.Elapsed -= OnTimerElapsed;
                ContextMenu = null;

                mainWidget.Destroyed -= OnDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Refreshes the entire tree from the specified descriptions.</summary>
        /// <param name="nodeDescriptions">The nodes descriptions.</param>
        private void Refresh(TreeViewNode nodeDescriptions)
        {
            // Record which rows are currently expanded.
            // We can't directly use a TreePath to an outdated TreeModel/TreeView, so we store the path as a string, and 
            // then parse that into a new TreePath after the model is reassembled. This assumes that the structure of the 
            // tree view/model does not change when RefreshNode() is called (e.g. it still has the same number of rows/columns).
            List<string> expandedRows = new List<string>();
            treeview1.MapExpandedRows(new TreeViewMappingFunc((tree, path) =>
            {
                expandedRows.Add(path.ToString());
            }));
            treemodel.Clear();
            TreeIter iter = treemodel.AppendNode();
            RefreshNode(iter, nodeDescriptions);
            treeview1.ShowAll();
            treeview1.ExpandRow(new TreePath("0"), false);
            // Expand all rows which were previously expanded by the user.
            try
            {
                expandedRows.ForEach(row => treeview1.ExpandRow(new TreePath(row), false));
            }
            catch
            {
            }

            if (ContextMenu != null)
                ContextMenu.AttachToWidget(treeview1);
        }

        /// <summary>
        /// Configure the specified tree node using the fields in 'Description'.
        /// Recursively descends through all child nodes as well.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="description">The description.</param>
        private void RefreshNode(TreeIter node, TreeViewNode description)
        {
            Gdk.Pixbuf pixbuf = null;
            if (MasterView != null && MasterView.HasResource(description.ResourceNameForImage))
                pixbuf = new Gdk.Pixbuf(null, description.ResourceNameForImage);
            string tick = description.Checked ? "✔" : "";
            treemodel.SetValues(node, description.Name, pixbuf, description.ToolTip, tick, description.Colour, description.Strikethrough);

            for (int i = 0; i < description.Children.Count; i++)
            {
                TreeIter iter = treemodel.AppendNode(node);
                RefreshNode(iter, description.Children[i]);
            }
        }

        /// <summary>Return a full path for the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        private string GetFullPath(TreePath path)
        {
            string result = "";
            if (path != null)
            {
                int[] ilist = path.Indices;
                TreeIter iter;
                treemodel.IterNthChild(out iter, ilist[0]);
                result = "." + (string)treemodel.GetValue(iter, 0);
                for (int i = 1; i < ilist.Length; i++)
                {
                   treemodel.IterNthChild(out iter, iter, ilist[i]);
                   result += "." + (string)treemodel.GetValue(iter, 0);
                }
            }
            return result;
        }

        /// <summary>
        /// Find a specific node with the node path.
        /// NodePath format: .Parent.Child.SubChild
        /// </summary>
        /// <param name="namePath">The name path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Invalid name path ' + namePath + '</exception>
        private TreeIter FindNode(string namePath)
        {
            if (!namePath.StartsWith(".", StringComparison.CurrentCulture))
                throw new Exception("Invalid name path '" + namePath + "'");

            namePath = namePath.Remove(0, 1); // Remove the leading '.'

            string[] namePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            TreeIter result = TreeIter.Zero;
            TreeIter iter;
            treemodel.GetIterFirst(out iter);

            foreach (string pathBit in namePathBits)
            {
                string nodeName = (string)treemodel.GetValue(iter, 0);
                while (nodeName != pathBit && treemodel.IterNext(ref iter))
                    nodeName = (string)treemodel.GetValue(iter, 0);
                if (nodeName == pathBit)
                {
                    result = iter;
                    TreePath path = treemodel.GetPath(iter);
                    if (!treeview1.GetRowExpanded(path))
                        treeview1.ExpandRow(path, false);
                    treemodel.IterChildren(out iter, iter);
                }
                else
                    return TreeIter.Zero;
            }
            return result;         
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
            try
            {
                // This makes a lot of assumptions about how the tree model is structured.
                if (cell is CellRendererText)
                {
                    Color colour = (Color)model.GetValue(iter, 4);
                    if (colour == Color.Empty)
                        colour = Utility.Colour.FromGtk(treeview1.Style.Foreground(StateType.Normal));
                    (cell as CellRendererText).Strikethrough = (bool)model.GetValue(iter, 5);

                    // This is a bit of a hack which we use to convert a System.Drawing.Color
                    // to its hex string equivalent (e.g. #FF0000).
                    string hex = Utility.Colour.ToHex(colour);

                    string text = (string)model.GetValue(iter, 0);
                    (cell as CellRendererText).Markup = "<span foreground=\"" + hex + "\">" + text + "</span>";
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>User has selected a node. Raise event for presenter.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TreeViewEventArgs"/> instance containing the event data.</param>
        private void OnAfterSelect(object sender, EventArgs e)
        {
            try
            {
                if (SelectedNodeChanged != null)
                {
                    treeview1.CursorChanged -= OnAfterSelect;
                    NodeSelectedArgs selectionChangedData = new NodeSelectedArgs();
                    selectionChangedData.OldNodePath = previouslySelectedNodePath;
                    TreePath selPath;
                    TreeViewColumn selCol;
                    treeview1.GetCursor(out selPath, out selCol);
                    selectionChangedData.NewNodePath = GetFullPath(selPath);
                    if (selectionChangedData.NewNodePath != selectionChangedData.OldNodePath)
                        SelectedNodeChanged.Invoke(this, selectionChangedData);
                    previouslySelectedNodePath = selectionChangedData.NewNodePath;
                }
                else
                {
                    // Presenter is ignoring the SelectedNodeChanged event.
                    // We should scroll to the newly selected node so the user
                    // can actually see what they've selected.
                    treeview1.GetCursor(out TreePath path, out _);
                    treeview1.ScrollToCell(path, null, false, 0, 1);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
            finally
            {
                if (SelectedNodeChanged != null && treeview1 != null)
                    treeview1.CursorChanged += OnAfterSelect;
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
            try
            {
                timer.Stop();
                if (e.Event.Button == 1 && e.Event.Type == Gdk.EventType.ButtonPress)
                {
                    TreePath path;
                    TreeViewColumn col;
                    // Get the clicked location
                    if (treeview1.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path, out col))
                    {
                        // See if the click was on the current selection
                        TreePath selPath;
                        TreeViewColumn selCol;
                        treeview1.GetCursor(out selPath, out selCol);
                        if (selPath != null && path.Compare(selPath) == 0)
                        {
                            // Check where on the row we are located, allowing 16 pixels for the image, and 2 for its border
                            Gdk.Rectangle rect = treeview1.GetCellArea(path, col);
                            if (e.Event.X > rect.X + 18)
                            {
                                timer.Interval = treeview1.Settings.DoubleClickTime + 10;  // We want this to be a bit longer than the double-click interval, which is normally 250 milliseconds
                                timer.AutoReset = false;
                                timer.Start();
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

        /// <summary>
        /// Timer has elapsed. Begin renaming node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Gtk.Application.Invoke(delegate
                {
                    BeginRenamingCurrentNode();
                });
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// A row in the tree view has been activated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRowActivated(object sender, RowActivatedArgs e)
        {
            try
            {
                timer.Stop();
                if (treeview1.GetRowExpanded(e.Path))
                    treeview1.CollapseRow(e.Path);
                else
                    treeview1.ExpandRow(e.Path, false);
                e.RetVal = true;

                DoubleClicked?.Invoke(this, new EventArgs());
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Displays the popup menu when the right mouse button is released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonUp(object sender, ButtonReleaseEventArgs e)
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

        /// <summary>Node has begun to be dragged.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragBegin(object sender, DragBeginArgs e)
        {
            try
            {
                if (textRender.Editable) // If the node to be dragged is open for editing (renaming), close it now.
                    textRender.StopEditing(true);
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

        /// <summary>
        /// A drag has completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragEnd(object sender, DragEndArgs e)
        {
            try
            {
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

        /// <summary>Node has been dragged over another node. Allow a drop here?</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragOver(object sender, DragMotionArgs e)
        {
            try
            {
                // e.Effect = DragDropEffects.None;
                e.RetVal = false;
                Gdk.Drag.Status(e.Context, 0, e.Time); // Default to no drop

                Gdk.Atom target = Drag.DestFindTarget(treeview1, e.Context, null);
                // Get the drop location
                TreePath path;
                TreeIter dest;
                if (treeview1.GetPathAtPos(e.X, e.Y, out path) && treemodel.GetIter(out dest, path) &&
                    target != Gdk.Atom.Intern("GDK_NONE", false))
                {
                    AllowDropArgs args = new AllowDropArgs();
                    args.NodePath = GetFullPath(path);
                    Drag.GetData(treeview1, e.Context, target, e.Time);
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

        /// <summary>Node has been dropped. Send to presenter.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragDataGet(object sender, DragDataGetArgs e)
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

        /// <summary>
        /// Drag data has been received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragDataReceived(object sender, DragDataReceivedArgs e)
        {
            try
            {
                byte[] data = e.SelectionData.Data;
                Int64 value = BitConverter.ToInt64(data, 0);
                if (value != 0)
                {
                    GCHandle handle = (GCHandle)new IntPtr(value);
                    dragDropData = (ISerializable)handle.Target;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Node has been dropped. Send to presenter.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragDrop(object sender, DragDropArgs e)
        {
            try
            {
                Gdk.Atom target = Drag.DestFindTarget(treeview1, e.Context, null);
                // Get the drop location
                TreePath path;
                TreeIter dest;
                bool success = false;
                if (treeview1.GetPathAtPos(e.X, e.Y, out path) && treemodel.GetIter(out dest, path) &&
                    target != Gdk.Atom.Intern("GDK_NONE", false))
                {
                    AllowDropArgs args = new AllowDropArgs();
                    args.NodePath = GetFullPath(path);

                    Drag.GetData(treeview1, e.Context, target, e.Time);
                    if (dragDropData != null)
                    {
                        DropArgs dropArgs = new DropArgs();
                        dropArgs.NodePath = GetFullPath(path);

                        dropArgs.DragObject = dragDropData;
                        if (e.Context.Action == Gdk.DragAction.Copy)
                            dropArgs.Copied = true;
                        else if (e.Context.Action == Gdk.DragAction.Move)
                            dropArgs.Moved = true;
                        else
                            dropArgs.Linked = true;
                        Droped(this, dropArgs);
                        success = true;
                    }
                }
                Gtk.Drag.Finish(e.Context, success, e.Context.Action == Gdk.DragAction.Move, e.Time);
                e.RetVal = success;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>User is about to start renaming a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NodeLabelEditEventArgs"/> instance containing the event data.</param>
        private void OnBeforeLabelEdit(object sender, EditingStartedArgs e)
        {
            try
            {
                nodePathBeforeRename = SelectedNode;
                // TreeView.ContextMenuStrip = null;
                // e.CancelEdit = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
        
        /// <summary>User has finished renaming a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NodeLabelEditEventArgs"/> instance containing the event data.</param>
        private void OnAfterLabelEdit(object sender, EditedArgs e)
        {
            try
            {
                textRender.Editable = false;
                // TreeView.ContextMenuStrip = this.PopupMenu;
                if (Renamed != null && !string.IsNullOrEmpty(e.NewText))
                {
                    NodeRenameArgs args = new NodeRenameArgs()
                    {
                        NodePath = this.nodePathBeforeRename,
                        NewName = e.NewText
                    };
                    Renamed(this, args);
                    if (!args.CancelEdit)
                        previouslySelectedNodePath = args.NodePath;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handle loss of focus by removing the accelerators from the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnTreeLoseFocus(object o, FocusOutEventArgs args)
        {
            try
            {
                if (ContextMenu != null)
                    (treeview1.Toplevel as Gtk.Window).RemoveAccelGroup(ContextMenu.Accelerators);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Handle receiving focus by adding accelerators for the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnTreeGainFocus(object o, FocusInEventArgs args)
        {
            try
            {
                if (ContextMenu != null)
                    (treeview1.Toplevel as Gtk.Window).AddAccelGroup(ContextMenu.Accelerators);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Expands all child nodes recursively.
        /// </summary>
        /// <param name="path">Path to the node. e.g. ".Simulations.DataStore"</param>
        public void ExpandChildren(string path)
        {
            TreePath nodePath = treemodel.GetPath(FindNode(path));
            treeview1.ExpandRow(nodePath, true);
        }

        /// <summary>
        /// Collapses all child nodes recursively.
        /// </summary>
        /// <param name="path">Path to the node. e.g. ".Simulations.DataStore"</param>
        public void CollapseChildren(string path)
        {
            TreePath nodePath = treemodel.GetPath(FindNode(path));
            treeview1.CollapseRow(nodePath);
        }
    }
}
