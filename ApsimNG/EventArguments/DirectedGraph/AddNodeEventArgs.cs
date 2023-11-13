using System;
using Models.Management;

namespace ApsimNG.EventArguments.DirectedGraph
{
    public class AddNodeEventArgs : EventArgs
    {
        public StateNode Node { get; set; }
        public AddNodeEventArgs(StateNode node) => Node = node;
    }
}