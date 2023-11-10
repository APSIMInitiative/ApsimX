using System;

namespace UserInterface.EventArguments.DirectedGraph
{
    public class DelNodeEventArgs : EventArgs
    {
        public int nodeIDToDelete { get; set; }
    }
}