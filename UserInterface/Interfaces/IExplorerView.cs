// -----------------------------------------------------------------------
// <copyright file="IExplorerView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Windows.Forms;
    using EventArguments;

    /// <summary>A structure for holding info about an item in the treeview.</summary>
    public class NodeDescriptionArgs : EventArgs
    {
        /// <summary>The name</summary>
        public string Name;
        /// <summary>The type(name) displayed on hover</summary>
        public string ToolTip = "";
        /// <summary>
        /// The resource name for image
        /// </summary>
        public string ResourceNameForImage;
        /// <summary>The children</summary>
        public List<NodeDescriptionArgs> Children = new List<NodeDescriptionArgs>();
    }

    /// <summary>A class for holding info about a collection of menu items.</summary>
    public class MenuDescriptionArgs : EventArgs
    {
        /// <summary>The name</summary>
        public string Name;
        /// <summary>
        /// The resource name for image
        /// </summary>
        public string ResourceNameForImage;
        /// <summary>The on click</summary>
        public EventHandler OnClick;
        /// <summary>The checked</summary>
        public bool Checked;
        /// <summary>The shortcut key</summary>
        public Keys ShortcutKey;
        /// <summary>The enabled</summary>
        public bool Enabled;
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

    /// <summary>
    /// The interface for an explorer view.
    /// NB: All node paths are compatible with XmlHelper node paths.
    /// e.g.  /simulations/test/clock
    /// </summary>
    public interface IExplorerView
    {
        /// <summary>
        /// This event will be invoked when a node is selected not by the user
        /// but by an Undo command.
        /// </summary>
        event EventHandler<NodeSelectedArgs> SelectedNodeChanged;

        /// <summary>
        /// Invoked when a drag operation has commenced. Need to create a DragObject.
        /// </summary>
        event EventHandler<DragStartArgs> DragStarted;

        /// <summary>
        /// Invoked when the view wants to know if a drop is allowed on the specified Node.
        /// </summary>
        event EventHandler<AllowDropArgs> AllowDrop;

        /// <summary>Invoked when a drop has occurred.</summary>
        event EventHandler<DropArgs> Droped;

        /// <summary>Invoked then a node is renamed.</summary>
        event EventHandler<NodeRenameArgs> Renamed;

        /// <summary>Invoked when a shortcut key is pressed.</summary>
        event EventHandler<KeysArgs> ShortcutKeyPressed;

        /// <summary>Refreshes the entire tree from the specified descriptions.</summary>
        /// <param name="nodeDescriptions">The nodes descriptions.</param>
        void Refresh(NodeDescriptionArgs nodeDescriptions);

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
        void AddChild(string parentNodePath, NodeDescriptionArgs nodeDescription, int position = -1);

        /// <summary>Gets or sets the currently selected node.</summary>
        /// <value>The selected node.</value>
        string SelectedNode { get; set; }

        /// <summary>Gets or sets the shortcut keys.</summary>
        /// <value>The shortcut keys.</value>
        Keys[] ShortcutKeys { get; set; }

        /// <summary>Populate the main menu tool strip.</summary>
        /// <param name="menuDescriptions">Menu descriptions for each menu item.</param>
        void PopulateMainToolStrip(List<MenuDescriptionArgs> menuDescriptions);

        /// <summary>Populates the label.</summary>
        /// <param name="labelText">The label text.</param>
        /// <param name="labelToolTip">The label tool tip.</param>
        void PopulateLabel(string labelText, string labelToolTip);

        /// <summary>
        /// Populate the context menu from the descriptions passed in.
        /// </summary>
        /// <param name="menuDescriptions">Menu descriptions for each menu item.</param>
        void PopulateContextMenu(List<MenuDescriptionArgs> menuDescriptions);
        
        /// <summary>
        /// Add a view to the right hand panel.
        /// </summary>
        void AddRightHandView(UserControl Control);

        /// <summary>
        /// Ask about saving.
        /// </summary>
        /// <returns>-1, 0, 1</returns>
        Int32 AskToSave();

        /// <summary>
        /// A helper function that asks user for a folder.
        /// </summary>
        /// <returns>Returns the selected folder or null if action cancelled by user.</returns>
        string AskUserForFolder(string prompt);
        
        /// <summary>
        /// A helper function that asks user for a file.
        /// </summary>
        /// <returns>Returns the selected file or null if action cancelled by user.</returns>
        string AskUserForFile(string prompt);

        /// <summary>
        /// Add a status message. A message of null will clear the status message.
        /// </summary>
        /// <param name="Message"></param>
        void ShowMessage(string Message, Models.DataStore.ErrorLevel errorLevel);

        /// <summary>
        /// A helper function that asks user for a SaveAs name and returns their new choice.
        /// </summary>
        string SaveAs(string OldFilename);

        /// <summary>
        /// Change the name of the tab.
        /// </summary>
        void ChangeTabText(string NewTabName);

        /// <summary>
        /// Turn on or off the 2nd explorer view.
        /// </summary>
        void ToggleSecondExplorerViewVisible();

        /// <summary>
        /// Gets or sets the width of the tree view.
        /// </summary>
        Int32 TreeWidth { get; set; }

        /// <summary>
        /// Close down APSIMX user interface.
        /// </summary>
        void Close();
    }


}
