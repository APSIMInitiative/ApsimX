using System;

namespace UserInterface.EventArguments.DirectedGraph
{
    public class DelArcEventArgs : EventArgs
    {
        public string ArcNameToDelete { get; set; }
    }
}