namespace UserInterface.Interfaces
{

    using System;
    using System.Runtime.Serialization;

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

}
