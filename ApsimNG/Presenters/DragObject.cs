namespace UserInterface.Presenters
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// An object that encompasses the data that is dragged during a drag/drop operation.
    /// </summary>
    [Serializable]
    public sealed class DragObject : ISerializable
    {
        /// <summary>Gets or sets the path to the node</summary>
        public string NodePath { get; set; }

        /// <summary>Gets or sets the string representation of a model.</summary>
        public string ModelString { get; set; }

        /// <summary>Gets or sets the type of model</summary>
        public Type ModelType { get; set; }

        /// <summary>Get data for the specified object in the xml</summary>
        /// <param name="info">Serialized object</param>
        /// <param name="context">The context</param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("NodePath", this.NodePath);
            info.AddValue("Xml", this.ModelString);
        }
    }
}
