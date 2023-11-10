using System;

namespace UserInterface.EventArguments.DirectedGraph
{
    public class DelArcEventArgs : EventArgs
    {
        public int arcIDToDelete { get; set; }
    }
}