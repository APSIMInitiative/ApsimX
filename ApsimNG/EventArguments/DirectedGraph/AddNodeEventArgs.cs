using APSIM.Shared.Graphing;
using System;

namespace ApsimNG.EventArguments.DirectedGraph
{
    public class AddNodeEventArgs : EventArgs
    {
        public Node Node { get; set; }
        public AddNodeEventArgs(Node node) => Node = node;
    }
}