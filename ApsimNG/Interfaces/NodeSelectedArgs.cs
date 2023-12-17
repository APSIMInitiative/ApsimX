namespace UserInterface.Interfaces
{

    using System;

    /// <summary>A class for holding info about a node selection event.</summary>
    public class NodeSelectedArgs : EventArgs
    {
        /// <summary>The old node path</summary>
        public string OldNodePath;
        /// <summary>The new node path</summary>
        public string NewNodePath;
    }

}
