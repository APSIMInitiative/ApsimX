// -----------------------------------------------------------------------
// <copyright file="ExplorerView.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Windows.Forms;
    using Commands;
    using EventArguments;
    using Interfaces;
    using Views;
    using System.Reflection;
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
    public partial class ExplorerView : UserControl, IExplorerView
    {
        /// <summary>The previously selected node path.</summary>
        private string previouslySelectedNodePath;

        /// <summary>The source path of item being dragged.</summary>
        private string sourcePathOfItemBeingDragged;

        /// <summary>The node path before rename.</summary>
        private string nodePathBeforeRename;

        /// <summary>Default constructor for ExplorerView</summary>
        public ExplorerView()
        {
            this.InitializeComponent();
            StatusWindow.Visible = false;
        }

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
        public new event EventHandler<AllowDropArgs> AllowDrop;

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
            TreeView.Nodes.Clear();
            TreeNode rootNode = new TreeNode();
            TreeView.Nodes.Add(rootNode);
            RefreshNode(rootNode, nodeDescriptions);
            TreeView.Nodes[0].Expand(); // expand the root tree node
        }

        /// <summary>Moves the specified node up 1 position.</summary>
        /// <param name="nodePath">The path of the node to move.</param>
        public void MoveUp(string nodePath)
        {
            TreeNode node = FindNode(nodePath);
            if (node.Index > 0)
            {
                TreeView.AfterSelect -= OnAfterSelect;
                int pos = node.Index - 1;
                TreeNode parent = node.Parent;
                parent.Nodes.Remove(node);
                parent.Nodes.Insert(pos, node);
                TreeView.SelectedNode = node;

                TreeView.AfterSelect += OnAfterSelect;
            }
        }

        /// <summary>Moves the specified node down 1 position.</summary>
        /// <param name="nodePath">The path of the node to move.</param>
        public void MoveDown(string nodePath)
        {
            TreeNode node = FindNode(nodePath);
            if (node.Index < node.Parent.Nodes.Count - 1)
            {
                TreeView.AfterSelect -= OnAfterSelect;

                int pos = node.Index + 1;
                TreeNode parent = node.Parent;
                parent.Nodes.Remove(node);
                parent.Nodes.Insert(pos, node);
                TreeView.SelectedNode = node;

                TreeView.AfterSelect += OnAfterSelect;

            }
        }

        /// <summary>Renames the specified node path.</summary>
        /// <param name="nodePath">The node path.</param>
        /// <param name="newName">The new name for the node.</param>
        public void Rename(string nodePath, string newName)
        {
            TreeNode node = FindNode(nodePath);
            node.Text = newName;
            previouslySelectedNodePath = FullPath(node);
        }

        /// <summary>Puts the current node into edit mode so user can rename it.</summary>
        public void BeginRenamingCurrentNode()
        {
            TreeView.SelectedNode.BeginEdit();
        }

        /// <summary>Deletes the specified node.</summary>
        /// <param name="nodePath">The node path.</param>
        public void Delete(string nodePath)
        {
            TreeNode node = FindNode(nodePath);
            node.Remove();
        }

        /// <summary>Adds a child node.</summary>
        /// <param name="parentNodePath">The node path.</param>
        /// <param name="nodeDescription">The node description.</param>
        /// <param name="position">The position.</param>
        public void AddChild(string parentNodePath, NodeDescriptionArgs nodeDescription, int position = -1)
        {
            TreeNode node = FindNode(parentNodePath);

            TreeNode childNode = new TreeNode();
            if (position == -1)
                node.Nodes.Add(childNode);
            else
                node.Nodes.Insert(position, childNode);
            RefreshNode(childNode, nodeDescription);
        }

        /// <summary>Gets or sets the currently selected node.</summary>
        /// <value>The selected node.</value>
        public string SelectedNode
        {
            get
            {
                if (TreeView.SelectedNode != null)
                    return this.FullPath(TreeView.SelectedNode);
                else
                    return string.Empty;
            }

            set
            {
                if (SelectedNode != value && value != string.Empty)
                {
                    // We want the BeforeSelect event to only fire when user clicks on a node
                    // in the tree.
                    TreeView.AfterSelect -= this.OnAfterSelect;

                    TreeNode nodeToSelect = FindNode(value);
                    if (nodeToSelect != null)
                    {
                        previouslySelectedNodePath = SelectedNode;
                        TreeView.SelectedNode = nodeToSelect;
                    }

                    TreeView.AfterSelect += OnAfterSelect;
                }
            }
        }

        /// <summary>Gets or sets the shortcut keys.</summary>
        /// <value>The shortcut keys.</value>
        public Keys[] ShortcutKeys { get; set; }

        /// <summary>Populate the main menu tool strip.</summary>
        /// <param name="menuDescriptions">Menu descriptions for each menu item.</param>
        public void PopulateMainToolStrip(List<MenuDescriptionArgs> menuDescriptions)
        {
            ToolStrip.Items.Clear();
            ToolStrip.Font = this.Font;
            foreach (MenuDescriptionArgs description in menuDescriptions)
            {
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(description.ResourceNameForImage);
                Bitmap icon = null;
                if (s != null)
                    icon = new Bitmap(s);
                ToolStripItem button = ToolStrip.Items.Add(description.Name, icon, description.OnClick);
                button.TextImageRelation = TextImageRelation.ImageAboveText;
            }
            ToolStrip.Visible = ToolStrip.Items.Count > 0;
        }

        /// <summary>Populate the context menu from the descriptions passed in.</summary>
        /// <param name="menuDescriptions">Menu descriptions for each menu item.</param>
        public void PopulateContextMenu(List<MenuDescriptionArgs> menuDescriptions)
        {
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
                Button.ShortcutKeys = Description.ShortcutKey;
                Button.Enabled = Description.Enabled;
            }
        }

        /// <summary>Populates the static label on the toolbar.</summary>
        /// <param name="labelText">The label text.</param>
        /// <param name="labelToolTip">The label tool tip.</param>
        public void PopulateLabel(string labelText, string labelToolTip)
        {
            // Add in version information as a label.
            ToolStripLabel label = new ToolStripLabel();
            label.ForeColor = Color.Gray;
            label.Alignment = ToolStripItemAlignment.Right;
            label.Text = labelText;
            label.ToolTipText = labelToolTip;
            ToolStrip.Items.Add(label);
        }

        /// <summary>
        /// Add a user control to the right hand panel. If Control is null then right hand panel will be cleared.
        /// </summary>
        /// <param name="control">The control to add.</param>
        public void AddRightHandView(UserControl control)
        {
            RightHandPanel.Controls.Clear();
            if (control != null)
            {
                RightHandPanel.Controls.Add(control);
                control.Dock = DockStyle.Fill;
            }
        }

        /// <summary>Ask the user if they wish to save the simulation.</summary>
        /// <returns>Choice for saving the simulation</returns>
        public Int32 AskToSave()
        {
            TabPage page = Parent as TabPage;
            DialogResult result = MessageBox.Show("Do you want to save changes for " + page.Text + " ?", "Save changes", MessageBoxButtons.YesNoCancel);
            switch (result)
            {
                case DialogResult.Cancel: return -1;
                case DialogResult.Yes: return 0;
                case DialogResult.No: return 1;
                default: return -1;
            }
        }

        /// <summary>A helper function that asks user for a folder.</summary>
        /// <param name="prompt"></param>
        /// <returns>
        /// Returns the selected folder or null if action cancelled by user.
        /// </returns>
        public string AskUserForFolder(string prompt)
        {
            folderBrowserDialog1.Description = prompt;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                return folderBrowserDialog1.SelectedPath;
            else
                return null;
        }

        /// <summary>Add a status message to the explorer window</summary>
        /// <param name="message">The message.</param>
        /// <param name="errorLevel">The error level.</param>
        public void ShowMessage(string message, Models.DataStore.ErrorLevel errorLevel)
        {
            StatusWindow.Visible = message != null;

            // Output the message
            if (errorLevel == Models.DataStore.ErrorLevel.Error)
            {
                StatusWindow.ForeColor = Color.Red;
            }
            else if (errorLevel == Models.DataStore.ErrorLevel.Warning)
            {
                StatusWindow.ForeColor = Color.Brown;
            }
            else
            {
                StatusWindow.ForeColor = Color.Blue;
            }
            message = message.TrimEnd("\n".ToCharArray());
            message = message.Replace("\n", "\n                      ");
            message += "\n";
            StatusWindow.Text = message;
            this.toolTip1.SetToolTip(this.StatusWindow, message);
        }

        /// <summary>
        /// A helper function that asks user for a SaveAs name and returns their new choice.
        /// </summary>
        /// <param name="oldFilename">The old filename.</param>
        /// <returns>
        /// Returns the new file name or null if action cancelled by user.
        /// </returns>
        public string SaveAs(string oldFilename)
        {
            TabbedExplorerView parentView = this.Parent.Parent.Parent as TabbedExplorerView;
            return parentView.AskUserForSaveFileName(oldFilename);
        }

        /// <summary>Change the name of the tab.</summary>
        /// <param name="newTabName">New name of the tab.</param>
        public void ChangeTabText(string newTabName)
        {
            TabPage page = Parent as TabPage;
            page.Text = newTabName;
        }

        /// <summary>Toggle the 2nd right hand side explorer view on/off</summary>
        public void ToggleSecondExplorerViewVisible()
        {
            MainForm mainForm = Application.OpenForms[0] as MainForm;
            mainForm.ToggleSecondExplorerViewVisible();
        }

        /// <summary>Gets or sets the width of the tree view.</summary>
        public Int32 TreeWidth
        {
            get { return TreeView.Width; }
            set { TreeView.Width = value; }
        }

        #region Protected & Privates

        /// <summary>
        /// Configure the specified tree node using the fields in 'Description'.
        /// Recursively descends through all child nodes as well.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="description">The description.</param>
        private void RefreshNode(TreeNode node, NodeDescriptionArgs description)
        {
            node.Text = description.Name;

            // Make sure the tree node image is right.
            int imageIndex = TreeImageList.Images.IndexOfKey(description.ResourceNameForImage);
            if (imageIndex == -1)
            {
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(description.ResourceNameForImage);
                if (s != null)
                {
                    Bitmap Icon = new Bitmap(s);

                    if (Icon != null)
                    {
                        TreeImageList.Images.Add(description.ResourceNameForImage, Icon);
                        imageIndex = TreeImageList.Images.Count - 1;
                    }
                }
            }
            node.ImageIndex = imageIndex;
            node.SelectedImageIndex = imageIndex;

            // Make sure we have the right number of child nodes.
            // Add extra nodes if necessary
            while (description.Children.Count > node.Nodes.Count)
                node.Nodes.Add(new TreeNode());

            // Remove unwanted nodes if necessary.
            while (description.Children.Count < node.Nodes.Count)
                node.Nodes.RemoveAt(node.Nodes.Count - 1);

            for (int i = 0; i < description.Children.Count; i++)
                RefreshNode(node.Nodes[i], description.Children[i]);
        }

        /// <summary>Return a full path for the specified node.</summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        private string FullPath(TreeNode node)
        {
            return "." + node.FullPath.Replace("\\", ".");
        }

        /// <summary>
        /// Find a specific node with the node path.
        /// NodePath format: .Parent.Child.SubChild
        /// </summary>
        /// <param name="namePath">The name path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Invalid name path ' + namePath + '</exception>
        private TreeNode FindNode(string namePath)
        {
            if (!namePath.StartsWith(".", StringComparison.CurrentCulture))
                throw new Exception("Invalid name path '" + namePath + "'");

            namePath = namePath.Remove(0, 1); // Remove the leading '.'

            TreeNode node = null;

            string[] NamePathBits = namePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string PathBit in NamePathBits)
            {
                if (node == null)
                    node = FindNode(TreeView.Nodes, PathBit);
                else
                {
                    if (!node.IsExpanded)
                        node.Expand();
                    node = FindNode(node.Nodes, PathBit);
                }

                if (node == null)
                    return null;
            }
            return node;
        }

        /// <summary>
        /// Find a child node with the specified name under the specified ParentNode.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private static TreeNode FindNode(TreeNodeCollection nodes, string name)
        {
            foreach (TreeNode childNode in nodes)
                if (childNode.Text.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    return childNode;

            return null;
        }

        /// <summary>
        /// Override the process command key method so that we can implement global keyboard
        /// shortcuts.
        /// </summary>
        /// <param name="msg">The windows message to process</param>
        /// <param name="keyData">The key to process</param>
        /// <returns>True if command key was processed.</returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ShortcutKeyPressed != null && ShortcutKeys != null && ShortcutKeys.Contains(keyData))
            {
                ShortcutKeyPressed.Invoke(this, new KeysArgs() { Keys = keyData });
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Events

        /// <summary>User has selected a node. Raise event for presenter.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TreeViewEventArgs"/> instance containing the event data.</param>
        private void OnAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (SelectedNodeChanged != null)
            {
                NodeSelectedArgs selectionChangedData = new NodeSelectedArgs();
                selectionChangedData.OldNodePath = previouslySelectedNodePath;
                selectionChangedData.NewNodePath = FullPath(e.Node);
                SelectedNodeChanged.Invoke(this, selectionChangedData);
                previouslySelectedNodePath = selectionChangedData.NewNodePath;
            }
        }

        /// <summary>User has moved the splitter.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SplitterEventArgs"/> instance containing the event data.</param>
        private void OnSplitterMoved(object sender, SplitterEventArgs e)
        {
            // There is a bug in mono when run on Mac - looks like the split position isn't working.
            // the workaround below gets around that.
            splitter1.SplitterMoved -= OnSplitterMoved;
            splitter1.SplitPosition = this.PointToClient(MousePosition).X;
            splitter1.SplitterMoved += OnSplitterMoved;
        }

        // Looks like drag and drop is broken on Mono on Mac. The data being dragged needs to be
        // serializable which is ok but it still doesn work. Gives the error:
        //     System.Runtime.Serialization.SerializationException: Unexpected binary element: 46
        //     at System.Runtime.Serialization.Formatters.Binary.ObjectReader.ReadObject (BinaryElement element, System.IO.BinaryReader reader, System.Int64& objectId, System.Object& value, System.Runtime.Serialization.SerializationInfo& info) [0x00000] in <filename unknown>:0 

        /// <summary>Node has begun to be dragged.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnNodeDrag(object sender, ItemDragEventArgs e)
        {
            DragStartArgs args = new DragStartArgs();
            args.NodePath = FullPath(e.Item as TreeNode);
            if (DragStarted != null)
            {
                DragStarted(this, args);
                if (args.DragObject != null)
                {
                    sourcePathOfItemBeingDragged = args.NodePath;
                    DoDragDrop(args.DragObject, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
                }
            }
        }

        /// <summary>Node has been dragged over another node. Allow a drop here?</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;

			// Get the drop location
			TreeNode DestinationNode = TreeView.GetNodeAt(TreeView.PointToClient(new Point(e.X, e.Y)));
			if (DestinationNode != null)
            {
                AllowDropArgs Args = new AllowDropArgs();
                Args.NodePath = FullPath(DestinationNode);

                string[] Formats = e.Data.GetFormats();
                if (Formats.Length > 0)
                {
                    Args.DragObject = e.Data.GetData(Formats[0]) as ISerializable;
                    if (AllowDrop != null)
                    {
                        AllowDrop(this, Args);
                        if (Args.Allow)
                        {
                            string SourceParent = null;
                            if (sourcePathOfItemBeingDragged != null)
                                SourceParent = StringUtilities.ParentName(sourcePathOfItemBeingDragged);

                            // Now determine the effect. If the drag originated from a different view 
                            // (e.g. a toolbox or another file) then only copy is supported.
                            if (sourcePathOfItemBeingDragged == null)
                                e.Effect = DragDropEffects.Copy;  // Dragging from a foreign view.
                            else if (SourceParent == Args.NodePath)
                                e.Effect = DragDropEffects.Copy;  // Dragged node's parent is the node we're currently over
                            else if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt)
                                e.Effect = DragDropEffects.Link;
                            else if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                                e.Effect = DragDropEffects.Move;
                            else
                                e.Effect = DragDropEffects.Copy;
                        }
                    }
                }
            }
        }

        /// <summary>Node has been dropped. Send to presenter.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event data.</param>
        private void OnDragDrop(object sender, DragEventArgs e)
        {
            TreeNode destinationNode = TreeView.GetNodeAt(TreeView.PointToClient(new Point(e.X, e.Y)));
            if (destinationNode != null && Droped != null)
            {
                DropArgs args = new DropArgs();
                args.NodePath = FullPath(destinationNode);

                string[] formats = e.Data.GetFormats();
                if (formats.Length > 0)
                {
                    args.DragObject = e.Data.GetData(formats[0]) as ISerializable;
                    if (e.Effect == DragDropEffects.Copy)
                        args.Copied = true;
                    else if (e.Effect == DragDropEffects.Move)
                        args.Moved = true;
                    else
                        args.Linked = true;
                    Droped(this, args);

                    // Under MONO / LINUX seem to need to deselect and reselect the node otherwise no
                    // node is selected after the drop and this causes problems later in OnTreeViewBeforeSelect
                    TreeView.SelectedNode = null;
                    TreeView.SelectedNode = destinationNode;
                }
            }

            sourcePathOfItemBeingDragged = null;
        }

        /// <summary>User is about to start renaming a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NodeLabelEditEventArgs"/> instance containing the event data.</param>
        private void OnBeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            nodePathBeforeRename = SelectedNode;
            TreeView.ContextMenuStrip = null;
            e.CancelEdit = false;
        }

        /// <summary>User has finished renamed a node.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NodeLabelEditEventArgs"/> instance containing the event data.</param>
        private void OnAfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            TreeView.ContextMenuStrip = this.PopupMenu;
            if (Renamed != null && e.Label != null)
            {
                NodeRenameArgs args = new NodeRenameArgs()
                {
                    NodePath = this.nodePathBeforeRename,
                    NewName = e.Label
                };
                Renamed(this, args);
                e.CancelEdit = args.CancelEdit;
                if (!e.CancelEdit)
                    previouslySelectedNodePath = args.NodePath;
            }
        }

        /// <summary>User has closed the status window.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void OnCloseStatusWindowClick(object sender, EventArgs e)
        {
            StatusWindow.Visible = false;
        }

        #endregion

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeView.SelectedNode = e.Node;
            }
        }
    }
}

