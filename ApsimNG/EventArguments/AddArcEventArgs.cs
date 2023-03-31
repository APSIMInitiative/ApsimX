using System;
using Models.Management;

namespace UserInterface.EventArguments.DirectedGraph
{
    public class AddArcEventArgs : EventArgs
    {
        public RuleAction Arc { get; set; }
    }
}