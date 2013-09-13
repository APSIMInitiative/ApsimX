using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using UserInterface.Commands;
using System.Runtime.Serialization;
using UserInterface.Views;

namespace UserInterface.Views
{

    /// <summary>
    /// An ExplorerView is a "Windows Explorer" like control that displays a virtual tree control on the left
    /// and a user interface on the right allowing the user to modify properties of whatever they
    /// click on in the tree control. 
    /// </summary>
    /// <remarks>
    /// This "view" class follows the Humble Dialog approach were
    /// no business logic is embedded in this class. This object is told what to do by a 
    /// presenter object which is responsible for populating all controls. The theory is that this
    /// class can be reused for differents types of data.
    /// 
    /// When populating nodes, it is given a list of NodeDescription objects that describes what the
    /// nodes look like.
    /// 
    /// NB: All node paths are compatible with Model node paths and includes the root node name:
    ///     If tree is:
    ///     Simulations
    ///         |
    ///         +-- Test
    ///               |
    ///               +--- Clock
    /// e.g.  Simulations.Test.Clock
    /// </remarks>
    public partial class ExplorerView : UserControl, IExplorerView
    {
        /// <summary>
        /// ExplorerView will invoke this event when it wants the presenter to populate 
        /// direct children of the specified node.
        /// </summary>
        public event EventHandler<NodeDescriptionArgs> PopulateChildNodes;

        /// <summary>
        /// This event will be invoked when the user selects a node.
        /// </summary>
        public event EventHandler<NodeSelectedArgs> NodeSelectedByUser;

        /// <summary>
        /// This event will be invoked when a node is selected not by the user
        /// but by an Undo command.
        /// </summary>
        public event EventHandler<NodeSelectedArgs> NodeSelected;

        /// <summary>
        /// ExplorerView will invoke this event when it wants the presenter to populate 
        /// the main menu with items.
        /// </summary>
        public event EventHandler<MenuDescriptionArgs> PopulateMainMenu;

        /// <summary>
        /// ExplorerView will invoke this event when it wants the presenter to populate
        /// the context (popup) menu for the specified node.
        /// </summary>
        public event EventHandler<MenuDescriptionArgs> PopulateContextMenu;
        private string PreviouslySelectedNodePath;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExplorerView()
        {
            InitializeComponent();
            StatusPanel.Visible = false;
        }

        #region Tree node
        ///// <summary>
        ///// Add a child node to the parent node (as specified by ParentPath). If
        ///// ParentPath is null then the node will be added as the root node.
        ///// </summary>
        //public void AddNode(string ParentPath, NodeDescription Description)
        //{
        //    TreeNode Node = new TreeNode();
        //    if (ParentPath == null)
        //    {
        //        // Root node.
        //        TreeView.Nodes.Add(Node);
        //        PopulateMainToolStrip();
        //    }
        //    else
        //    {
        //        TreeNode ParentNode = FindNode(ParentPath);
        //        if (ParentNode == null)
        //            throw new Exception("Cannot find tree node: " + ParentPath);
        //        ParentNode.Nodes.Add(Node);
        //    }
        //    ConfigureNode(Node, Description);
        //}

        ///// <summary>
        ///// Remove the node as specified by NodePath.
        ///// </summary>
        //public void RemoveNode(string NodePath)
        //{
        //    TreeNode Node = FindNode(NodePath);
        //    if (Node != null)
        //        Node.Remove();
        //}

        ///// <summary>
        ///// Clear all child nodes under the specified NodePath.
        ///// </summary>
        //public void ClearNodes(string NodePath)
        //{
        //    TreeNode Node = FindNode(NodePath);
        //    if (Node != null)
        //        Node.Nodes.Clear();
        //}
        ///// <summary>
        ///// A property providing access to the currently selected node.
        ///// </summary>
        ///// 
        public string CurrentNodePath
        {
            get
            {
                if (TreeView.SelectedNode != null)
                    return FullPath(TreeView.SelectedNode);
                else
                    return "";
            }
            set
            {
                // We want the BeforeSelect event to only fire when user clicks on a node
                // in the tree.
                TreeView.AfterSelect -= TreeView_AfterSelect;

                TreeNode NodeToSelect;
                if (value == "")
                    NodeToSelect = null;
                else
                    NodeToSelect = FindNode(value);

                if (TreeView.SelectedNode != NodeToSelect)
                {
                    TreeView.SelectedNode = NodeToSelect;
                    if (NodeSelected != null)
                        NodeSelected.Invoke(this, new NodeSelectedArgs()
                        {
                            OldNodePath = PreviouslySelectedNodePath,
                            NewNodePath = value
                        });
                }

                TreeView.AfterSelect += TreeView_AfterSelect;
            }
        }

        /// <summary>
        /// Configure the specified tree node using the fields in 'Description'
        /// </summary>
        private void ConfigureNode(TreeNode Node, NodeDescriptionArgs.Description Description)
        {
            Node.Text = Description.Name;
            int imageIndex = TreeImageList.Images.IndexOfKey(Description.ResourceNameForImage);
            if (imageIndex == -1)
            {
                Bitmap Icon = Properties.Resources.ResourceManager.GetObject(Description.ResourceNameForImage) as Bitmap;
                if (Icon != null)
                {
                    TreeImageList.Images.Add(Description.ResourceNameForImage, Icon);
                    imageIndex = TreeImageList.Images.Count - 1;
                }
            }
            Node.ImageKey = Description.ResourceNameForImage;
            Node.SelectedImageKey = Description.ResourceNameForImage;
            if (Description.HasChildren)
                Node.Nodes.Add("Loading...");
        }

        /// <summary>
        /// Return a full path for the specified node.
        /// </summary>
        private string FullPath(TreeNode Node)
        {
            return Node.FullPath.Replace("\\", ".");
        }

        /// <summary>
        /// Find a specific node with the node path.
        /// NodePath format: Parent.Child.SubChild
        /// </summary>
        private TreeNode FindNode(string NamePath)
        {
            TreeNode Node = null;

            string[] NamePathBits = NamePath.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string PathBit in NamePathBits)
            {
                if (Node == null)
                    Node = FindNode(TreeView.Nodes, PathBit);
                else
                    Node = FindNode(Node.Nodes, PathBit);

                if (Node == null)
                    return null;
            }
            return Node;
        }

        /// <summary>
        /// Find a child node with the specified name under the specified ParentNode.
        /// </summary>
        private static TreeNode FindNode(TreeNodeCollection Nodes, string name)
        {
            foreach (TreeNode ChildNode in Nodes)
                if (ChildNode.Text.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    return ChildNode;

            return null;
        }

        #endregion

        #region Right hand panel
        /// <summary>
        /// Add a user control to the right hand panel. If Control is null then right hand panel will be cleared.
        /// </summary>
        public void AddRightHandView(UserControl Control)
        {
            RightHandPanel.Controls.Clear();
            if (Control != null)
            {
                RightHandPanel.Controls.Add(Control);
                Control.Dock = DockStyle.Fill;
                //Control.BringToFront();

                // In MONO OSX, if the right hand panel isn't given the focus then when the user clicks on 
                // a GridView in the right hand window (e.g. clocks gridview) and starts using the cursor 
                // keys to navigate the grid then the key presses seem to go to the StartPageView in the 
                // other tab in the top level tab control. Then an exception is throw from StartPageView.
                if (Environment.OSVersion.Platform != PlatformID.Win32NT &&
                    Environment.OSVersion.Platform != PlatformID.Win32Windows)
                    RightHandPanel.Focus();  // On Windows this causes a blicking event on the node focus.
            }
        }
        #endregion

        #region Main menu

        /// <summary>
        /// Populate the main menu tool strip.
        /// </summary>
        private void PopulateMainToolStrip()
        {
            if (PopulateMainMenu != null)
            {
                MenuDescriptionArgs Args = new MenuDescriptionArgs();
                PopulateMainMenu(this, Args);

                ToolStrip.Items.Clear();
                foreach (MenuDescriptionArgs.Description Description in Args.Descriptions)
                {
                    Bitmap Icon = Properties.Resources.ResourceManager.GetObject(Description.ResourceNameForImage) as Bitmap;
                    ToolStripItem Button = ToolStrip.Items.Add(Description.Name, Icon, Description.OnClick);
                    Button.TextImageRelation = TextImageRelation.ImageAboveText;
                }
                ToolStrip.Visible = ToolStrip.Items.Count > 0;
            }
        }

        #endregion

        /// <summary>
        /// Change the name of the tab.
        /// </summary>
        public void ChangeTabText(string NewTabName)
        {
            TabPage Page = Parent as TabPage;
            Page.Text = NewTabName;

        }

        /// <summary>
        /// Add a status message to the explorer window
        /// </summary>
        /// <param name="Message"></param>
        public void AddStatusMessage(string Message)
        {
            StatusPanel.Visible = Message != null;
            StatusLabel.Text = Message;
            Application.DoEvents();
        }

        /// <summary>
        /// A helper function that asks user for a SaveAs name and returns their new choice.
        /// </summary>
        /// <returns>Returns the new file name or null if action cancelled by user.</returns>
        public string SaveAs(string OldFilename)
        {
            SaveFileDialog.FileName = OldFilename;
            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
                return SaveFileDialog.FileName;
            else
                return null;
        }

        /// <summary>
        /// Invalidate (redraw) the specified node and its direct child nodes.
        /// </summary>
        public void InvalidateNode(string NodePath, NodeDescriptionArgs.Description Description)
        {
            TreeNode Node = FindNode(NodePath);
            ConfigureNode(Node, Description);
            PopulateNodes(Node);
        }

        #region Events

        /// <summary>
        /// User is expanding a node. Populate the child nodes if necessary.
        /// </summary>
        private void OnBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (PopulateChildNodes != null && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "Loading...")
            {
                PopulateNodes(e.Node);
            }
        }

        /// <summary>
        /// Populate all direct children under the specified Node. If Node = null then
        /// populate all root nodes.
        /// </summary>
        private void PopulateNodes(TreeNode ParentNode)
        {
            NodeDescriptionArgs Args = new NodeDescriptionArgs();
            if (ParentNode != null)
                Args.NodePath = FullPath(ParentNode);
            PopulateChildNodes.Invoke(this, Args);

            TreeNodeCollection Nodes;
            if (ParentNode == null)
                Nodes = TreeView.Nodes;
            else
                Nodes = ParentNode.Nodes;

            // Make sure we have the right number of child nodes.
            // Add extra nodes if necessary
            while (Args.Descriptions.Count > Nodes.Count)
                Nodes.Add(new TreeNode());

            // Remove unwanted nodes if necessary.
            while (Args.Descriptions.Count < Nodes.Count)
                Nodes.RemoveAt(0);

            // Configure each child node.
            for (int i = 0; i < Args.Descriptions.Count; i++)
                ConfigureNode(Nodes[i], Args.Descriptions[i]);
        }

        /// <summary>
        /// A node is about to be selected. Take note of the previous node path.
        /// </summary>
        private void OnTreeViewBeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            PreviouslySelectedNodePath = CurrentNodePath;
        }

        /// <summary>
        /// A node has been selected. Let the presenter know.
        /// </summary>
        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (NodeSelectedByUser != null)
            {
                NodeSelectedByUser.Invoke(this, new NodeSelectedArgs()
                {
                    OldNodePath = PreviouslySelectedNodePath,
                    NewNodePath = FullPath(e.Node)
                });
            }
        }

        /// <summary>
        /// User has moved the splitter.
        /// </summary>
        private void OnSplitterMoved(object sender, SplitterEventArgs e)
        {
            // There is a bug in mono when run on Mac - looks like the split position isn't working.
            // the workaround below gets around that.
            splitter1.SplitterMoved -= OnSplitterMoved;
            splitter1.SplitPosition = this.PointToClient(MousePosition).X;
            splitter1.SplitterMoved += OnSplitterMoved;
        }

        /// <summary>
        /// User has right clicked on a node, opening the context popup menu. Go create that menu
        /// by asking the presenter what to put on the menu.
        /// </summary>
        private void OnPopupMenuOpening(object sender, CancelEventArgs e)
        {
            if (PopulateContextMenu != null)
            {
                MenuDescriptionArgs Args = new MenuDescriptionArgs();
                PopulateContextMenu(this, Args);

                PopupMenu.Items.Clear();
                foreach (MenuDescriptionArgs.Description Description in Args.Descriptions)
                {
                    Bitmap Icon = Properties.Resources.ResourceManager.GetObject(Description.ResourceNameForImage) as Bitmap;
                    ToolStripItem Button = PopupMenu.Items.Add(Description.Name, Icon, Description.OnClick);
                    Button.TextImageRelation = TextImageRelation.ImageAboveText;
                }
            }
            e.Cancel = false;
        }


        #endregion

        #region Drag and drop

        /// <summary>
        /// An object that encompases the data that is dragged during a drag/drop operation.
        /// </summary>
        [Serializable]
        public class DragObject : ISerializable
        {
            public string NodePath = "asdf";

            void ISerializable.GetObjectData(SerializationInfo oInfo, StreamingContext oContext)
            {
                oInfo.AddValue("NodePath", NodePath);
            }
        }


        // Looks like drag and drop is broken on Mono on Mac. The data being dragged needs to be
        // serializable which is ok but it still doesn work. Gives the error:
        //     System.Runtime.Serialization.SerializationException: Unexpected binary element: 46
        //     at System.Runtime.Serialization.Formatters.Binary.ObjectReader.ReadObject (BinaryElement element, System.IO.BinaryReader reader, System.Int64& objectId, System.Object& value, System.Runtime.Serialization.SerializationInfo& info) [0x00000] in <filename unknown>:0 

        private void TreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DragObject DragObj = new DragObject() {NodePath = FullPath(e.Item as TreeNode)};
            DoDragDrop(DragObj, DragDropEffects.Copy);
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void TreeView_DragDrop(object sender, DragEventArgs e)
        {
           DragObject DragObject = e.Data.GetData(typeof(DragObject)) as DragObject;
           MessageBox.Show(DragObject.NodePath);
        }
        #endregion

        private void ExplorerView_Load(object sender, EventArgs e)
        {
            PopulateMainToolStrip();
            PopulateNodes(null);
        }

        /// <summary>
        /// Toggle the 2nd right hand side explorer view on/off
        /// </summary>
        public void ToggleSecondExplorerViewVisible()
        {
            MainForm MainForm = Application.OpenForms[0] as MainForm;
            MainForm.ToggleSecondExplorerViewVisible();
        }






    }



}

