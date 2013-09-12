using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace UserInterface.Views
{
    /// <summary>
    /// A structure for holding info about an item in the treeview.
    /// </summary>
    public class NodeDescriptionArgs : EventArgs
    {
        public struct Description
        {
            public string Name;
            public string ResourceNameForImage;
            public bool HasChildren;
        }
        /// <summary>
        /// The path of the node that needs child descriptions. If this is null then
        /// Descriptions needs to contain root nodes.
        /// </summary>
        public string NodePath;
        public List<Description> Descriptions = new List<Description>();
    }

    /// <summary>
    /// A class for holding info about a collection of menu items.
    /// </summary>
    public class MenuDescriptionArgs : EventArgs
    {
        public struct Description
        {
            public string Name;
            public string ResourceNameForImage;
            public EventHandler OnClick;
        }

        public List<Description> Descriptions = new List<Description>();
    }

    /// <summary>
    /// A class for holding info about a node selection event.
    /// </summary>
    public class NodeSelectedArgs : EventArgs
    {
        public string OldNodePath;
        public string NewNodePath;
    }

    /// <summary>
    /// The interface for an explorer view.
    /// NB: All node paths are compatible with XmlHelper node paths.
    /// e.g.  /simulations/test/clock
    /// </summary>
    interface IExplorerView
    {
        /// <summary>
        /// ExplorerView will invoke this event when it wants the presenter to populate 
        /// direct children of the specified node.
        /// </summary>
        event EventHandler<NodeDescriptionArgs> PopulateChildNodes;

        /// <summary>
        /// This event will be invoked when the user selects a node.
        /// </summary>
        event EventHandler<NodeSelectedArgs> NodeSelectedByUser;

        /// <summary>
        /// This event will be invoked when a node is selected not by the user
        /// but by an Undo command.
        /// </summary>
        event EventHandler<NodeSelectedArgs> NodeSelected;

        /// <summary>
        /// ExplorerView will invoke this event when it wants the presenter to populate 
        /// the main menu with items.
        /// </summary>
        event EventHandler<MenuDescriptionArgs> PopulateMainMenu;

        /// <summary>
        /// ExplorerView will invoke this event when it wants the presenter to populate
        /// the context (popup) menu for the specified node.
        /// </summary>
        event EventHandler<MenuDescriptionArgs> PopulateContextMenu;

        /// <summary>
        /// Return the current node path.
        /// </summary>
        string CurrentNodePath { get; set; }

        /// <summary>
        /// Invalidate (redraw) the specified node and its direct child nodes.
        /// </summary>
        void InvalidateNode(string NodePath, NodeDescriptionArgs.Description Description);

        /// <summary>
        /// Add a view to the right hand panel.
        /// </summary>
        void AddRightHandView(UserControl Control);

        /// <summary>
        /// Add a status message. A message of null will clear the status message.
        /// </summary>
        /// <param name="Message"></param>
        void AddStatusMessage(string Message);

        /// <summary>
        /// A helper function that asks user for a SaveAs name and returns their new choice.
        /// </summary>
        string SaveAs(string OldFilename);

        /// <summary>
        /// Change the name of the tab.
        /// </summary>
        void ChangeTabText(string NewTabName);
    }


}
