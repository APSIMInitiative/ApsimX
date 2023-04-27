using System;

namespace UserInterface.EventArguments.DirectedGraph
{
    public class DelNodeEventArgs : EventArgs
    {
        public string NodeNameToDelete { get; set; }
    }
}