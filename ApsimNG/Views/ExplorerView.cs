// -----------------------------------------------------------------------
// <copyright file="ExplorerView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

// The basics are all here, but there are still a few things to be implemented:
// Drag and drop is pinning an object so we can pass its address around as data. Is there a better way?
// (Probably not really, as we go through a native layer, unless we can get by with the serialized XML).
// Shortcuts (accelerators in Gtk terminology) haven't yet been implemented.
// Link doesn't work, but it appears that move and link aren't working in the Windows.Forms implementation either.
// Actually, Move "works" here but doesn't undo correctly

namespace UserInterface.Views
{
    using EventArguments;
    using Glade;
    using Gtk;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;
    using APSIM.Shared.Utilities;
    /// <summary>
    /// An ExplorerView is a "Windows Explorer" like control that displays a virtual tree control on the left
    /// and a user interface on the right allowing the user to modify properties of whatever they
    /// click on in the tree control.
    /// </summary>
    /// <remarks>
    /// This "view" class follows the Humble Dialog approach were
    /// no business logic is embedded in this class. This object is told what to do by a
    /// presenter object which is responsible for populating all controls. The theory is that this
    /// class can be reused for different types of data.
    /// <para />
    /// When populating nodes, it is given a list of NodeDescription objects that describes what the
    /// nodes look like.
    /// <para />
    /// NB: All node paths are compatible with Model node paths and includes the root node name:
    /// If tree is:
    /// Simulations
    /// |
    /// +-- Test
    /// |
    /// +--- Clock
    /// e.g.  .Simulations.Test.Clock
    /// </remarks>
    public class ExplorerView : ViewBase, IExplorerView
    {
        /// <summary>The previously selected node path.</summary>
        private string previouslySelectedNodePath;

        /// <summary>The source path of item being dragged.</summary>
        private string sourcePathOfItemBeingDragged;

        /// <summary>The node path before rename.</summary>
        private string nodePathBeforeRename;

        [Widget]
        private VBox vbox1;
        [Widget]
        private Toolbar toolStrip = null;
        [Widget]
        private TreeView treeview1 = null;
        [Widget]
        private Viewport RightHandView = null;
        [Widget]
        private Label toolbarlabel = null;

        private Menu Popup = new Menu();

        private TreeStore treemodel = new TreeStore(typeof(string), typeof(Gdk.Pixbuf), typeof(string));
        private CellRendererText textRender;

        /// <summary>Default constructor for ExplorerView</summary>
        public ExplorerView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.ExplorerView.glade", "vbox1");
            gxml.Autoconnect(this);
            _mainWidget = vbox1;

            treeview1.Model = treemodel;
            TreeViewColumn column = new TreeViewColumn();
            CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf();
            column.PackStart(iconRender, false);
            textRender = new Gtk.CellRendererText();
            textRender.Editable = true;
            textRender.EditingStarted += OnBeforeLabelEdit;
            textRender.Edited += OnAfterLabelEdit;
            column.PackStart(textRender, true);
            column.SetAttributes(iconRender, "pixbuf", 1);
            column.SetAttributes(textRender, "text", 0);
//            column.SetCellDataFunc(textRender, treecelldatafunc);
            treeview1.AppendColumn(column);

            treeview1.CursorChanged += OnAfterSelect;
            treeview1.ButtonReleaseEvent += OnButtonUp;

            TargetEntry[] target_table = new TargetEntry[] {
               new TargetEntry ("application/x-model-component", TargetFlags.App, 0)
            };

            Gdk.DragAction actions = Gdk.DragAction.Copy | Gdk.DragAction.Link | Gdk.DragAction.Move;
            //treeview1.EnableModelDragDest(target_table, actions);
            //treeview1.EnableModelDragSource(Gdk.ModifierType.Button1Mask, target_table, actions);
            Drag.SourceSet(treeview1, Gdk.ModifierType.Button1Mask, target_table, actions);
            Drag.DestSet(treeview1, 0, target_table, actions);
            treeview1.DragMotion += OnDragOver;
            treeview1.DragDrop += OnDragDrop;
            treeview1.DragBegin += OnDragBegin;
            treeview1.DragDataGet += OnDragDataGet;
            treeview1.DragDataReceived += OnDragDataReceived;
            treeview1.DragEnd += OnDragEnd;
            treeview1.DragDataDelete += OnDragDataDelete;
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            if (RightHandView != null)
            {
                foreach (Widget child in RightHandView.Children)
                {
                    RightHandView.Remove(child);
                    child.Destroy();
                }
            }
            textRender.EditingStarted -= OnBeforeLabelEdit;
            textRender.Edited -= OnAfterLabelEdit;
            treeview1.CursorChanged -= OnAfterSelect;
            treeview1.ButtonReleaseEvent -= OnButtonUp;
            treeview1.DragMotion -= OnDragOver;
            treeview1.DragDrop -= OnDragDrop;
            treeview1.DragBegin -= OnDragBegin;
            treeview1.DragDataGet -= OnDragDataGet;
            treeview1.DragDataReceived -= OnDragDataReceived;
            treeview1.DragEnd -= OnDragEnd;
            treeview1.DragDataDelete -= OnDragDataDelete;
            foreach (Widget child in toolStrip.Children)
            {
                if (child is ToolButton)
                {
                    PropertyInfo pi = child.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (pi != null)
                    {
                        System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(child);
                        if (handlers != null && handlers.ContainsKey("clicked"))
                        {
                            EventHandler handler = (EventHandler)handlers["clicked"];
                            (child as ToolButton).Clicked -= handler;
                        }
                    }
                }
            }
            ClearPopup();
        }

        private void ClearPopup()
        {
            foreach (Widget w in Popup)
            {
                if (w is ImageMenuItem)
                {
                    PropertyInfo pi = w.GetType().GetProperty("AfterSignals", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (pi != null)
                    {
                        System.Collections.Hashtable handlers = (System.Collections.Hashtable)pi.GetValue(w);
                        if (handlers != null && handlers.ContainsKey("activate"))
                        {
                            EventHandler handler = (EventHandler)handlers["activate"];
                            (w as ImageMenuItem).Activated -= handler;
                        }
                    }
                    Popup.Remove(w);
                }
            }
        }

        //        public void treecelldatafunc(TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
        //        {
        //            If a grid value is numeric or a dateTime, we can set the text property of its renderer by formatting the value here, I think
        //            TreePath path = model.GetPath(iter);
        //            if (path.Depth > 1 && path.Indices[1] % 2 == 0)
        //            {
        //                col.Cells[0].Visible = true;
        //                col.Cells[1].Visible = false;
        //            }
        //            else
        //            {
        //                col.Cells[1].Visible = true;
        //                col.Cells[0].Visible = false;
        //            }
        //        }

        /// <summary>
        /// This event will be invoked when a node is selected not by the user
        /// but by an Undo command.
        /// </summary>
        public event EventHandler<NodeSelectedArgs> SelectedNodeChanged;

        /// <summary>
        /// Invoked when a drag operation has commenced. Need to create a DragObject.
        /// </summary>
        public event EventHandler<DragStartArgs> DragStarted;

        /// <summary>
        /// Invoked when the view wants to know if a drop is allowed on the specified Node.
        /// </summary>
        public event EventHandler<AllowDropArgs> AllowDrop;

        /// <summary>Invoked when a drop has occurred.</summary>
        public event EventHandler<DropArgs> Droped;

        /// <summary>Invoked then a node is renamed.</summary>
        public event EventHandler<NodeRenameArgs> Renamed;

        /// <summary>Invoked when a global key is pressed.</summary>
        public event EventHandler<KeysArgs> ShortcutKeyPressed;

        /// <summary>Refreshes the entire tree from the specified descriptions.</summary>
        /// <param name="nodeDescriptions">The nodes descriptions.</param>
        public void Refresh(NodeDescriptionArgs nodeDescriptions)
        {
            treemodel.Clear();
            TreeIter iter = treemodel.AppendNode();
            RefreshNode(iter, nodeDescriptions);
            treeview1.ShowAll();
            treeview1.ExpandRow(new TreePath("0"), false);
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
            previouslySelectedNodePath = FullPath(treemodel.GetPath(node));
        }

        /// <summary>Puts the current node into edit mode so user can rename it.</summary>
        public void BeginRenamingCurrentNode()
        {
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
            treemodel.Remove(ref node);
        }

        /// <summary>Adds a child node.</summary>
        /// <param name="parentNodePath">The node path.</param>
        /// <param name="nodeDescription">The node description.</param>
        /// <param name="position">The position.</param>
        public void AddChild(string parentNodePath, NodeDescriptionArgs nodeDescription, int position = -1)
        {
            TreeIter node = FindNode(parentNodePath);

            TreeIter iter;
            if (position == -1)
                iter = treemodel.AppendNode(node);
            else
                iter = treemodel.InsertNode(node, position);
            RefreshNode(iter, nodeDescription);
        }

        /// <summary>Gets or sets the currently selected node.</summary>
        /// <value>The selected node.</value>
        public string SelectedNode
        {
            get
            {
                TreePath selPath;
                TreeViewColumn selCol;
                treeview1.GetCursor(out selPath, out selCol);
                if (selPath != null)
                    return this.FullPath(selPath);
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

        /// <summary>Gets or sets the shortcut keys.</summary>
        /// <value>The shortcut keys.</value>
        public string[] ShortcutKeys { get; set; }

        /// <summary>
        /// Populate the main menu tool strip.
        /// We rebuild the contents from scratch
        /// </summary>
        /// <param name="menuDescriptions">Menu descriptions for each menu item.</param>
        public void PopulateMainToolStrip(List<MenuDescriptionArgs> menuDescriptions)
        {
            foreach (Widget child in toolStrip.Children)
                toolStrip.Remove(child);
            foreach (MenuDescriptionArgs description in menuDescriptions)
            {
                if (!hasResource(description.ResourceNameForImage))
                {
                    MessageDialog md = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok,
                        "Program error. Could not locate the resource named " + description.ResourceNameForImage);
                    md.Run();
                    md.Destroy();
                }
                else
                {

                    Gdk.Pixbuf pixbuf = new Gdk.Pixbuf(null, description.ResourceNameForImage, 20, 20);
                    ToolButton button = new ToolButton(new Gtk.Image(pixbuf), description.Name);
                    button.Homogeneous = false;
                    button.LabelWidget = new Label(description.Name);
                    Pango.FontDescription font = new Pango.FontDescription();
                    font.Size = (int)(8 * Pango.Scale.PangoScale);
                    button.LabelWidget.ModifyFont(font);
                    if (description.OnClick != null)
                        button.Clicked += description.OnClick;
                    toolStrip.Add(button);
                }
            }
            ToolItem item = new ToolItem();
            item.Expand = true;
            toolbarlabel = new Label();
            toolbarlabel.Xalign = 1.0F;
            toolbarlabel.Xpad = 10;
            toolbarlabel.ModifyFg(StateType.Normal, new Gdk.Color(0x99, 0x99, 0x99));
            item.Add(toolbarlabel);
            toolbarlabel.Visible = false;
            toolStrip.Add(item);
            toolStrip.ShowAll();
        }

        /// <summary>Populate the context menu from the descriptions passed in.</summary>
        /// <param name="menuDescriptions">Menu descriptions for each menu item.</param>
        public void PopulateContextMenu(List<MenuDescriptionArgs> menuDescriptions)
        {
            ClearPopup();
            ///AccelGroup accel = new AccelGroup();
            ///(treeview1.Toplevel as Window).AddAccelGroup(accel);
            foreach (MenuDescriptionArgs Description in menuDescriptions)
            {
                ImageMenuItem item = new ImageMenuItem(Description.Name);
                if (!String.IsNullOrEmpty(Description.ResourceNameForImage) && hasResource(Description.ResourceNameForImage) )
                    item.Image = new Image(null, Description.ResourceNameForImage);
                item.Activated += Description.OnClick;
                Popup.Append(item);

            }
            if (Popup.AttachWidget == null)
                Popup.AttachToWidget(treeview1, null);
            Popup.ShowAll();
            //Popup.Popup();


                /* TBI
                PopupMenu.Font = this.Font;
                PopupMenu.Items.Clear();
                foreach (MenuDescriptionArgs Description in menuDescriptions)
                {
                    Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(Description.ResourceNameForImage);
                    Bitmap Icon = null;
                    if (s != null)
                        Icon = new Bitmap(s);

                    ToolStripMenuItem Button = PopupMenu.Items.Add(Description.Name, Icon, Description.OnClick) as ToolStripMenuItem;
                    Button.TextImageRelation = TextImageRelation.ImageBeforeText;
                    Button.Checked = Description.Checked;
                    if (Description.ShortcutKey != null)
                    {
                        KeysConverter kc = new KeysConverter();
                        Button.ShortcutKeys = (Keys)kc.ConvertFromString(Description.ShortcutKey);
                    }
                    Button.Enabled = Description.Enabled;
                } */
            }

        /// <summary>Populates the static label on the toolbar.</summary>
        /// <param name="labelText">The label text.</param>
        /// <param name="labelToolTip">The label tool tip.</param>
        public void PopulateLabel(string labelText, string labelToolTip)
        {
            toolbarlabel.Text = labelText;
            toolbarlabel.TooltipText = labelToolTip;
            toolbarlabel.Visible = !String.IsNullOrEmpty(labelText);
        }

        /// <summary>
        /// Add a user control to the right hand panel. If Control is null then right hand panel will be cleared.
        /// </summary>
        /// <param name="control">The control to add.</param>
        public void AddRightHandView(object control)
        {
            foreach (Widget child in RightHandView.Children)
            {
                RightHandView.Remove(child);
                child.Destroy();
            }
            ViewBase view = control as ViewBase;
            if (view != null)
            {
                RightHandView.Add(view.MainWidget);
                RightHandView.ShowAll();
            }
        }

        /// <summary>Ask the user if they wish to save the simulation.</summary>
        /// <returns>Choice for saving the simulation</returns>
        public Int32 AskToSave()
        {
            /*
            TabbedExplorerView owner = Owner as TabbedExplorerView;
            if (owner != null)
            {
                Notebook notebook = owner.MainWidget as Notebook;
                string name = notebook.GetMenuLabelText(MainWidget);
                string message = "Do you want to save changes for " + name + " ?";
                MessageDialog md = new MessageDialog(MainWidget.Toplevel as Window, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, message);
                md.Title = "Save changes";
                int result = md.Run();
                md.Destroy();
                switch ((Gtk.ResponseType)result)
                {
                    case Gtk.ResponseType.Yes: return 0;
                    case Gtk.ResponseType.No: return 1;
                    default: return -1;
                }
            }
            */
            return -1;
        }

        /// <summary>A helper function that asks user for a folder.</summary>
        /// <param name="prompt"></param>
        /// <returns>
        /// Returns the selected folder or null if action cancelled by user.
        /// </returns>
        public string AskUserForFolder(string prompt)
        {
            string folderName = null;
            FileChooserDialog fileChooser = new FileChooserDialog(prompt, null, FileChooserAction.SelectFolder, "Cancel", ResponseType.Cancel, "Select", ResponseType.Accept);
            if (fileChooser.Run() == (int)ResponseType.Accept)
                folderName = fileChooser.Filename;
            fileChooser.Destroy();
            return folderName;
        }

        /// <summary>A helper function that asks user for a file.</summary>
        /// <param name="prompt"></param>
        /// <returns>
        /// Returns the selected file or null if action cancelled by user.
        /// </returns>
        public string AskUserForFile(string prompt)
        {
            string fileName = null;
            FileChooserDialog fileChooser = new FileChooserDialog(prompt, null, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Select", ResponseType.Accept);
            if (fileChooser.Run() == (int)ResponseType.Accept)
                fileName = fileChooser.Filename;
            fileChooser.Destroy();
            return fileName;
        }

        /// <summary>Show the wait cursor</summary>
        /// <param name="wait">If true will show the wait cursor otherwise the normal cursor.</param>
        public void ShowWaitCursor(bool wait)
        {
            WaitCursor = wait;
        }

        /// <summary>Gets or sets the width of the tree view.</summary>
        public Int32 TreeWidth
        {
            get { return treeview1.Allocation.Width; } 
            set { treeview1.WidthRequest = value; }
        }

        #region Protected & Privates

        /// <summary>
        /// Configure the specified tree node using the fields in 'Description'.
        /// Recursively descends through all child nodes as well.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="description">The description.</param>
        private void RefreshNode(TreeIter node, NodeDescriptionArgs description)
        {
            Gdk.Pixbuf pixbuf;
            if (hasResource(description.ResourceNameForImage))
                pixbuf = new Gdk.Pixbuf(null, description.ResourceNameForImage);
            else
                pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.TreeViewImages.Simulations.png"); // It there something else we could use as a default?

            treemodel.SetValues(node, description.Name, pixbuf, description.ToolTip);

            for (int i = 0; i < description.Children.Count; i++)
            {
                TreeIter iter = treemodel.AppendNode(node);
                RefreshNode(iter, description.Children[i]);
            }
        }

        /// <summary>Return a full path for the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        private string FullPath(TreePath path)
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

        /* TBI
        /// <summary>
        /// Override the process command key method so that we can implement global keyboard
        /// shortcuts.
        /// </summary>
        /// <param name="msg">The windows message to process</param>
        /// <param name="keyData">The key to process</param>
        /// <returns>True if command key was processed.</returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ShortcutKeyPressed != null && ShortcutKeys != null)
            {
                KeysConverter kc = new KeysConverter();
                string keyName = kc.ConvertToString(keyData);
                if (ShortcutKeys.Contains(keyName))
                {
                    ShortcutKeyPressed.Invoke(this, new KeysArgs() { Keys = keyData });
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Events
        */
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
                selectionChangedData.NewNodePath = FullPath(selPath);
                SelectedNodeChanged.Invoke(this, selectionChangedData);
                previouslySelectedNodePath = selectionChangedData.NewNodePath;
            }
        }

        private void OnButtonUp(object sender, ButtonReleaseEventArgs e)
        {
            if (e.Event.Button == 3)
                Popup.Popup();
        }

        // Looks like drag and drop is broken on Mono on Mac. The data being dragged needs to be
        // serializable which is ok but it still doesn work. Gives the error:
        //     System.Runtime.Serialization.SerializationException: Unexpected binary element: 46
        //     at System.Runtime.Serialization.Formatters.Binary.ObjectReader.ReadObject (BinaryElement element, System.IO.BinaryReader reader, System.Int64& objectId, System.Object& value, System.Runtime.Serialization.SerializationInfo& info) [0x00000] in <filename unknown>:0 

        private GCHandle dragSourceHandle;

        /// <summary>Node has begun to be dragged.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragBegin(object sender, DragBeginArgs e)
        {
            DragStartArgs args = new DragStartArgs();
            args.NodePath = SelectedNode; //  FullPath(e.Item as TreeNode);
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
            //e.Effect = DragDropEffects.None;
            e.RetVal = false;

            // Get the drop location
            TreePath path;
            TreeIter dest;
            if (treeview1.GetPathAtPos(e.X, e.Y, out path) && treemodel.GetIter(out dest, path))
            {
                AllowDropArgs Args = new AllowDropArgs();
                Args.NodePath = FullPath(path);
                Drag.GetData(treeview1, e.Context, e.Context.Targets[0], e.Time);
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
                        else
                            Gdk.Drag.Status(e.Context, 0, e.Time);
                    }
                }
            }
        }

        private ISerializable DragDropData = null;

        /// <summary>Node has been dropped. Send to presenter.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragDataGet(object sender, DragDataGetArgs e)
        {
            Gdk.Atom[] targets = e.Context.Targets;
            IntPtr data = (IntPtr)dragSourceHandle;
            Int64 ptrInt = data.ToInt64();
            e.SelectionData.Set(targets[0], 8, BitConverter.GetBytes(ptrInt));
        }

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
            // Get the drop location
            TreePath path;
            TreeIter dest;
            bool success = false;
            if (treeview1.GetPathAtPos(e.X, e.Y, out path) && treemodel.GetIter(out dest, path))
            {
                AllowDropArgs Args = new AllowDropArgs();
                Args.NodePath = FullPath(path);

                Drag.GetData(treeview1, e.Context, e.Context.Targets[0], e.Time);
                if (DragDropData != null)
                {
                    DropArgs args = new DropArgs();
                    args.NodePath = FullPath(path);

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

        /// <summary>
        /// Delete the source item at the end of a drag Move operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragDataDelete(object sender, DragDataDeleteArgs e) 
        {
            Delete(sourcePathOfItemBeingDragged);
        }

        /// <summary>User is about to start renaming a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NodeLabelEditEventArgs"/> instance containing the event data.</param>
        private void OnBeforeLabelEdit(object sender, EditingStartedArgs e)
        {
            nodePathBeforeRename = SelectedNode;
            //TreeView.ContextMenuStrip = null;
            //e.CancelEdit = false;
        }
        
        /// <summary>User has finished renamed a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NodeLabelEditEventArgs"/> instance containing the event data.</param>
        private void OnAfterLabelEdit(object sender, EditedArgs e)
        {
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
        /// Get whatever text is currently on the clipboard
        /// </summary>
        /// <returns></returns>
        public string GetClipboardText()
        {
            Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
            return cb.WaitForText();
        }

        /// <summary>
        /// Place text on the clipboard
        /// </summary>
        /// <param name="text"></param>
        public void SetClipboardText(string text)
        {
            Clipboard cb = MainWidget.GetClipboard(Gdk.Selection.Clipboard);
            cb.Text = text;
        }

        /*
        /// <summary>User has closed the status window.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void OnCloseStatusWindowClick(object sender, EventArgs e)
        {
            StatusWindow.Visible = false;
        }

         */
        #endregion

    }
}

