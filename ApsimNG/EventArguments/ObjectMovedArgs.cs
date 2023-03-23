using System;
using APSIM.Interop.Visualisation;

namespace UserInterface.EventArguments.DirectedGraph
{
    public class ObjectMovedArgs : EventArgs
    {
        /// <summary>
        /// The object which has been moved.
        /// </summary>
        public DGObject MovedObject { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="obj">The object which has been moved.</param>
        public ObjectMovedArgs(DGObject obj)
        {
            MovedObject = obj;
        }
    }
}