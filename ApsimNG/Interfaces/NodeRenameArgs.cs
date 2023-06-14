namespace UserInterface.Interfaces
{

    using System;

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

}
