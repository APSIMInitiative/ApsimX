using APSIM.Shared.Graphing;
using System;

namespace ApsimNG.EventArguments.DirectedGraph
{
    public class AddArcEventArgs : EventArgs
    {
        public Arc Arc { get; set; }
    }
}