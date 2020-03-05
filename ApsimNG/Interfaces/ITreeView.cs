using UserInterface.Views;
namespace UserInterface.Interfaces
{
    
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// This interface encapsulates a hierachical tree view that the user interacts with.
    /// </summary>
    public interface ITreeView
    {
        /// <summary>Invoked when a node is selected not by the user but by an Undo command.</summary>
        event EventHandler<NodeSelectedArgs> SelectedNodeChanged;

        /// <summary>Invoked when a drag operation has commenced. Need to create a DragObject.</summary>
        event EventHandler<DragStartArgs> DragStarted;

        /// <summary>Invoked to determine if a drop is allowed on the specified Node.</summary>
        event EventHandler<AllowDropArgs> AllowDrop;

        /// <summary>Invoked when a drop has occurred.</summary>
        event EventHandler<DropArgs> Droped;

        /// <summary>Invoked then a node is renamed.</summary>
        event EventHandler<NodeRenameArgs> Renamed;

        /// <summary>Invoked then a node is double clicked.</summary>
        event EventHandler<EventArgs> DoubleClicked;

        /// <summary>Gets or sets the currently selected node.</summary>
        string SelectedNode { get; set; }

        /// <summary>Gets or sets the width of the tree view.</summary>
        Int32 TreeWidth { get; set; }

        /// <summary>Gets or sets whether tree nodes can be changed.</summary>
        bool ReadOnly { get; set; }

        /// <summary>Gets or sets the popup menu of the tree view.</summary>
        MenuView ContextMenu { get; set; }

        /// <summary>Populate the treeview.</summary>
        /// <param name="rootNode">A description of the top level root node</param>
        void Populate(TreeViewNode rootNode);

        /// <summary>Moves the specified node up 1 position.</summary>
        /// <param name="nodePath">The path of the node to move.</param>
        void MoveUp(string nodePath);

        /// <summary>Moves the specified node down 1 position.</summary>
        /// <param name="nodePath">The path of the node to move.</param>
        void MoveDown(string nodePath);

        /// <summary>Renames the specified node path.</summary>
        /// <param name="nodePath">The node path.</param>
        /// <param name="newName">The new name for the node.</param>
        void Rename(string nodePath, string newName);

        /// <summary>Puts the current node into edit mode so user can rename it.</summary>
        void BeginRenamingCurrentNode();

        /// <summary>Deletes the specified node.</summary>
        /// <param name="nodePath">The node path.</param>
        void Delete(string nodePath);

        /// <summary>Adds a child node.</summary>
        /// <param name="parentNodePath">The parent node path.</param>
        /// <param name="nodeDescription">The node description.</param>
        /// <param name="position">The position.</param>
        void AddChild(string parentNodePath, TreeViewNode nodeDescription, int position = -1);

        /// <summary>
        /// Returns tree nodes which are expanded.
        /// </summary>
        /// <returns></returns>
        Utility.TreeNode[] GetExpandedNodes();

        /// <summary>
        /// Expands nodes.
        /// </summary>
        /// <param name="expandedNodes"></param>
        void ExpandNodes(Utility.TreeNode[] expandedNodes);

        /// <summary>
        /// Expands all child nodes recursively.
        /// </summary>
        /// <param name="path">Path to the node. e.g. ".Simulations.DataStore"</param>
        void ExpandChildren(string path);

        /// <summary>
        /// Collapses all child nodes recursively.
        /// </summary>
        /// <param name="path">Path to the node. e.g. ".Simulations.DataStore"</param>
        void CollapseChildren(string path);
    }

    /// <summary>A structure for holding info about an item in the treeview.</summary>
    public class TreeViewNode : EventArgs
    {
        /// <summary>The name of the node</summary>
        public string Name;

        /// <summary>The text displayed on mouse hover</summary>
        public string ToolTip = null;

        /// <summary>The resource name for image</summary>
        public string ResourceNameForImage;

        /// <summary>The child nodes of this node</summary>
        public List<TreeViewNode> Children = new List<TreeViewNode>();

        /// <summary>Determines whether this node is checked</summary>
        public bool Checked { get; set; }

        /// <summary>The text colour of this node.</summary>
        public System.Drawing.Color Colour { get; set; }

        /// <summary>Determines whether this node's font is strikethrough</summary>
        public bool Strikethrough;
    }

    /// <summary>A class for holding info about a node selection event.</summary>
    public class NodeSelectedArgs : EventArgs
    {
        /// <summary>The old node path</summary>
        public string OldNodePath;
        /// <summary>The new node path</summary>
        public string NewNodePath;
    }

    /// <summary>A clas for holding info about a node rename event.</summary>
    public class NodeRenameArgs : EventArgs
    {
        /// <summary>The node path</summary>
        public string NodePath;
        /// <summary>The new name</summary>
        public string NewName;
        /// <summary>The cancel edit</summary>
        public bool CancelEdit;
    }

    /// <summary>A class for holding info about a begin drag event.</summary>
    public class DragStartArgs : EventArgs
    {
        /// <summary>The node path</summary>
        public string NodePath;
        /// <summary>The drag object</summary>
        public ISerializable DragObject;
    }

    /// <summary>A class for holding info about a begin drag event.</summary>
    public class AllowDropArgs : EventArgs
    {
        /// <summary>The node path</summary>
        public string NodePath;
        /// <summary>The drag object</summary>
        public ISerializable DragObject;
        /// <summary>The allow</summary>
        public bool Allow;
    }

    /// <summary>A class for holding info about a begin drag event.</summary>
    public class DropArgs : EventArgs
    {
        /// <summary>The node path</summary>
        public string NodePath;
        /// <summary>The copied</summary>
        public bool Copied;
        /// <summary>The moved</summary>
        public bool Moved;
        /// <summary>The linked</summary>
        public bool Linked;
        /// <summary>The drag object</summary>
        public ISerializable DragObject;
    }

}
