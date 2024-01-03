using System;
using UserInterface.Interfaces;
using UserInterface.Views;
using Utility;

namespace UnitTests.Core
{
    internal class MockTreeView : ITreeView
    {
        public string SelectedNode { get; set; }
        public int TreeWidth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ReadOnly { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public MenuView ContextMenu { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<NodeSelectedArgs> SelectedNodeChanged;
        public event EventHandler<DragStartArgs> DragStarted;
        public event EventHandler<AllowDropArgs> AllowDrop;
        public event EventHandler<DropArgs> Droped;
        public event EventHandler<NodeRenameArgs> Renamed;
        public event EventHandler<EventArgs> DoubleClicked;

        public void AddChild(string parentNodePath, TreeViewNode nodeDescription, int position = -1)
        {
            throw new NotImplementedException();
        }

        public void BeginRenamingCurrentNode()
        {
            throw new NotImplementedException();
        }

        public void CollapseChildren(string path)
        {
            throw new NotImplementedException();
        }

        public void Delete(string nodePath)
        {
            throw new NotImplementedException();
        }

        public void ExpandChildren(string path, bool recursive = true)
        {
            throw new NotImplementedException();
        }

        public void ExpandNodes(TreeNode[] expandedNodes)
        {
            throw new NotImplementedException();
        }

        public TreeNode[] GetExpandedNodes()
        {
            throw new NotImplementedException();
        }

        public int GetNodePosition(string path)
        {
            throw new NotImplementedException();
        }

        public void MoveDown(string nodePath)
        {
            
        }

        public void MoveUp(string nodePath)
        {
            
        }

        public void Populate(TreeViewNode rootNode)
        {
            throw new NotImplementedException();
        }

        public void RefreshNode(string path, TreeViewNode description)
        {
            throw new NotImplementedException();
        }

        public void Rename(string nodePath, string newName)
        {
            throw new NotImplementedException();
        }
    }
}