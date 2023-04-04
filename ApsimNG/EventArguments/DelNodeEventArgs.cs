using System;

namespace UserInterface.EventArguments.DirectedGraph
{
    public class DelNodeEventArgs : EventArgs
    {
        public string nodeNameToDelete { get; set; }
    }
}