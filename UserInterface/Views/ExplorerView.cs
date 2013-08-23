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
    /// A structure for holding info about an item in the treeview.
    /// </summary>
    public struct NodeDescription
    {
        public string Name;
        public string ResourceNameForImage;
        public bool HasChildren;
    }

    public delegate void NodePathDelegate(string NodePath);


    /// <summary>
    /// The interface for an explorer view.
    /// NB: All node paths are compatible with XmlHelper node paths.
    /// e.g.  /simulations/test/clock
    /// </summary>
    interface IExplorerView
    {
        event NodePathDelegate PopulateChildNodes;
        event NodePathDelegate NodeSelectedByUser;
        event NodePathDelegate NodeSelected;

        /// <summary>
        /// Add a child node to the parent node (as specified by ParentPath). If
        /// ParentPath is null then the node will be added as the root node.
        /// </summary>
        void AddNode(string ParentPath, NodeDescription Description);

        /// <summary>
        /// Remove the specified node.
        /// </summary>
        void RemoveNode(string NodePath);

        /// <summary>
        /// Clear all child nodes under the specified NodePath.
        /// </summary>
        void ClearNodes(string NodePath);

        /// <summary>
        /// Return the current node path.
        /// </summary>
        string CurrentNodePath { get; set; }

        /// <summary>
        /// Add an action (on toolstrip).
        /// </summary>
        void AddAction(string ButtonText, Image Image, System.EventHandler OnClick);

        /// <summary>
        /// Remove an action (from toolstrip).
        /// </summary>
        void RemoveAction(string ButtonText);

        /// <summary>
        /// Add an action (on context menu).
        /// </summary>
        void AddContextAction(string ButtonText, Image Image, System.EventHandler OnClick);

        /// <summary>
        /// Remove an action (from context menu).
        /// </summary>
        void RemoveContextAction(string ButtonText);

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

        public event NodePathDelegate PopulateChildNodes;
        public event NodePathDelegate NodeSelectedByUser;
        public event NodePathDelegate NodeSelected;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExplorerView()
        {
            InitializeComponent();
            StatusPanel.Visible = false;
        }

        #region Tree node
        /// <summary>
        /// Add a child node to the parent node (as specified by ParentPath). If
        /// ParentPath is null then the node will be added as the root node.
        /// </summary>
        public void AddNode(string ParentPath, NodeDescription Description)
        {
            TreeNode Node = new TreeNode();
            if (ParentPath == null)
                TreeView.Nodes.Add(Node);
            else
            {
                TreeNode ParentNode = FindNode(ParentPath);
                if (ParentNode == null)
                    throw new Exception("Cannot find tree node: " + ParentPath);
                ParentNode.Nodes.Add(Node);
            }
            ConfigureNode(Node, Description);
        }

        /// <summary>
        /// Remove the node as specified by NodePath.
        /// </summary>
        public void RemoveNode(string NodePath)
        {
            TreeNode Node = FindNode(NodePath);
            if (Node != null)
                Node.Remove();
        }

        /// <summary>
        /// Clear all child nodes under the specified NodePath.
        /// </summary>
        public void ClearNodes(string NodePath)
        {
            TreeNode Node = FindNode(NodePath);
            if (Node != null)
                Node.Nodes.Clear();
        }
        /// <summary>
        /// A property providing access to the currently selected node.
        /// </summary>
        /// 
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
                TreeView.BeforeSelect -= TreeView_BeforeSelect;

                TreeNode NodeToSelect;
                if (value == "")
                    NodeToSelect = null;
                else
                    NodeToSelect = FindNode(value);

                if (TreeView.SelectedNode != NodeToSelect)
                {
                    TreeView.SelectedNode = NodeToSelect;
                    TreeView.BeforeSelect += TreeView_BeforeSelect;
                    if (NodeSelected != null)
                        NodeSelected.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Configure the specified tree node using the fields in 'Description'
        /// </summary>
        private void ConfigureNode(TreeNode Node, NodeDescription Description)
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
                Control.BringToFront();
            }
        }
        #endregion

        #region Action

        /// <summary>
        /// Add an action (on toolstrip).
        /// </summary>
        public void AddAction(string ButtonText, Image Image, System.EventHandler OnClick)
        {
            ToolStripItem Button = ToolStrip.Items.Add(ButtonText, Image, OnClick);
            Button.TextImageRelation = TextImageRelation.ImageAboveText;
            ToolStrip.Visible = ToolStrip.Items.Count > 0;
        }

        /// <summary>
        /// Remove an action (from toolstrip).
        /// </summary>
        public void RemoveAction(string ButtonText)
        {
            ToolStripItem[] Button = ToolStrip.Items.Find(ButtonText, false);
            if (Button.Length != 0)
                ToolStrip.Items.Remove(Button[0]);
        }
        #endregion

        #region Context Actions
        /// <summary>
        /// Add the specified tool to the top tool strip. Make sure the toolstrip is visible.
        /// </summary>
        public void AddContextAction(string ButtonText, Image Image, System.EventHandler OnClick)
        {
            ToolStripItem Button = PopupMenu.Items.Add(ButtonText, Image, OnClick);
            Button.TextImageRelation = TextImageRelation.ImageAboveText;
        }

        /// <summary>
        /// Delete the specified tool from the top tool strip. Make the toolstrip invisible
        /// if there are no tools left.
        /// </summary>
        public void RemoveContextAction(string ButtonText)
        {
            ToolStripItem[] Button = PopupMenu.Items.Find(ButtonText, false);
            if (Button.Length != 0)
                PopupMenu.Items.Remove(Button[0]);
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


        #region Events


        /// <summary>
        /// User is expanding a node. Populate the child nodes if necessary.
        /// </summary>
        private void OnBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (PopulateChildNodes != null && e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "Loading...")
                PopulateChildNodes.Invoke(FullPath(e.Node));
        }

        /// <summary>
        /// A node is about to be selected by the user.
        /// </summary>
        private void TreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (NodeSelectedByUser != null && TreeView.SelectedNode != e.Node)
            {
                string PathOfNode = FullPath(e.Node);
                NodeSelectedByUser.Invoke(PathOfNode);
            }
        }

        /// <summary>
        /// User has moved the splitter.
        /// </summary>
        private void SplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            // There is a bug in mono when run on Mac - looks like the split position isn't working.
            // the workaround below gets around that.
            splitter1.SplitterMoved -= SplitContainer_SplitterMoved;
            splitter1.SplitPosition = this.PointToClient(MousePosition).X;
            splitter1.SplitterMoved += SplitContainer_SplitterMoved;
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



    }



}

