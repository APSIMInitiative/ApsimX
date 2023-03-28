namespace UserInterface.Interfaces
{

    using System;
    using System.Runtime.Serialization;

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
