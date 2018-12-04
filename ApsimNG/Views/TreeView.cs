// -----------------------------------------------------------------------
// <copyright file="TreeView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
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
        private ISerializable DragDropData = null;
        private GCHandle dragSourceHandle;
        private CellRendererText textRender;
        private const string modelMime = "application/x-model-component";
        private Timer timer = new Timer();

        // If you add a new item to the tree model that is not at the end (e.g. add a bool as the third item), a lot of things will break.
        private TreeStore treemodel = new TreeStore(typeof(string), typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(Color), typeof(bool));

        /// <summary>Constructor</summary>
        public TreeView(ViewBase owner, Gtk.TreeView treeView) : base(owner)
        {
            treeview1 = treeView;
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
            textRender.Editable = true;
            TreePath selPath;
            TreeViewColumn selCol;
            treeview1.GetCursor(out selPath, out selCol);
            treeview1.GrabFocus();
            treeview1.SetCursor(selPath, treeview1.GetColumn(0), true);
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
            _mainWidget.Destroyed -= OnDestroyed;
            _owner = null;
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
            if (MasterView.HasResource(description.ResourceNameForImage))
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

            string[] NamePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            TreeIter result = TreeIter.Zero;
            TreeIter iter;
            treemodel.GetIterFirst(out iter);

            foreach (string PathBit in NamePathBits)
            {
                string nodeName = (string)treemodel.GetValue(iter, 0);
                while (nodeName != PathBit && treemodel.IterNext(ref iter))
                    nodeName = (string)treemodel.GetValue(iter, 0);
                if (nodeName == PathBit)
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
            // This makes a lot of assumptions about how the tree model is structured.
            if (cell is CellRendererText)
            {
                Color colour = (Color)model.GetValue(iter, 4);
                (cell as CellRendererText).Strikethrough = (bool)model.GetValue(iter, 5);
                //(cell as CellRendererText).ForegroundGdk = colour;

                // This is a bit of a hack which we use to convert a System.Drawing.Color
                // to its hex string equivalent (e.g. #FF0000).
                string hex = ColorTranslator.ToHtml(Color.FromArgb(colour.ToArgb()));

                string text = (string)model.GetValue(iter, 0);
                (cell as CellRendererText).Markup = "<span foreground=\"" + hex + "\">" + text + "</span>";
            }
        }

        /// <summary>User has selected a node. Raise event for presenter.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TreeViewEventArgs"/> instance containing the event data.</param>
        private void OnAfterSelect(object sender, EventArgs e)
        {
            if (SelectedNodeChanged != null)
            {
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

        /// <summary>
        /// Timer has elapsed. Begin renaming node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Gtk.Application.Invoke(delegate
            {
                BeginRenamingCurrentNode();
            });
        }

        /// <summary>
        /// A row in the tree view has been activated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRowActivated(object sender, RowActivatedArgs e)
        {
            timer.Stop();
            if (treeview1.GetRowExpanded(e.Path))
                treeview1.CollapseRow(e.Path);
            else
                treeview1.ExpandRow(e.Path, false);
            e.RetVal = true;
        }

        /// <summary>
        /// Displays the popup menu when the right mouse button is released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonUp(object sender, ButtonReleaseEventArgs e)
        {
            if (e.Event.Button == 3 && ContextMenu != null)
                ContextMenu.Show();
        }

        /// <summary>Node has begun to be dragged.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragBegin(object sender, DragBeginArgs e)
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

        /// <summary>
        /// A drag has completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragEnd(object sender, DragEndArgs e)
        {
            if (dragSourceHandle.IsAllocated)
            {
                dragSourceHandle.Free();
            }
            sourcePathOfItemBeingDragged = null;
        }

        /// <summary>Node has been dragged over another node. Allow a drop here?</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragOver(object sender, DragMotionArgs e)
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
                AllowDropArgs Args = new AllowDropArgs();
                Args.NodePath = GetFullPath(path);
                Drag.GetData(treeview1, e.Context, target, e.Time);
                if (DragDropData != null)
                {
                    Args.DragObject = DragDropData;
                    if (AllowDrop != null)
                    {
                        AllowDrop(this, Args);
                        if (Args.Allow)
                        {
                            e.RetVal = true;
                            string SourceParent = null;
                            if (sourcePathOfItemBeingDragged != null)
                                SourceParent = StringUtilities.ParentName(sourcePathOfItemBeingDragged);
                                                              
                            // Now determine the effect. If the drag originated from a different view 
                            // (e.g. a toolbox or another file) then only copy is supported.
                            if (sourcePathOfItemBeingDragged == null)
                                Gdk.Drag.Status(e.Context, Gdk.DragAction.Copy, e.Time);
                            else if (SourceParent == Args.NodePath)
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

        /// <summary>Node has been dropped. Send to presenter.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragDataGet(object sender, DragDataGetArgs e)
        {
            IntPtr data = (IntPtr)dragSourceHandle;
            Int64 ptrInt = data.ToInt64();
            Gdk.Atom target = Drag.DestFindTarget(sender as Widget, e.Context, null);
            if (target != Gdk.Atom.Intern("GDK_NONE", false))
               e.SelectionData.Set(target, 8, BitConverter.GetBytes(ptrInt));
        }

        /// <summary>
        /// Drag data has been received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragDataReceived(object sender, DragDataReceivedArgs e)
        {
            byte[] data = e.SelectionData.Data;
            Int64 value = BitConverter.ToInt64(data, 0);
            if (value != 0)
            {
                GCHandle handle = (GCHandle)new IntPtr(value);
                DragDropData = (ISerializable)handle.Target;
            }
        }

        /// <summary>Node has been dropped. Send to presenter.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragDrop(object sender, DragDropArgs e)
        {
            Gdk.Atom target = Drag.DestFindTarget(treeview1, e.Context, null);
                // Get the drop location
                TreePath path;
            TreeIter dest;
            bool success = false;
            if (treeview1.GetPathAtPos(e.X, e.Y, out path) && treemodel.GetIter(out dest, path) &&
                target != Gdk.Atom.Intern("GDK_NONE", false))                
            {
                AllowDropArgs Args = new AllowDropArgs();
                Args.NodePath = GetFullPath(path);

                Drag.GetData(treeview1, e.Context, target, e.Time);
                if (DragDropData != null)
                {
                    DropArgs args = new DropArgs();
                    args.NodePath = GetFullPath(path);

                    args.DragObject = DragDropData;
                    if (e.Context.Action == Gdk.DragAction.Copy)
                        args.Copied = true;
                    else if (e.Context.Action == Gdk.DragAction.Move)
                        args.Moved = true;
                    else
                        args.Linked = true;
                    Droped(this, args);
                    success = true;
                }
            }
            Gtk.Drag.Finish(e.Context, success, e.Context.Action == Gdk.DragAction.Move, e.Time);
            e.RetVal = success;
        }

        /// <summary>User is about to start renaming a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NodeLabelEditEventArgs"/> instance containing the event data.</param>
        private void OnBeforeLabelEdit(object sender, EditingStartedArgs e)
        {
            nodePathBeforeRename = SelectedNode;
            // TreeView.ContextMenuStrip = null;
            // e.CancelEdit = false;
        }
        
        /// <summary>User has finished renaming a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NodeLabelEditEventArgs"/> instance containing the event data.</param>
        private void OnAfterLabelEdit(object sender, EditedArgs e)
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

        /// <summary>
        /// Handle loss of focus by removing the accelerators from the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnTreeLoseFocus(object o, FocusOutEventArgs args)
        {
            if (ContextMenu != null)
                (treeview1.Toplevel as Gtk.Window).RemoveAccelGroup(ContextMenu.Accelerators);
        }

        /// <summary>
        /// Handle receiving focus by adding accelerators for the popup menu
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnTreeGainFocus(object o, FocusInEventArgs args)
        {
            if (ContextMenu != null)
                (treeview1.Toplevel as Gtk.Window).AddAccelGroup(ContextMenu.Accelerators);
        }
    }
}
